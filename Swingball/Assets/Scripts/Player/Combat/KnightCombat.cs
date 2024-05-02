using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KnightCombat : PlayerCombat
{
    [SerializeField] private float trapUpwardForce = 12f;
    [SerializeField] private float trapTime = .8f;

    [SerializeField] WeaponCollider baseWeaponCollider;
    [SerializeField] WeaponCollider holyWeaponCollider;
    [SerializeField] WeaponCollider holyTrapCollider;

    [SerializeField] GameObject baseWeapon;
    [SerializeField] GameObject holyWeapon;
    [SerializeField] PlayerAnimationEvent playerEvent;

    [SerializeField] List<ParticleSystem> invicibleParticles;

    [SerializeField] List<ParticleSystem> enableHolyWeaponParticles;
    [SerializeField] List<ParticleSystem> enableHolyTrapParticles;

    [SerializeField] List<ParticleSystem> showTrapZoneParticles;

    [SerializeField] List<ParticleSystem> parryParticles;

    [SerializeField] float currentSpeedModifier = 1f;
    private KnightAnimatorController animator;
    private float startGuardTime = 0f;
    private float parryTime = .2f;

    public bool Blocking { get; private set; }

    private void Start()
    {
        animator = GetComponentInChildren<KnightAnimatorController>();
        DefensiveMove.OnValueChanged += EnableSpecialDefenseMoveVFX;

        playerEvent.OffensiveEnabledEvent.AddListener(delegate { EnableHolyWeapon(true); });
        playerEvent.OffensiveDisabledEvent.AddListener(delegate { EnableHolyWeapon(false); });

        playerEvent.DefensiveEnabledEvent.AddListener(delegate { EnableHolyTrap(true); });
        playerEvent.DefensiveDisabledEvent.AddListener(delegate { EnableHolyTrap(false); });

        WeaponColliders[3].ControlEnabledEvent.AddListener(delegate { startGuardTime = Time.time + parryTime; parryParticles.ForEach(p => p.Play()); });
        WeaponColliders[3].ControlDisabledEvent.AddListener(delegate { parryParticles.ForEach(p => p.Stop()); });
    }

    public override void ResetActions()
    {
        base.ResetActions();
        baseWeaponCollider.StopAllEffects();
        holyWeaponCollider.StopAllEffects();
        holyTrapCollider.StopAllEffects();

        invicibleParticles.ForEach(p => p.Stop());
        enableHolyWeaponParticles.ForEach(p => p.Stop());
        enableHolyTrapParticles.ForEach(p => p.Stop());
        showTrapZoneParticles.ForEach(p => p.Stop());
        parryParticles.ForEach(p => p.Stop());

        animator.ToIdle();
    }
    private void EnableHolyTrap(bool value)
    {
        baseWeapon.SetActive(!value);

        if (value)
            enableHolyTrapParticles.ForEach(p => p.Play());
        else
        {
            enableHolyTrapParticles.ForEach(p => p.Stop());
        }
    }


    private void EnableHolyWeapon(bool value)
    {
        holyWeapon.SetActive(value);
        baseWeapon.SetActive(!value);

        if (value)
            enableHolyWeaponParticles.ForEach(p => p.Play());
        else
        {
            enableHolyWeaponParticles.ForEach(p => p.Stop());
        }
    }

    public override void PerformMoveAction()
    {
        if (IsOwner)
            StartCoroutine(InvicibleTime());
    }

    private IEnumerator InvicibleTime()
    {
        player.Invincible = true;
        PlayInvicibleEffectServerRpc();
        invicibleParticles.ForEach(p => p.Play());
        yield return new WaitForSeconds(0.5f);
        player.Invincible = false;
    }
    [ServerRpc]
    private void PlayInvicibleEffectServerRpc()
    {
        PlayInvicibleEffectClientRpc();
    }

    [ClientRpc]
    private void PlayInvicibleEffectClientRpc()
    {
        if (!IsOwner)
            invicibleParticles.ForEach(p => p.Play());
    }

    private void FixedUpdate()
    {
        if (IsOwner)
            if (specialDefensive.Value)
                if (player.CanSpecialMove(false))
                    RequestDefensiveMoveServerRpc();
                else
                {
                    EnableSpecialDefenseMoveVFX(true, false);
                    specialDefensive.Value = false;
                }
    }

    [ServerRpc]
    private void RequestDefensiveMoveServerRpc()
    {
        player.RemoveSpecialPoints(defensiveMoveValue);
    }

    protected override void SetControlSettings()
    {
        LobSettings = new ControlSettings()
        {
            Speed = 7f,
            Direction = (hitDirection) => { return (VectorOperation.GetFlatVector(hitDirection).normalized).normalized; }
        };
    }

    public override void PlayerTouched(Player player, WeaponCollider.BallState state)
    {
        Debug.Log("PlayerTouched");
        if (state == WeaponCollider.BallState.Defensive)
            EjectPlayerServerRpc(player.NetworkObjectId);
    }

    [ServerRpc]
    void EjectPlayerServerRpc(ulong networkObjectId)
    {
        Debug.Log("PlayerTouched : EjectPlayerServerRpc");
        EjectPlayerClientRpc(networkObjectId);
    }
    [ClientRpc]
    void EjectPlayerClientRpc(ulong networkObjectId)
    {
        Debug.Log("PlayerTouched : EjectPlayerClientRpc");
        var player = GetNetworkObject(networkObjectId).GetComponent<Player>();
        if (player && player.IsOwner)
        {

            Debug.Log("PlayerTouched : EjectPlayerClientRpc is owner");
            player.Controller.Body.velocity = Vector3.up * trapUpwardForce;
            player.SetFall(trapTime);
        }
    }

    internal void PerformCounter(Ball ball)
    {
        if (ball.TryHitBall(player, -ball.Direction, 1.2f))
        {
            player.SetSlowMo(true);
            ball.OnIdleOverEvent.AddListener(delegate
            {
                player.SetSlowMo(false);
                ball.OnIdleOverEvent.RemoveAllListeners();
            });
        }
    }

    public void EnableSpecialDefenseMoveVFX(bool old, bool current)
    {
        if (current)
            showTrapZoneParticles.ForEach(p => p.Play());
        //holyTrapCollider.gameObject.SetActive(true);
    }

    internal override void PerformSpecialDefensive(Ball ball)
    {
    }

    public override void ControlBall(Ball ball)
    {
        if (startGuardTime > Time.time)
        {
            animator.Parry();
            PerformCounter(ball);
        }
        else
        {
            animator.Block();
            ball.TryLobBall(player, player.GetLobDirection(), player.GetLobSpeed());
        }
    }
    internal override void ControlMove(Ball ball)
    {
    }

    internal override void PerformSpecialOffensive(Ball ball)
    {
        // Calculer une direction sympa pour envoyer haut mais vers la target
        if (ball.TryHitBall(player, (VectorOperation.GetFlatVector(ball.GetDir()) + Vector3.up * 10f).normalized, 3f))
        {
            player.SetSlowMo(true);
            ball.OnIdleOverEvent.AddListener(delegate
            {
                player.SetSlowMo(false);
                ball.OnIdleOverEvent.RemoveAllListeners();
            });
        }
    }
}
