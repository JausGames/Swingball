using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;
using System.Collections.Generic;

enum Action
{
    Strike,
    Lob,
    Offensive,
    Defensive,
    Move,
    Nothing
}
public class ControlSettings
{
    public float Speed;
    public Func<Vector3, Vector3> Direction;
}
public abstract class PlayerCombat : NetworkBehaviour
{
    [SerializeField] protected Player player;
    [SerializeField] protected Rigidbody body;
    protected OnlinePlayerInputs inputs;
    [SerializeField] protected AudioSource source;
    [SerializeField] protected List<WeaponCollider> weaponColliders = new List<WeaponCollider>();

    [SerializeField] protected NetworkVariable<bool> specialDefensive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] protected NetworkVariable<bool> specialOffensive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] protected NetworkVariable<bool> attacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] protected NetworkVariable<bool> lobbing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] protected NetworkVariable<bool> moving = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> Attacking { get => attacking; set => attacking = value; }
    public NetworkVariable<bool> Lobbing { get => lobbing; set => lobbing = value; }
    [SerializeField] private ControlSettings lobSettings;
    public NetworkVariable<bool> DefensiveMove { get => specialDefensive; set => specialDefensive = value; }
    public NetworkVariable<bool> OffensiveMove { get => specialOffensive; set => specialOffensive = value; }
    public float DefensiveMoveValue { get => defensiveMoveValue; set => defensiveMoveValue = value; }
    public float OffensiveMoveValue { get => offensiveMoveValue; set => offensiveMoveValue = value; }
    public ControlSettings LobSettings { get => lobSettings; set => lobSettings = value; }
    public NetworkVariable<bool> Moving { get => moving; set => moving = value; }
    public float NextMoveAction { get => nextMoveAction; }
    public float MoveActionCooldown { get => moveActionCooldown; }
    public Player Player { get => player; set => player = value; }
    public List<WeaponCollider> WeaponColliders { get => weaponColliders; internal set => weaponColliders = value; }

    [Header("Values")]
    [SerializeField] protected float defensiveMoveValue = 40f;
    [SerializeField] protected float offensiveMoveValue = 60f;
    [SerializeField] float moveActionCooldown = 2f;
    float nextMoveAction;

    private void Awake()
    {
        inputs = GetComponent<OnlinePlayerInputs>();
        SetControlSettings();
        inputs.StartAttackEvent.AddListener(delegate () { Attack(true); });
        inputs.StopAttackEvent.AddListener(delegate () { Attack(false); });

        inputs.StartLobEvent.AddListener(delegate () { Control(true); });
        inputs.StopLobEvent.AddListener(delegate () { Control(false); });

        inputs.StartOffensiveEvent.AddListener(delegate () { SpecialOffensive(true); });
        inputs.StopOffensiveEvent.AddListener(delegate () { SpecialOffensive(false); });

        inputs.StartDefensiveEvent.AddListener(delegate () { SpecialDefensive(true); });
        inputs.StopDefensiveEvent.AddListener(delegate () { SpecialDefensive(false); });

        inputs.StartMoveEvent.AddListener(delegate () { Move(true); });
        inputs.StopMoveEvent.AddListener(delegate () { Move(false); });
    }



    protected abstract void SetControlSettings();
    #region Attack base
    public void Attack(bool Attacking)
    {
        if (IsOwner && enabled)
        {
            Debug.Log("attacking : " + Attacking);
            SetAttacking(Attacking);
        }
    }

    public void SetAttacking(bool Attacking)
    {
        this.attacking.Value = Attacking;
    }
    public bool GetAttacking()
    {
        return enabled ? attacking.Value : false;
    }

    internal void SmashBallDetected(Ball ball)
    {
        if (ball.TrySmashBall(player, player.hitDirection))
        {
            player.SetSlowMo(true);
            ball.OnIdleOverEvent.AddListener(delegate
            {
                player.SetSlowMo(false);
                player.SubmitAddSpecialRequestServerRpc(ball.Speed.magnitude);
                ball.OnIdleOverEvent.RemoveAllListeners();
            });
        }
    }
    internal void HitBallDetected(Ball ball)
    {
        if (ball.TryHitBall(player, player.hitDirection))
        {
            player.SetSlowMo(true);
            ball.OnIdleOverEvent.AddListener(delegate
            {
                player.SetSlowMo(false);
                player.SubmitAddSpecialRequestServerRpc(ball.Speed.magnitude);
                ball.OnIdleOverEvent.RemoveAllListeners();
            });
        }
    }

    #endregion
    #region Special defensif
    public void SpecialDefensive(bool performed)
    {
        if (IsOwner && player.CanSpecialMove(false) && enabled)
        {
            SetSpecialDefensive(performed);
        }
    }

    public void SetSpecialDefensive(bool perfomed)
    {
        this.specialDefensive.Value = perfomed;
    }
    public bool GetSpecialDefensive()
    {
        return enabled ? specialDefensive.Value : false;
    }
    internal abstract void PerformSpecialDefensive(Ball ball);
    internal virtual void SpecialDefensiveBallDetected(Ball ball)
    {
        if ((player.Controller.Grounded || player.Controller.WallLeft || player.Controller.WallRight)
            && ball.TrySpecialBall(player, player.hitDirection))
        {
            PerformSpecialDefensive(ball);
        }
    }
    #endregion
    #region Special offensif
    public void SpecialOffensive(bool performed)
    {
        if (IsOwner && player.CanSpecialMove(true) && enabled)
        {
            SetSpecialOffensive(performed);
        }
    }
    internal void SpecialOffensiveBallDetected(Ball ball)
    {
        if ((player.Controller.Grounded || player.Controller.WallLeft || player.Controller.WallRight)
            && ball.TrySpecialBall(player, player.hitDirection))
        {
            PerformSpecialOffensive(ball);
        }
    }

    internal abstract void PerformSpecialOffensive(Ball ball);


    public void SetSpecialOffensive(bool perfomed)
    {
        this.specialOffensive.Value = perfomed;
    }
    public bool GetSpecialOffensive()
    {
        return enabled ? specialOffensive.Value : false;
    }
    #endregion
    #region Control
    public virtual void ControlBall(Ball ball)
    {
        if ((player.Controller.Grounded || player.Controller.WallLeft || player.Controller.WallRight)
            && ball.TryLobBall(player, player.GetLobDirection(), player.GetLobSpeed()))
        {
            ControlMove(ball);
        }
    }
    public void Control(bool lobbing)
    {
        if (IsOwner && enabled)
        {
            SetLobbing(lobbing);
            /*if (lobbing)
                StartCoroutine(ResetLobBoolIfInavailable());*/
        }
    }
    public void SetLobbing(bool lobbing)
    {
        this.lobbing.Value = lobbing;
    }
    public bool GetLobbing()
    {
        return enabled ? lobbing.Value : false;
    }
    internal abstract void ControlMove(Ball ball);
    #endregion
    #region Move
    public void Move(bool moving)
    {
        if (IsOwner)
        {
            if (moving && CheckIfCanMoveAction())
            {
                nextMoveAction = Time.time + moveActionCooldown;
                SetMoving(true);
            }
            else if (!moving)
                SetMoving(false);
        }
    }

    public virtual void PlayerTouched(Player player, WeaponCollider.BallState state)
    {
    }


    protected virtual bool CheckIfCanMoveAction()
    {
        return nextMoveAction <= Time.time && enabled;
    }

    public virtual void ResetActions()
    {
        if (IsOwner)
        {
            Lobbing.Value = false;
            DefensiveMove.Value = false;
            OffensiveMove.Value = false;
            Attacking.Value = false;
            Moving.Value = false;
        }
    }

    public void SetMoving(bool moving)
    {
        this.moving.Value = moving;
    }
    public bool GetMoving()
    {
        return enabled ? moving.Value : false;
    }
    public abstract void PerformMoveAction();
    #endregion
}
