using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KnightCombat : PlayerCombat
{
    [SerializeField] WeaponCollider baseWeaponCollider;
    [SerializeField] WeaponCollider weapon;
    [SerializeField] GameObject holyWeapon;
    [SerializeField] PlayerAnimationEvent playerEvent;

    [SerializeField] List<ParticleSystem> enableHolyWeaponParticles;
    [SerializeField] List<ParticleSystem> disableHolyWeaponParticles;


    private void Start()
    {
        //Time.timeScale = .2f;
        weapon = GetComponentInChildren<WeaponCollider>();
        specialDefensive.OnValueChanged += StopSpecialDefenseMoveVFX;
        playerEvent.OffensiveEnabledEvent.AddListener(delegate { EnableHolyWeapon(true); });
        playerEvent.OffensiveDisabledEvent.AddListener(delegate { EnableHolyWeapon(false); });
    }

    private void EnableHolyWeapon(bool value)
    {
        holyWeapon.SetActive(value);
        if(value)
            enableHolyWeaponParticles.ForEach(p => p.Play());
        else
        {
            enableHolyWeaponParticles.ForEach(p => p.Stop());
        }
    }

    public override void PerformMoveAction()
    {
        throw new System.NotImplementedException();
    }

    private void FixedUpdate()
    {
        if (specialDefensive.Value)
            if (player.CanSpecialMove(false))
                RequestDefensiveMoveServerRpc();
            else
            {
                StopSpecialDefenseMoveVFX(true, false);
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

    public void StopSpecialDefenseMoveVFX(bool old, bool current)
    {
        if (!current)
            weapon.StopEffect(1);
    }

    internal override void SpecialDefensiveMove(Ball ball)
    {
        weapon.StopEffect(1);
    }

    internal override void SpecialOffensiveMove(Ball ball)
    {
        if (ball.TrySmashBall(player, player.hitDirection))
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
