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
        Control,
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
    [HideInInspector] public UnityEvent OffensiveEnabledEvent = new UnityEvent();
    [HideInInspector] public UnityEvent OffensiveDisabledEvent = new UnityEvent();
    [HideInInspector] public UnityEvent DefensiveEnabledEvent = new UnityEvent();
    [HideInInspector] public UnityEvent DefensiveDisabledEvent = new UnityEvent();
    [HideInInspector] public UnityEvent ControlEnabledEvent = new UnityEvent();
    [HideInInspector] public UnityEvent ControlDisabledEvent = new UnityEvent();

    public Player Owner { get => owner; set => owner = value; }

    public void StopAllEffects()
    {
        effects.ForEach(e => e.Reinit());
    }
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

        var ball = other.GetComponentInParent<Ball>();
        var player = other.GetComponent<Player>() ;

        if (ball && !touched.Contains(ball) && ball.State1 != Ball.State.Idle)
        {
            touched.Add(ball);
            switch (state)
            {
                case BallState.Normal:
                    if (owner.Controller.Grounded || owner.Controller.WallLeft || owner.Controller.WallRight)
                        owner.Combat.HitBallDetected(ball);
                    else
                        owner.Combat.SmashBallDetected(ball);
                    break;
                case BallState.Offensive:
                    owner.Combat.SpecialOffensiveBallDetected(ball);
                    break;
                case BallState.Defensive:
                    owner.Combat.SpecialDefensiveBallDetected(ball);
                    break;
                case BallState.Control:
                    owner.Combat.ControlBall(ball);
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
            case BallState.Control:
                if (v)
                    ControlEnabledEvent.Invoke();
                else
                    ControlDisabledEvent.Invoke();
                break;
            default:
                break;
        }
    }
}
