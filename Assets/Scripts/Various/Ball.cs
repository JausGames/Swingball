using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Ball : NetworkBehaviour
{
    internal enum State
    {
        Normal,
        NoOwner,
        Idle,
    }

    MatchManager match;
    private bool desync;
    [SerializeField] private Vector3 direction;
    [SerializeField] private Vector3 speed;
    [SerializeField] private State state = State.Normal;

    [Header("Network")]
    [SerializeField] private ulong owner;
    [SerializeField] private OnlinePlayer ownerObj;
    [SerializeField] OnlinePlayer target;
    [SerializeField] private List<OnlinePlayer> victims;
    [Header("Colors")]
    [SerializeField] Color noOwnerColor;
    [SerializeField] Color selfColor;
    [SerializeField] Color oppsColor;
    [Header("Component")]
    [SerializeField] List<ParticleSystem> particlesInst;
    [SerializeField] List<ParticleSystem> particles;
    [SerializeField] TrailRenderer trail;
    [SerializeField] MeshRenderer renderer;
    [SerializeField] SpriteRenderer spriteFloorTarget;
    [SerializeField] Collider selfCollider;
    [SerializeField] Collider floorCollider;
    [Header("Stats")]
    [SerializeField] private float damageMult = 5f;
    [SerializeField] private float originSpeed = 10f;
    [SerializeField] private float currSpeed = 10f;
    [SerializeField] private int increment = 1;
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float redirectFactor = 1f;
    [SerializeField] private float posOffset = 1f;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource loopAudioSource;
    [SerializeField] private AudioSource slowMoAudioSource;
    [SerializeField] private AudioLowPassFilter lowPass;
    [SerializeField] private AudioClip  hitclip;
    [SerializeField] private AudioClip  lobClip;
    [SerializeField] private AudioClip  smashClip;

    private UnityEvent onIdleOverEvent = new UnityEvent();
    private Rigidbody body;
    private float lobSpeed = 8f;
    private float minPitch = .8f;
    private float maxPitch = 2f;
    private float idleTime;
    private long startIdleTime;
    [SerializeField] private long endIdle;

    public MatchManager Match { get => match; set => match = value; }
    public Rigidbody Body { get => body; set => body = value; }
    public Collider FloorCollider { get => floorCollider; set => floorCollider = value; }
    public UnityEvent OnIdleOverEvent { get => onIdleOverEvent; set => onIdleOverEvent = value; }
    internal State State1 { get => state; set => state = value; }

    // Update is called once per frame
    void Update()
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
                if (IsServer && transform.position.y < -1f)
                {
                    int ownerNb = -1;
                    for(int i = 0; i < match.Players.Length; i++)
                    {
                        if (match.Players[i].OwnerClientId != owner)
                            ownerNb = i;
                    }
                    enabled = false;
                    match.TryInstantiateBallServerRpc(ownerNb, true, increment);
                }
                break;
            case State.Idle:
                Debug.Log($"Ball, Update : startIdleTime {startIdleTime}, endIdle {endIdle}, Now { System.DateTime.Now.Ticks}");
                Debug.Log($"Ball, Update : endIdle - Now {endIdle - System.DateTime.Now.Ticks}");
                var lapse = endIdle - startIdleTime;

                float avancement = (float)(System.DateTime.Now.Ticks - startIdleTime) / (float)lapse;

                var maxPitch = 1f;
                var minPitch = 0.05f;
                var maxFilter = 22000f;
                var minFilter = 120f;

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




                if (endIdle < System.DateTime.Now.Ticks)
                {
                    ownerObj.AudioSource.pitch = 1f;
                    ownerObj.AudioSource.Play();
                    ownerObj.LowPass.enabled = false;
                    lowPass.enabled = false;
                    body.isKinematic = false;
                    state = State.Normal;
                    body.velocity = speed;
                    desync = false;
                    onIdleOverEvent.Invoke();
                    slowMoAudioSource.Stop();
                }
                break;
            default:
                break;

        }
        if (IsServer)
            SynchroniseBallClientRpc(transform.position, speed);

        loopAudioSource.pitch = minPitch + (maxPitch - minPitch) * (currSpeed / maxSpeed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        /*private void OnTriggerEnter(Collider other)
        {*/
        //if (!IsServer) return;
        Debug.Log("Ball, OnColisionEnter : collider = " + collision.collider);

        if (state == State.NoOwner) return;

        var pl = collision.collider.GetComponent<OnlinePlayer>();

        if (pl && pl.IsOwner && !victims.Contains(pl))
        {
            Debug.Log("Ball, OnColisionEnter : pl = " + pl);
            Debug.Log("Ball, OnColisionEnter : pl.IsOwner = " + pl.IsOwner);
            if (pl == ownerObj) return;
            Debug.Log("Ball, OnColisionEnter : ownerObj = " + ownerObj);
            pl.GetHit(currSpeed * damageMult);
            victims.Add(pl);
        }
    }

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
    internal void SetUpBallClientRpc(ulong ownerClientDd, int target, int increment)
    {
        body = GetComponent<Rigidbody>();
        match = FindObjectOfType<MatchManager>();
        selfCollider = GetComponent<SphereCollider>();
        floorCollider = match.FloorCollider;
        if(increment != 0)
        {
            increment--;
            this.increment = increment;
            FindNewSpeed();
        }

        IgnoreFloor(true);
        ChangeOwner(target);
        //ChangeOwnerClientRpc(target);
    }

    internal void ChangeOwner(int nb)
    {
        Debug.Log("SetBallHitServerRpc, ChangeOwner : nb = " + nb);
        victims.Clear();
        if (state == State.NoOwner)
            IgnoreFloor(true);

        target = match.Players[nb == 0 ? 1 : 0];
        owner = match.Players[nb].OwnerClientId;
        ownerObj = match.Players[nb];

        if (IsServer && !IsHost) return;

        var color = match.Players[nb].IsOwner ? selfColor : oppsColor;
        foreach(var prtc in particles)
        {
            var colorOverLifeTime = prtc.colorOverLifetime;
            colorOverLifeTime.color = new ParticleSystem.MinMaxGradient(color, new Color(1f,1f,1f,0f));
        }
        renderer.material.color = color;
        spriteFloorTarget.color= color;
        trail.material.SetColor("_Color01", color);

    }
    internal void RemoveOwner(int nb)
    {
        Debug.Log("SetBallHitServerRpc, RemoveOwner");
        victims.Clear();
        state = State.NoOwner;
        IgnoreFloor(false);
        target = null;
        ownerObj = null;
        owner = match.Players[nb].OwnerClientId;

        if (IsServer && !IsHost) return;

        var color = noOwnerColor;
        foreach (var prtc in particles)
        {
            var colorOverLifeTime = prtc.colorOverLifetime;
            colorOverLifeTime.color = new ParticleSystem.MinMaxGradient(color, new Color(1f, 1f, 1f, 0f));
        }
        renderer.material.color = color;
        spriteFloorTarget.color = color;
        trail.material.SetColor("_Color01", color);

    }

    private void IgnoreFloor(bool value)
    {
        Debug.Log("SetBallHitServerRpc, IgnoreFloor : value = " + value);
        Physics.IgnoreCollision(selfCollider, floorCollider, value);
    }

    internal bool TryHitBall(OnlinePlayer owner, Vector3 hitDirection)
    {
        if (this.ownerObj == owner) return false;
        this.owner = owner.OwnerClientId;
        this.ownerObj = owner;
        target = owner == match.Players[0] ? match.Players[1] : match.Players[0];

        desync = true;
        SetIdleForOwner();

        SetBallHitServerRpc(this.owner, owner.NetworkObjectId, Time.time, transform.position, hitDirection);
        audioSource.PlayOneShot(hitclip);

        return true;
    }
    internal bool TryLobBall(OnlinePlayer owner, Vector3 hitDirection)
    {
        if (this.ownerObj == owner) return false;
        this.ownerObj = null;
        target = null;
        this.owner = owner.OwnerClientId;

        desync = true;
        SetBallLobServerRpc(this.owner, owner.NetworkObjectId, Time.time, transform.position, (VectorOperation.GetFlatVector(hitDirection).normalized + 4f * Vector3.up).normalized);
        audioSource.PlayOneShot(lobClip);

        return true;
    }
    internal bool TrySmashBall(OnlinePlayer owner, Vector3 hitDirection)
    {
        if (this.ownerObj == owner) return false;
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
    private void SetBallHitServerRpc(ulong owner, ulong ownerPlayerObject, float timestamp, Vector3 oldPos, Vector3 direction)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 1 : 0;
        ChangeOwner(nb);

        FindNewSpeed();

        Debug.Log("SetBallHitServerRpc, Ball : owner = " + owner);
        Debug.Log("SetBallHitServerRpc, Ball : target = " + target.OwnerClientId);

        var dist = (match.Players[0].transform.position - match.Players[1].transform.position).sqrMagnitude;

        transform.position = oldPos;

        this.direction = direction;
        speed = currSpeed * direction;

        SetBallHitClientRpc(this.owner, currSpeed, direction, speed);

        SetIdleTime();
        SetIdleTimeClientRpc(endIdle, idleTime, startIdleTime);

        //transform.position = GetPredictedPosition(timestamp, oldPos, speed);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetBallLobServerRpc(ulong owner, ulong ownerPlayerObject, float timestamp, Vector3 oldPos, Vector3 direction)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 1 : 0;
        RemoveOwner(nb);

        SetLobSpeed();

        Debug.Log("SetBallLobServerRpc, Ball : owner = " + owner);
        //Debug.Log("SetBallLobServerRpc, Ball : target = " + target.OwnerClientId);

        var dist = (match.Players[0].transform.position - match.Players[1].transform.position).sqrMagnitude;

        this.direction = direction;
        speed = currSpeed * direction; 

        SetBallLobClientRpc(this.owner, currSpeed, direction, speed);

        //transform.position = GetPredictedPosition(timestamp, oldPos, speed);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetBallSmashServerRpc(ulong owner, ulong ownerPlayerObject, float timestamp, Vector3 oldPos, Vector3 direction)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 1 : 0;
        ChangeOwner(nb);

        FindNewSmashSpeed();

        Debug.Log("SetBallHitServerRpc, Ball : owner = " + owner);
        Debug.Log("SetBallHitServerRpc, Ball : target = " + target.OwnerClientId);

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
    private void SetIdleTimeClientRpc(long endIdle, float idleTime, long startIdleTime)
    {
        body.velocity = Vector3.zero;
        state = State.Idle;
        body.isKinematic = true;
        lowPass.enabled = true;
        this.endIdle = endIdle;
        this.startIdleTime = startIdleTime;
        this.idleTime = idleTime;

        Debug.Log($"Ball, SetIdleTimeClientRpc : start {startIdleTime}, end {endIdle}");

        slowMoAudioSource.Play();
    }

    private void SetIdleTime()
    {
        body.velocity = Vector3.zero;
        state = State.Idle;
        body.isKinematic = true;
        ownerObj.AudioSource.Stop();

        idleTime = (currSpeed / maxSpeed) * .5f;

        startIdleTime = System.DateTime.Now.Ticks;
        endIdle = System.DateTime.Now.AddSeconds(idleTime).Ticks;
    }

    private void FindNewSpeed()
    {
        increment++;
        currSpeed = Mathf.Min(1f, (Mathf.Log(increment + 1f) / 3.5f)) * (maxSpeed - originSpeed) + originSpeed;
        Debug.Log("Ball, FindNewSpeed : currSpeed = " + currSpeed + ", increment = " + increment);
    }
    private void SetLobSpeed()
    {
        currSpeed = lobSpeed;
    }
    private void FindNewSmashSpeed()
    {
        increment++;
        currSpeed = Mathf.Min(1f, (Mathf.Log(increment + 1f) / 3.5f)) * (maxSpeed * 1.5f - originSpeed) + originSpeed;
        Debug.Log("Ball, FindNewSmashSpeed : currSpeed = " + currSpeed + ", increment = " + increment);
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
        var nb = owner == match.Players[0].OwnerClientId ? 1 : 0;

        this.currSpeed = currSpeed;
        this.direction = direction;
        this.speed = speed;

        ChangeOwner(nb);
        foreach(var prtc in particlesInst)
            prtc.Play();

        if(!this.ownerObj.IsOwner)
            audioSource.PlayOneShot(hitclip);
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
        foreach (var prtc in particlesInst)
            prtc.Play();

        if (match.Players[nb].IsOwner)
            audioSource.PlayOneShot(lobClip);
    }

    [ClientRpc]
    private void SetBallSmashClientRpc(ulong owner, float currSpeed, Vector3 direction, Vector3 speed)
    {
        var nb = owner == match.Players[0].OwnerClientId ? 1 : 0;

        this.currSpeed = currSpeed;
        this.direction = direction;
        this.speed = speed;

        ChangeOwner(nb);
        foreach (var prtc in particlesInst)
            prtc.Play();

        if (!this.ownerObj.IsOwner)
            audioSource.PlayOneShot(smashClip);
    }


    [ClientRpc]
    private void SynchroniseBallClientRpc(Vector3 position, Vector3 speed)
    {
        if (desync) return;
        if ((transform.position - position).magnitude > 1.5f)
            transform.position = position;
        else
            transform.position = Vector3.MoveTowards(transform.position, position, Time.deltaTime * 5f);

        if ((this.speed - speed).magnitude > 1.5f)
            this.speed = speed;
        else
            this.speed = Vector3.MoveTowards(this.speed, speed, Time.deltaTime * 5f);
    }
}
