using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

enum Action
{
    Strike,
    Lob,
    Offensive,
    Defensive,
    Move,
    Nothing
}
public class NinjaCombat : PlayerCombat
{
    [Header("Ninja stuff")]
    [SerializeField] WeaponCollider weapon;
    [SerializeField] ParticleSystem smokeCloud;
    [SerializeField] NinjaAnimatorController animator;
    [SerializeField] ParticleSystem mirageParticles;
    [SerializeField] SkinnedMeshRenderer meshRenderer;
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

    private void Start()
    {
        animator = GetComponent<NinjaAnimatorController>();
        inputs.StartAttackEvent.AddListener(delegate () { nextMove = Action.Strike; });
        inputs.StartLobEvent.AddListener(delegate () { nextMove = Action.Lob; });
        inputs.StartOffensiveEvent.AddListener(delegate () { nextMove = Action.Offensive; });
        inputs.StartDefensiveEvent.AddListener(delegate () { nextMove = Action.Defensive; });
        inputs.StartMoveEvent.AddListener(delegate () { nextMove = Action.Nothing; });
        specialOffensive.OnValueChanged += PlaySpecialOffSound;
    }

    private void PlaySpecialOffSound(bool previousValue, bool newValue)
    {
        if (newValue)
            player.AudioSource.PlayOneShot(ultiStartClip);
    }

    protected override void SetLobSettings()
    {
        LobSettings = new LobSettings()
        {
            Speed = 8f,
            Direction = (hitDirection) => { return (VectorOperation.GetFlatVector(hitDirection).normalized + 4f * Vector3.up).normalized; }
        };
    }
    internal override void SpecialDefensiveMove(Ball ball)
    {
        // Smoke teleport to the air whit with ball
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

    internal override void SpecialOffensiveMove(Ball ball)
    {
        AskInstanciateFakeBallsServerRpc(ball.NetworkObjectId, player.hitDirection);
    }

    [ServerRpc]
    private void AskInstanciateFakeBallsServerRpc(ulong networkObjectId, Vector3 direction)
    {
        var ball = GetNetworkObject(networkObjectId).GetComponent<Ball>();

        ball.TryHitBall(player, direction);

        weapon.StartEffect(3);

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
        new BallOffset{ PositionOffset = Vector3.right, SpeedOffset = isRight ? rotatedR * ball.Speed.magnitude * .33f : rotatedL * ball.Speed.magnitude * .33f},
        new BallOffset{ PositionOffset = Vector3.right, SpeedOffset = isRight ? rotatedL * ball.Speed.magnitude * .67f : rotatedR * ball.Speed.magnitude * .67f}
        });

            balls.ForEach(b => weapon.AddTouchedBall(b));
        });

    }
    IEnumerator WaitToStopCheckBall()
    {
        var endTime = Time.time + dashTime;
        while (Time.time < endTime)
        {
            var ball = DetectBall();
            if (ball)
            {
                switch (nextMove)
                {
                    case Action.Strike:
                    case Action.Offensive:
                        if (player.Controller.Grounded)
                            ball.TryHitBall(player, player.hitDirection);
                        else if (!player.Controller.Grounded)
                            ball.TrySmashBall(player, player.hitDirection);
                        break;
                    case Action.Lob:
                    case Action.Defensive:
                        ball.TryLobBall(player, player.GetLobDirection(), player.GetLobSpeed());
                        break;
                    default:
                    case Action.Move:
                    case Action.Nothing:
                        //ball.RequestIdleTimeServerRpc(slowtime, false, Ball.State.NoOwner);
                        break;
                }
                /*animator.OnBallTouchedDuringMoveAction();
                player.Controller.enabled = true;*/
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        animator.OnBallTouchedDuringMoveAction();
    }


    public override void PerformMoveAction()
    {
        if (player.Ball)
            body.velocity = (player.Ball.transform.position - transform.position).normalized * speed;
        else
            body.velocity = player.transform.forward * speed;

        Moving.Value = false;
        StartCoroutine(WaitToStopCheckBall());
        PlayMirageParticles(true);
        PlayMirageParticlesServerRpc(true);
    }
    [ServerRpc]
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
    /*protected override bool CheckIfCanMoveAction()
    {
        return base.CheckIfCanMoveAction() && ;
    }*/
}
