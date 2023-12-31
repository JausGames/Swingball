using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

public class Ball : NetworkBehaviour
{
    public enum State
    {
        Normal,
        NoOwner,
        Idle,
    }

    MatchManager match;
    private bool desync;
    [SerializeField] private Vector3 direction;
    [SerializeField] protected Vector3 speed;
    [SerializeField] private State state = State.Normal;
    [SerializeField] private LayerMask playerMask;

    [Header("Network")]
    [SerializeField] private ulong owner;
    [SerializeField] private Player ownerObj;
    [SerializeField] private Player target;
    [SerializeField] private List<Player> victims;
    [Header("Colors")]
    [SerializeField] Color noOwnerColor;
    [SerializeField] Color selfColor;
    [SerializeField] Color oppsColor;
    [Header("Component")]
    [SerializeField] protected List<ParticleSystem> particlesInst;
    [SerializeField] protected List<ParticleSystem> particles;
    [SerializeField] TrailRenderer trail;
    [SerializeField] protected new MeshRenderer renderer;
    [SerializeField] SpriteRenderer spriteFloorTarget;
    [SerializeField] protected Collider selfCollider;
    [SerializeField] protected Collider floorCollider;
    [SerializeField] private GameObject fakeBallPrefab;
    [Header("Stats")]
    [SerializeField] private float damageMult = 5f;
    [SerializeField] private float originSpeed = 10f;
    [SerializeField] protected float currSpeed = 10f;
    [SerializeField] protected int increment = 1;
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float redirectFactor = 1f;
    [SerializeField] private float posOffset = 1f;


    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource loopAudioSource;
    [SerializeField] private AudioSource slowMoAudioSource;
    [SerializeField] private AudioLowPassFilter lowPass;
    [SerializeField] private AudioClip hitclip;
    [SerializeField] private AudioClip lobClip;
    [SerializeField] private AudioClip smashClip;

    private UnityEvent onIdleOverEvent = new UnityEvent();
    private Rigidbody body;
    /*    private float lobSpeed = 8f;*/
    private float minPitch = .8f;
    private float maxPitch = 2f;
    private float idleTime;
    private long startIdleTime;
    private long endIdle;
    private AudioClip nextClip;
    private bool endIdleVfx;
    [SerializeField] private State endIdleState;

    public MatchManager Match { get => match; set => match = value; }
    public Rigidbody Body { get => body; set => body = value; }
    public Collider FloorCollider { get => floorCollider; set => floorCollider = value; }
    public UnityEvent OnIdleOverEvent { get => onIdleOverEvent; set => onIdleOverEvent = value; }
    internal State State1 { get => state; set => state = value; }
    internal Vector3 Speed { get => speed; set => speed = value; }
    public Player Target { get => target; set => target = value; }
    public Player OwnerObj { get => ownerObj; set => ownerObj = value; }
    public int Increment { get => increment; set => increment = value; }
    public Color Color { get => spriteFloorTarget.color; set => ChangeColors(value); }
    public ulong Owner { get => owner; set => owner = value; }
    public float CurrSpeed { get => currSpeed; set => currSpeed = value; }

