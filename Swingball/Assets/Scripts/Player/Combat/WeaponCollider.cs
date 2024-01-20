using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

// Make sure the order is later than your LateUpdate script, say IK system, you can check the order at Edit->ProejectSettings->ScriptExecutionOrder
//[DefaultExecutionOrder(11000)]
public class WeaponCollider : MonoBehaviour
{
    public enum BallState
    {
        Normal,
        Offensive,
        Defensive,
        Lob,
    }

    [SerializeField] LayerMask layermask;
    [SerializeField] float damage = 100f;
    [SerializeField] List<ParticleSystem> particles = new List<ParticleSystem>();
    [SerializeField] List<VisualEffect> effects = new List<VisualEffect>();
    [SerializeField] Player owner;
    [SerializeField] private bool isActive = false;
    [SerializeField] private List<Ball> touched = new List<Ball>();
    [SerializeField] private List<Player> playerTouched = new List<Player>();
    private BallState state;
    public UnityEvent OffensiveEnabledEvent = new UnityEvent();
    public UnityEvent OffensiveDisabledEvent = new UnityEvent();
    public UnityEvent DefensiveEnabledEvent = new UnityEvent();
    public UnityEvent DefensiveDisabledEvent = new UnityEvent();

    public Player Owner { get => owner; set => owner = value; }

    public void StopEffect(int id)
    {
        if (effects.Count > id)
            effects[id].Reinit();
    }
    public void StartEffect(int id)
    {
        if (effects.Count > id)
            effects[id].Play();
    }

    private void Awake()
    {
        owner = GetComponentInParent<Player>();
    }

    private void LateUpdate()
    {
        foreach (var prtl in particles)
        {
            //prtl.Simulate(Time.deltaTime, true, false, false);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        TryHitCollider(other);
    }
    private void OnTriggerEnter(Collider other)
    {
        TryHitCollider(other);
    }

    private void TryHitCollider(Collider other)
    {
        if (!isActive || !owner.IsOwner) return;

        var isBall = layermask == (layermask | (1 << other.gameObject.layer));

        if (!isBall) return;

        /* var victim = other.GetComponentInParent<OnlinePlayer>();

         if(victim && victim != owner && !touched.Contains(victim))
         {
             victim.GetHit(damage);
             touched.Add(victim);
         }*/

        var ball = other.GetComponentInParent<Ball>();
        var player = other.GetComponent<Player>() ;

        if (ball && !touched.Contains(ball) && ball.State1 != Ball.State.Idle)
        {
            touched.Add(ball);
            switch (state)
            {
                case BallState.Normal:
                    if (owner.Controller.Grounded || owner.Controller.WallLeft || owner.Controller.WallRight)
                    {
                        if (ball.TryHitBall(owner, owner.hitDirection))
                        {
                            owner.SetSlowMo(true);
                            ball.OnIdleOverEvent.AddListener(delegate
                            {
                                owner.SetSlowMo(false);
                                owner.SubmitAddSpecialRequestServerRpc(ball.Speed.magnitude);
                                ball.OnIdleOverEvent.RemoveAllListeners();
                            });
                        }
                    }
                    else
                    {
                        if (ball.TrySmashBall(owner, owner.hitDirection))
                        {
                            owner.SetSlowMo(true);
                            ball.OnIdleOverEvent.AddListener(delegate
                            {
                                owner.SetSlowMo(false);
                                owner.SubmitAddSpecialRequestServerRpc(ball.Speed.magnitude);
                                ball.OnIdleOverEvent.RemoveAllListeners();
                            });
                        }
                    }

                    break;
                case BallState.Offensive:
                    if ((owner.Controller.Grounded || owner.Controller.WallLeft || owner.Controller.WallRight)
                        && ball.TrySpecialBall(owner, owner.hitDirection))
                    {
                        owner.SpecialOffensiveMove(ball);
                    }
                    break;
                case BallState.Defensive:
                    if ((owner.Controller.Grounded || owner.Controller.WallLeft || owner.Controller.WallRight)
                        && ball.TrySpecialBall(owner, owner.hitDirection))
                    {
                        owner.SpecialDefensiveMove(ball);
                    }
                    break;
                case BallState.Lob:
                    var dir = owner.GetLobDirection();
                    var speed = owner.GetLobSpeed();
                    ball.TryLobBall(owner, owner.GetLobDirection(), owner.GetLobSpeed());
                    break;
                default:
                    break;
            }
        }

        if(player && player != owner && !playerTouched.Contains(player))
        {
            owner.PlayerTouched(player, state);
            playerTouched.Add(player);
        }
    }

    internal void AddTouchedBall(Ball b)
    {
        touched.Add(b);
    }

    internal void IsActive(bool v, BallState state = BallState.Normal, int slashEffectNb = -1)
    {
        isActive = v;
        this.state = state;
        if (!v) foreach (var particle in particles) particle.Stop();
        else
        {
            if (slashEffectNb >= 0 && effects.Count > slashEffectNb) effects[slashEffectNb].Play();
            foreach (var particle in particles) particle.Play();
        }
        if (v) touched.Clear();
        if (v) playerTouched.Clear();

        switch (state)
        {
            case BallState.Offensive:
                if (v)
                    OffensiveEnabledEvent.Invoke();
                else
                    OffensiveDisabledEvent.Invoke();
                break;
            case BallState.Defensive:
                if (v)
                    DefensiveEnabledEvent.Invoke();
                else
                    DefensiveDisabledEvent.Invoke();
                break;
            case BallState.Normal:
            case BallState.Lob:
            default:
                break;
        }
    }
}
