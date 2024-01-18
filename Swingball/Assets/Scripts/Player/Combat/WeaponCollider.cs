using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

// Make sure the order is later than your LateUpdate script, say IK system, you can check the order at Edit->ProejectSettings->ScriptExecutionOrder
//[DefaultExecutionOrder(11000)]
public class WeaponCollider : MonoBehaviour
{
    internal enum State
    {
        Normal,
        Offensive,
        Defensive,
        Lob,
        Fire,
    }

    [SerializeField] LayerMask layermask;
    [SerializeField] float damage = 100f;
    [SerializeField] List<ParticleSystem> particles = new List<ParticleSystem>();
    [SerializeField] List<VisualEffect> effects = new List<VisualEffect>();
    [SerializeField] Player owner;
    [SerializeField] private bool isActive = false;
    [SerializeField] private List<Ball> touched = new List<Ball>();
    private State state;
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

        if (ball && !touched.Contains(ball) && ball.State1 != Ball.State.Idle)
        {
            touched.Add(ball);
            switch (state)
            {
                case State.Normal:
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
                case State.Offensive:
                    if ((owner.Controller.Grounded || owner.Controller.WallLeft || owner.Controller.WallRight)
                        && ball.TrySpecialBall(owner, owner.hitDirection))
                    {
                        owner.SpecialOffensiveMove(ball);
                    }
                    break;
                case State.Defensive:
                    if ((owner.Controller.Grounded || owner.Controller.WallLeft || owner.Controller.WallRight)
                        && ball.TrySpecialBall(owner, owner.hitDirection))
                    {
                        owner.SpecialDefensiveMove(ball);
                    }
                    break;
                case State.Lob:
                    var dir = owner.GetLobDirection();
                    var speed = owner.GetLobSpeed();
                    ball.TryLobBall(owner, owner.GetLobDirection(), owner.GetLobSpeed());
                    break;
                default:
                    break;
            }
        }
    }

    internal void AddTouchedBall(Ball b)
    {
        touched.Add(b);
    }

    internal void IsActive(bool v, State state = State.Normal, int slashEffectNb = -1)
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

        switch (state)
        {
            case State.Offensive:
                if (v)
                    OffensiveEnabledEvent.Invoke();
                else
                    OffensiveDisabledEvent.Invoke();
                break;
            case State.Defensive:
                if (v)
                    OffensiveEnabledEvent.Invoke();
                else
                    DefensiveDisabledEvent.Invoke();
                break;
            case State.Normal:
            case State.Lob:
            default:
                break;
        }
    }
}