    // Update is called once per frame
    void LateUpdate()
    {

        switch (state)
        {
            case State.Normal:
                if (match && target && ownerObj)
                {

                    if (match.Players.Length == 2)
                    {

                        var dist = (ownerObj.transform.position - target.transform.position).sqrMagnitude;

                        Vector3 toTargetFlat = VectorOperation.GetFlatVector(target.transform.position - transform.position + posOffset * Vector3.up);
                        Vector3 toTargetVertical = new Vector3(0, target.transform.position.y + posOffset, 0) - new Vector3(0, transform.position.y, 0);


                        var horSpeed = Vector3.MoveTowards(speed,
                           (target.transform.position - transform.position + posOffset * Vector3.up).normalized * currSpeed,
                            Time.deltaTime * (currSpeed + originSpeed) * redirectFactor);

                        var vertSpeed = Vector3.MoveTowards(Vector3.up * body.velocity.y,
                           toTargetVertical.y < 0f ? toTargetVertical.normalized * Physics.gravity.magnitude : Vector3.zero,
                            Time.deltaTime * (currSpeed + originSpeed) * redirectFactor);

                        //speed = horSpeed + vertSpeed;
                        speed = horSpeed;
                        body.velocity = speed;

                        //physics
                        //body.AddForce((toTargetFlat + toTargetVertical).normalized * currSpeed * Time.deltaTime, ForceMode.Acceleration);
                    }
                }
                break;
            case State.NoOwner:
                speed += Physics.gravity * Time.deltaTime;
                body.velocity = speed;

                //***  Was used to respawn the ball under the ground
                if (IsServer && transform.position.y < -20f)
                {
                    int ownerNb = -1;
                    for (int i = 0; i < match.Players.Length; i++)
                    {
                        if (match.Players[i].OwnerClientId == owner)
                            ownerNb = i;
                    }
                    enabled = false;
                    match.TryInstantiateBall(ownerNb, true, increment);
                }
                break;
            case State.Idle:
                var debug = endIdle - System.DateTime.UtcNow.Ticks;
                var lapse = endIdle - startIdleTime;

                float avancement = (float)(System.DateTime.UtcNow.Ticks - startIdleTime) / (float)lapse;

                Debug.Log($"Ball, Update : endIdle - Now: {Mathf.Round(debug / 10000f)}ms, avancement : {avancement}");

                var maxPitch = 1f;
                var minPitch = 0.05f;
                var maxFilter = 22000f;
                var minFilter = 100f;

                //  __________
                //      try put impact sound at the end of slowmo
                //      try stop sounds while slow mo then restart
                //  __________

                if (avancement < .5f)
                {
                    var percent = avancement / .5f;
                    var processPercent = Mathf.Pow(percent, .25f);
                    Debug.Log($"Ball, Update : pitch down = {slowMoAudioSource.pitch}, processPercent {processPercent}");
                    slowMoAudioSource.pitch = Mathf.Lerp(maxPitch, minPitch, processPercent);
                    lowPass.cutoffFrequency = Mathf.Lerp(maxFilter, minFilter, processPercent);
                    //ownerObj.AudioSource.pitch = Mathf.Lerp(maxPitch, minPitch, processPercent);
                    //ownerObj.LowPass.cutoffFrequency = Mathf.Lerp(maxFilter, minFilter, processPercent);
                }

                if (avancement >= .5f)
                {
                    var percent = (avancement - .5f) / .5f;
                    var processPercent = Mathf.Pow(percent, 4f);
                    Debug.Log($"Ball, Update : pitch up = {slowMoAudioSource.pitch}, processPercent {processPercent}");
                    slowMoAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, processPercent);
                    lowPass.cutoffFrequency = Mathf.Lerp(minFilter, maxFilter, processPercent);
                    //ownerObj.AudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, processPercent);
                    //ownerObj.LowPass.cutoffFrequency = Mathf.Lerp(minFilter, maxFilter, processPercent);
                }




                if (endIdle < System.DateTime.UtcNow.Ticks)
                {
                    //ownerObj.AudioSource.pitch = 1f;
                    //ownerObj.AudioSource.Play();
                    //ownerObj.LowPass.enabled = false;
                    lowPass.enabled = false;
                    body.isKinematic = false;
                    state = endIdleState;
                    body.velocity = speed;
                    desync = false;
                    onIdleOverEvent.Invoke();
                    onIdleOverEvent.RemoveAllListeners();
                    slowMoAudioSource.Stop();


                    if (IsServer && endIdleVfx)
                        PlayBallHitClientRpc();
                }
                break;
            default:
                break;

        }
        if (IsServer)
            SynchronizeBallClientRpc(transform.position, speed);

