using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KnightCombat : PlayerCombat
{
    [SerializeField] private float trapUpwardForce = 8f;
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

    [SerializeField] float currentSpeedModifier = 1f;

    private void Start()
    {
        //Time.timeScale = .2f;
        DefensiveMove.OnValueChanged += EnableSpecialDefenseMoveVFX;

        playerEvent.OffensiveEnabledEvent.AddListener(delegate { EnableHolyWeapon(true); });
        playerEvent.OffensiveDisabledEvent.AddListener(delegate { EnableHolyWeapon(false); });
        playerEvent.DefensiveEnabledEvent.AddListener(delegate { EnableHolyTrap(true); });
        playerEvent.DefensiveDisabledEvent.AddListener(delegate { EnableHolyTrap(false); });
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

    protected override void SetLobSettings()
    {
        LobSettings = new LobSettings()
        {
            Speed = 6f,
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


    public void EnableSpecialDefenseMoveVFX(bool old, bool current)
    {
        if (current)
            showTrapZoneParticles.ForEach(p => p.Play());
        //holyTrapCollider.gameObject.SetActive(true);
    }

    internal override void SpecialDefensiveMove(Ball ball)
    {
        //baseWeaponCollider.StopEffect(1);
    }

    internal override void SpecialOffensiveMove(Ball ball)
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
