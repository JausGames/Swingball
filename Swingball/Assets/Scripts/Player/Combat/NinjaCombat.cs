using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class NinjaCombat : PlayerCombat
{
    [Header("Ninja stuff")]
    [SerializeField] WeaponCollider weapon;
    [SerializeField] ParticleSystem smokeCloud;
    [SerializeField] NinjaAnimatorController animator;
    [SerializeField] ParticleSystem mirageParticles;
    [SerializeField] SkinnedMeshRenderer meshRenderer;
    [SerializeField] VisualEffect lightningVFX;
    [Header("Defensive move")]
    [SerializeField] float floatTime = .4f;
    [Header("Move action")]
    [SerializeField] float speed = 14f;
    float slowtime = .5f;
    [SerializeField] float dashTime = .5f;
    [SerializeField] LayerMask ballLayer;
    [SerializeField] AudioClip mirageClip;
    [SerializeField] AudioClip ultiStartClip;
    [SerializeField] private Action nextMove = Action.Nothing;
    private bool isInMirage;

    public bool IsInMirage { get => isInMirage; 
        set 
        { 
            isInMirage = value;
            PlayMirageParticles(value);
            PlayMirageParticlesServerRpc(value);
            player.Controller.enabled = !value;
        } 
    }

    //private bool isInMirage;

    private void Start()
    {
        animator = GetComponent<NinjaAnimatorController>();
        inputs.StartAttackEvent.AddListener(delegate () { nextMove = Action.Strike; });
        inputs.StartLobEvent.AddListener(delegate () { nextMove = Action.Lob; });
        inputs.StartOffensiveEvent.AddListener(delegate () { nextMove = Action.Offensive; });
        inputs.StartDefensiveEvent.AddListener(delegate () { nextMove = Action.Defensive; });
        inputs.StartMoveEvent.AddListener(delegate () { nextMove = Action.Nothing; });
        specialOffensive.OnValueChanged += PlaySpecialOffSound;

        player.GetHitEvent.AddListener(delegate
        {
            if (isInMirage)
            {
                Move(false);
                IsInMirage = false;
            };
        });
    }

    public override void ResetActions()
    {
        base.ResetActions();
        IsInMirage = false;
        weapon.StopAllEffects();
        animator.ToIdle();
    }

    private void PlaySpecialOffSound(bool previousValue, bool newValue)
    {
        if (newValue)
            player.AudioSource.PlayOneShot(ultiStartClip);
    }

    protected override void SetControlSettings()
    {
        LobSettings = new ControlSettings()
        {
            Speed = 8f,
            Direction = (hitDirection) => { return (VectorOperation.GetFlatVector(hitDirection).normalized + 4f * Vector3.up).normalized; }
        };
    }
    internal override void PerformSpecialDefensive(Ball ball)
    {
        // Smoke teleport to the air with ball
        smokeCloud.Play();
        RequestDefensiveMoveServerRpc();
        ball.TryLobBall(player, player.GetLobDirection(), player.GetLobSpeed());
    }

    [ServerRpc]
    private void RequestDefensiveMoveServerRpc()
    {
        player.RemoveSpecialPoints(defensiveMoveValue);
        RequestDefensiveMoveClientRpc();
    }
    [ClientRpc]
    private void RequestDefensiveMoveClientRpc()
    {
        smokeCloud.Play();
        if (IsOwner)
            transform.position += Vector3.up * 2f;

        StartCoroutine(StayWithNoGravity());
    }

    IEnumerator StayWithNoGravity()
    {
        player.Controller.NoGravity = true;
        body.velocity = body.velocity - body.velocity.y * Vector3.up;
        yield return new WaitForSecondsRealtime(floatTime);
        player.Controller.NoGravity = false;
    }

    internal override void PerformSpecialOffensive(Ball ball)
    {
        AskInstanciateFakeBallsServerRpc(ball.NetworkObjectId, player.hitDirection);
    }

    [ServerRpc]
    private void AskInstanciateFakeBallsServerRpc(ulong networkObjectId, Vector3 direction)
    {
        var ball = GetNetworkObject(networkObjectId).GetComponent<Ball>();

        ball.TryHitBall(player, direction);

        lightningVFX.Play();

        player.SetSlowMo(true);
        ball.OnIdleOverEvent.AddListener(delegate
        {
            player.SetSlowMo(false);
            ball.OnIdleOverEvent.RemoveAllListeners();
        });

        ball.OnIdleOverEvent.AddListener(delegate
        {
            var flat = VectorOperation.GetFlatVector(direction);
            Vector3 rotatedR = Quaternion.AngleAxis(45f, Vector3.up) * flat + Vector3.up * direction.y;
            Vector3 rotatedL = Quaternion.AngleAxis(-45f, Vector3.up) * flat + Vector3.up * direction.y;

            var isRight = UnityEngine.Random.Range(0, 2) == 0;

            var balls = ball.CreateFakeBall(2, new BallOffset[] {
        new BallOffset{ PositionOffset = -transform.right * 2f, SpeedOffset = isRight ? rotatedR * ball.Speed.magnitude * .33f : rotatedL * ball.Speed.magnitude * .33f},
        new BallOffset{ PositionOffset = transform.right * 2f, SpeedOffset = isRight ? rotatedL * ball.Speed.magnitude * .67f : rotatedR * ball.Speed.magnitude * .67f}
        });

            balls.ForEach(b => weapon.AddTouchedBall(b));
        });

    }

    public override void PerformMoveAction()
    {
        if (player.Ball)
            body.velocity = (player.Ball.transform.position - transform.position).normalized * speed;
        else
            body.velocity = player.transform.forward * speed;

        Moving.Value = false;
        StartCoroutine(WaitToStopCheckBall());
    }
    IEnumerator WaitToStopCheckBall()
    {
        var endTime = Time.time + dashTime;
        IsInMirage = true;
        while (Time.time < endTime && isInMirage)
        {
            var ball = DetectBall();
            if (ball)
            {
                switch (nextMove)
                {
                    case Action.Strike:
                        HitBallDetected(ball);
                        IsInMirage = false;
                        animator.OnBallTouchedDuringMoveAction();
                        break;
                    case Action.Lob:
                    case Action.Defensive:
                        ControlBall(ball);
                        IsInMirage = false;
                        animator.OnBallTouchedDuringMoveAction();
                        break;
                    default:
                    case Action.Move:
                    case Action.Nothing:
                        break;
                }
            }
            yield return new WaitForEndOfFrame();
        }
        IsInMirage = false;
    }
    [ServerRpc(RequireOwnership = false)]
    public void PlayMirageParticlesServerRpc(bool play)
    {
        PlayMirageParticlesClientRpc(play);
    }
    [ClientRpc]
    private void PlayMirageParticlesClientRpc(bool play)
    {
        if (!IsOwner)
            PlayMirageParticles(play);
    }

    public void PlayMirageParticles(bool play)
    {
        if (play)
        {
            meshRenderer.enabled = false;
            mirageParticles.Play();
            source.PlayOneShot(mirageClip);
        }
        else
        {
            player.transform.eulerAngles = new Vector3(0f, player.transform.eulerAngles.y, 0f);
            meshRenderer.enabled = true;
            mirageParticles.Stop();
        }
    }

    Ball DetectBall()
    {
        var cols = Physics.OverlapSphere(transform.position, 1.2f, ballLayer);
        if (cols.Length > 0)
        {
            foreach (var col in cols)
            {
                var ball = col.GetComponent<Ball>();
                if (ball) return ball;
            }
        }
        return null;
    }

    internal override void ControlMove(Ball ball)
    {
    }
    /*protected override bool CheckIfCanMoveAction()
{
   return base.CheckIfCanMoveAction() && ;
}*/
}
