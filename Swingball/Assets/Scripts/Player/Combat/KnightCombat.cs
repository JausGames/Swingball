using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KnightCombat : PlayerCombat
{
    [SerializeField] WeaponCollider weapon;

    private void Start()
    {
        //Time.timeScale = .2f;
        weapon = GetComponentInChildren<WeaponCollider>();
        specialDefensive.OnValueChanged += StopSpecialDefenseMoveVFX;
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