        loopAudioSource.pitch = minPitch + (maxPitch - minPitch) * (currSpeed / maxSpeed);


        if (state == State.NoOwner) return;

        var cols = Physics.OverlapSphere(transform.position, .3f, playerMask);
        CheckColision(cols);
    }

    protected virtual bool CheckColision(Collider[] cols)
    {
        var res = false;
        foreach (var col in cols)
        {
            var pl = col.GetComponent<Player>();

            if (pl && pl.IsOwner && !victims.Contains(pl) && pl != ownerObj)
            {
                pl.GetHit(currSpeed * damageMult);
                victims.Add(pl);
                res = true;
            }
        }
        return res;
    }

    [ClientRpc]
    private void PlayBallHitClientRpc()
    {
        PlayBallHit();
    }

    /*private void OnCollisionEnter(Collision collision)
    {
        *//*private void OnTriggerEnter(Collider other)
        {*//*
        //if (!IsServer) return;
        Debug.Log("Ball, OnColisionEnter : collider = " + collision.collider);
        CheckCollision(collision);
    }

    protected virtual void CheckCollision(Collision collision)
    {
        if (state == State.NoOwner) return;

        var pl = collision.collider.GetComponent<Player>();

        if (pl && pl.IsOwner && !victims.Contains(pl))
        {
            if (pl == ownerObj) return;
            pl.GetHit(currSpeed * damageMult);
            victims.Add(pl);
        }
    }*/

    internal void SetUpBall(MatchManager matchManager, int target, int increment)
    {
        body = GetComponent<Rigidbody>();
        match = matchManager;
        selfCollider = GetComponent<SphereCollider>();
        floorCollider = match.FloorCollider;
        if (increment != 0)
        {
            this.increment = increment;
            currSpeed = 10f;
        }

        IgnoreFloor(true);
        ChangeOwner(target);
        //ChangeOwnerClientRpc(target);

        SetUpBallClientRpc(match.Players[target].OwnerClientId, target, increment);
    }

    [ClientRpc]
    internal void SetUpBallClientRpc(ulong ownerClientDd, int target, int increment, float speed = 0)
    {
        body = GetComponent<Rigidbody>();
        match = FindObjectOfType<MatchManager>();
        selfCollider = GetComponent<SphereCollider>();
        floorCollider = match.FloorCollider;
        if (increment != 0)
        {
            increment--;
            this.increment = increment;
            if (speed == 0)
                FindNewSpeed();
            else
                currSpeed = speed;
        }

        IgnoreFloor(true);
        ChangeOwner(target);
        //ChangeOwnerClientRpc(target);
    }

    internal void ChangeOwner(int nb)
    {
        victims.Clear();
        if (state == State.NoOwner)
            IgnoreFloor(true);

        target = match.Players[nb == 0 ? 1 : 0];
        owner = match.Players[nb].OwnerClientId;
        ownerObj = match.Players[nb];

        if (IsServer && !IsHost) return;

        ChangeColors(ownerObj.IsOwner ? selfColor : oppsColor);

    }


    internal void RemoveOwner(int nb)
    {
        victims.Clear();
        state = State.NoOwner;
        IgnoreFloor(false);
        target = null;
        ownerObj = null;
        owner = match.Players[nb].OwnerClientId;

        if (IsServer && !IsHost) return;

        ChangeColors(noOwnerColor);

    }
    private void ChangeColors(Color color)
    {
        /*foreach (var prtc in particles)
        {
            var colorOverLifeTime = prtc.colorOverLifetime;
            colorOverLifeTime.color = new ParticleSystem.MinMaxGradient(color, new Color(1f, 1f, 1f, 0f));
        }*/
        renderer.material.SetColor("_EmissiveColor", color);
        //renderer.material.color = color;
        spriteFloorTarget.color = color;
        match.SetSeeThroughColor(color);
        trail.material.SetColor("_Color01", color);
    }

    protected void IgnoreFloor(bool value)
    {
        Physics.IgnoreCollision(selfCollider, floorCollider, value);
    }

    public virtual bool TrySpecialBall(Player owner, Vector3 hitDirection)
    {
        //if (this.ownerObj == owner) return false;
        this.ownerObj = null;
        target = null;
        this.owner = owner.OwnerClientId;

        desync = true;
        //SetBallLobServerRpc(this.owner, owner.NetworkObjectId, Time.time, transform.position, (VectorOperation.GetFlatVector(hitDirection).normalized + 4f * Vector3.up).normalized);
        //audioSource.PlayOneShot(lobClip);

        return true;
    }
    public virtual bool TryHitBall(Player owner, Vector3 hitDirection)
    {
        //if (this.ownerObj == owner) return false;
        this.owner = owner.OwnerClientId;
        this.ownerObj = owner;
        target = owner == match.Players[0] ? match.Players[1] : match.Players[0];

        desync = true;
        SetIdleForOwner();

        SetBallHitServerRpc(this.owner, owner.NetworkObjectId, Time.time, transform.position, hitDirection);
        audioSource.PlayOneShot(hitclip);

        return true;
    }
    public virtual bool TryLobBall(Player owner, Vector3 hitDirection, float lobSpeed)
    {
        //if (this.ownerObj == owner) return false;
        this.ownerObj = null;
        target = null;
        this.owner = owner.OwnerClientId;

        desync = true;
        SetBallLobServerRpc(this.owner, owner.NetworkObjectId, Time.time, transform.position, hitDirection, lobSpeed);
        audioSource.PlayOneShot(lobClip);

        return true;
    }
    public virtual bool TrySmashBall(Player owner, Vector3 hitDirection)
    {
        //if (this.ownerObj == owner) return false;
        this.owner = owner.OwnerClientId;
        this.ownerObj = owner;
        target = owner == match.Players[0] ? match.Players[1] : match.Players[0];

        desync = true;
        SetIdleForOwner();

        SetBallSmashServerRpc(this.owner, owner.NetworkObjectId, Time.time, transform.position, hitDirection);
        audioSource.PlayOneShot(smashClip);

        return true;
    }

    private void SetIdleForOwner()
    {
        //state = State.Idle;
        body.velocity = Vector3.zero;
        body.isKinematic = true;
        //startIdle = Time.time;
        //idleTime = Mathf.Infinity;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetBallHitServerRpc(ulong owner, ulong ownerPlayerObject, float timestamp, Vector3 oldPos, Vector3 direction, float idleTime = -1f)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 0 : 1;
        ChangeOwner(nb);

        FindNewSpeed();

        var dist = (match.Players[0].transform.position - match.Players[1].transform.position).sqrMagnitude;

        transform.position = oldPos;

        this.direction = direction;
        speed = currSpeed * direction;

        SetBallHitClientRpc(this.owner, currSpeed, direction, speed);

        if (idleTime < 0f)
            SetIdleTime();
        else
            SetFixedIdleTime(idleTime);

        SetIdleTimeClientRpc(endIdle, idleTime, startIdleTime);

        //transform.position = GetPredictedPosition(timestamp, oldPos, speed);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetBallLobServerRpc(ulong owner, ulong ownerPlayerObject, float timestamp, Vector3 oldPos, Vector3 direction, float lobSpeed)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 0 : 1;
        RemoveOwner(nb);


        currSpeed = lobSpeed;

        var dist = (match.Players[0].transform.position - match.Players[1].transform.position).sqrMagnitude;

        this.direction = direction;
        speed = currSpeed * direction;

        SetBallLobClientRpc(this.owner, currSpeed, direction, speed);

        //transform.position = GetPredictedPosition(timestamp, oldPos, speed);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetBallSmashServerRpc(ulong owner, ulong ownerPlayerObject, float timestamp, Vector3 oldPos, Vector3 direction)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 0 : 1;
        ChangeOwner(nb);

        FindNewSmashSpeed();

        var dist = (match.Players[0].transform.position - match.Players[1].transform.position).sqrMagnitude;

        transform.position = oldPos;

        this.direction = direction;
        speed = currSpeed * direction;


        SetBallSmashClientRpc(this.owner, currSpeed, direction, speed);

        SetIdleTime();
        SetIdleTimeClientRpc(endIdle, idleTime, startIdleTime);

        //transform.position = GetPredictedPosition(timestamp, oldPos, speed);
    }

    [ClientRpc]
    private void SetIdleTimeClientRpc(long endIdle, float idleTime, long startIdleTime, State endIdleState = State.Normal)
    {
        body.velocity = Vector3.zero;
        this.endIdleState = endIdleState;
        state = State.Idle;
        body.isKinematic = true;
        lowPass.enabled = true;
        this.endIdle = endIdle;
        this.startIdleTime = startIdleTime;
        this.idleTime = idleTime;

        //Debug.Log($"Ball, SetIdleTime : start {new DateTime(startIdleTime).Second}:{new DateTime(startIdleTime).Millisecond}s");
        //Debug.Log($"Ball, SetIdleTime : end {new DateTime(endIdle).AddSeconds(idleTime).Second}:{new DateTime(endIdle).AddSeconds(idleTime).Millisecond}s");

        slowMoAudioSource.Play();
    }

    private void SetIdleTime(bool endIdleVfx = true)
    {
        this.endIdleVfx = endIdleVfx;
        body.velocity = Vector3.zero;
        state = State.Idle;
        body.isKinematic = true;
        //ownerObj.AudioSource.Stop();

        idleTime = (currSpeed / maxSpeed) * .5f + 0.4f;

        startIdleTime = System.DateTime.UtcNow.Ticks;
        endIdle = System.DateTime.UtcNow.AddSeconds(idleTime).Ticks;

        //Debug.Log($"Ball, SetIdleTime : start {System.DateTime.UtcNow.Second}:{System.DateTime.UtcNow.Millisecond}s");
        //Debug.Log($"Ball, SetIdleTime : end {System.DateTime.UtcNow.AddSeconds(idleTime).Second}:{System.DateTime.UtcNow.AddSeconds(idleTime).Millisecond}s");
    }
    [ServerRpc]
    public void RequestStopSpeedServerRpc()
    {
        body.velocity = Vector3.zero;
        StopSpeedClientRpc();
    }
    [ClientRpc]
    public void StopSpeedClientRpc()
    {
        body.velocity = Vector3.zero;
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestIdleTimeServerRpc(float idleTime, bool endIdleVfx = true, State endIdleState = State.Normal)
    {

        SetFixedIdleTime(idleTime, endIdleVfx, endIdleState);
        SetIdleTimeClientRpc(endIdle, idleTime, startIdleTime, endIdleState);
    }
    public void SetFixedIdleTime(float time, bool endIdleVfx = true, State endIdleState = State.Normal)
    {
        this.endIdleVfx = endIdleVfx;
        this.endIdleState = endIdleState;
        body.velocity = Vector3.zero;
        state = State.Idle;
        body.isKinematic = true;
        //ownerObj.AudioSource.Stop();

        startIdleTime = DateTime.UtcNow.Ticks;
        endIdle = DateTime.UtcNow.AddSeconds(time).Ticks;

        //Debug.Log($"Ball, SetIdleTime : start {DateTime.UtcNow.Second}:{DateTime.UtcNow.Millisecond}s");
        //Debug.Log($"Ball, SetIdleTime : end {DateTime.UtcNow.AddSeconds(time).Second}:{DateTime.UtcNow.AddSeconds(time).Millisecond}s");
    }

    private void FindNewSpeed()
    {
        increment++;
        currSpeed = Mathf.Min(1f, (Mathf.Log(increment + 1f) / 3.5f)) * (maxSpeed - originSpeed) + originSpeed;
        //Debug.Log("Ball, FindNewSpeed : currSpeed = " + currSpeed + ", increment = " + increment);
    }
    private void FindNewSmashSpeed()
    {
        increment++;
        currSpeed = Mathf.Min(1f, (Mathf.Log(increment + 1f) / 3.5f)) * (maxSpeed * 1.5f - originSpeed) + originSpeed;
        //Debug.Log("Ball, FindNewSmashSpeed : currSpeed = " + currSpeed + ", increment = " + increment);
    }

    private static Vector3 GetPredictedPosition(float timestamp, Vector3 oldPos, Vector3 speed)
    {
        return oldPos + (Time.time - timestamp) * speed * Time.deltaTime;
    }

    [ClientRpc]
    public void ChangeOwnerClientRpc(int nb)
    {
        ChangeOwner(nb);
    }
    [ClientRpc]
    private void SetBallHitClientRpc(ulong owner, float currSpeed, Vector3 direction, Vector3 speed)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 0 : 1;

        this.currSpeed = currSpeed;
        this.direction = direction;
        this.speed = speed;

        ChangeOwner(nb);
        nextClip = hitclip;
    }

    private void PlayBallHit()
    {
        foreach (var prtc in particlesInst)
            prtc.Play();

        if (!this.ownerObj.IsOwner)
            audioSource.PlayOneShot(nextClip);
    }

    [ClientRpc]
    private void SetBallLobClientRpc(ulong owner, float currSpeed, Vector3 direction, Vector3 speed)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 0 : 1;

        this.currSpeed = currSpeed;
        this.direction = direction;
        this.speed = speed;

        RemoveOwner(nb);
        desync = false;
        PlayLobSfx(nb);
    }

    private void PlayLobSfx(int nb)
    {
        foreach (var prtc in particlesInst)
            prtc.Play();

        if (match.Players[nb].IsOwner)
            audioSource.PlayOneShot(lobClip);
    }

    [ClientRpc]
    private void SetBallSmashClientRpc(ulong owner, float currSpeed, Vector3 direction, Vector3 speed)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 0 : 1;

        this.currSpeed = currSpeed;
        this.direction = direction;
        this.speed = speed;

        ChangeOwner(nb);
        nextClip = smashClip;
    }


    [ClientRpc]
    private void SynchronizeBallClientRpc(Vector3 position, Vector3 speed)
    {
        if (desync) return;
        /*if ((transform.position - position).magnitude > 2f)
            transform.position = position;
        else*/
        transform.position = Vector3.MoveTowards(transform.position, position, Time.deltaTime * .1f);

        /*if ((this.speed - speed).magnitude > .5f)
            this.speed = speed;
        else*/
        this.speed = Vector3.MoveTowards(this.speed, speed, Time.deltaTime * .1f);
    }
    [ClientRpc]
    private void ForceSynchronizeBallClientRpc(Vector3 position, Vector3 speed, float currspeed = 0f)
    {
        transform.position = position;
        this.speed = speed;
        if (currspeed != 0)
            this.currSpeed = currspeed;
    }
    internal List<FakeBall> CreateFakeBall(int nb, BallOffset[] offsets)
    {
        List<FakeBall> balls = new List<FakeBall>();
        for (int i = 0; i < nb; i++)
        {
            var ballobj = Instantiate(fakeBallPrefab, transform.position, transform.rotation, null);
            ballobj.GetComponent<NetworkObject>().Spawn();
            var fakeBall = ballobj.GetComponent<FakeBall>();

            var playerNb = owner == Match.Players[0].OwnerClientId ? 0 : 1;

            fakeBall.SetUpBall(Match, playerNb, increment);
            fakeBall.SetUpOffset(offsets[i]);
            fakeBall.ForceSynchronizeBallClientRpc(fakeBall.transform.position, fakeBall.speed, fakeBall.currSpeed);
            balls.Add(fakeBall);
            Match.FakeBalls.AddRange(balls);
        }
        return balls;
    }

}
