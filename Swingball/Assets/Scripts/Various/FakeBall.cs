using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

internal class BallOffset
{
    private bool letPassOnce = true;
    public Vector3 SpeedOffset { get; set; }
    public Vector3 PositionOffset { get; set; }
}

public class FakeBall : Ball
{
    internal void SetUpOffset(BallOffset offset)
    {
        transform.position += offset.PositionOffset;
        speed = offset.SpeedOffset;
        currSpeed = offset.SpeedOffset.magnitude;
    }
    protected override bool CheckColision(Collider[] cols)
    {
        var res = base.CheckColision(cols);
        if (res)
        {
            AskForDespawnServerRpc();
            renderer.enabled = false;
        }
        return res;
    }
    public override bool TryHitBall(Player owner, Vector3 hitDirection)
    {
        if (this.OwnerObj == owner) return false;
        AskForDespawnServerRpc();
        renderer.enabled = false;
        return false;
    }
    public override bool TryLobBall(Player owner, Vector3 hitDirection, float lobspeed)
    {
        if (this.OwnerObj == owner) return false;
        AskForDespawnServerRpc();
        renderer.enabled = false;
        return false;
    }
    public override bool TrySmashBall(Player owner, Vector3 hitDirection)
    {
        if (this.OwnerObj == owner) return false;
        AskForDespawnServerRpc();
        renderer.enabled = false;
        return false;
    }
    public override bool TrySpecialBall(Player owner, Vector3 hitDirection)
    {
        if (this.OwnerObj == owner) return false;
        AskForDespawnServerRpc();
        renderer.enabled = false;
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AskForDespawnServerRpc()
    {
        if(this.GetComponent<NetworkObject>() && this.GetComponent<NetworkObject>().IsSpawned)
        {
            renderer.enabled = false;
            PlayDeadParticlesClientRpc();
            StartCoroutine(WaitToKill());
        }
    }

    IEnumerator WaitToKill()
    {
        yield return new WaitForSeconds(.2f);
        GetNetworkObject(NetworkObjectId).Despawn();
    }

    public override void OnNetworkDespawn()
    {
        Match.FakeBalls.Remove(this);
        base.OnNetworkDespawn();
    }

    [ClientRpc]
    public void PlayDeadParticlesClientRpc()
    {
        particlesInst.ForEach(p =>
        {
            p.transform.parent = null;
            p.Play();
            Destroy(p.gameObject, .4f);
        });
        renderer.enabled = false;
    }
}
