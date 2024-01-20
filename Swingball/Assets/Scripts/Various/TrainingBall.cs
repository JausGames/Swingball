using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public class TrainingBall : Ball
{
    private void Start()
    {
        ChangeColors(oppsColor);
    }
    protected override float GetDist()
    {
        return 30f;
    }
    public override Vector3 GetDir()
    {
        return match.Players[0].hitDirection;
    }
    protected override void FindNewTargetLocally(Player owner)
    {
        this.owner = owner.OwnerClientId;
        //this.ownerObj = owner;
        target = match.Players[0];
    }
    protected override void ChangeOwner(int nb)
    {
        victims.Clear();
        if (state == State.NoOwner)
            IgnoreFloor(true);

        target = match.Players[0];
        owner = match.Players[0].OwnerClientId;
        //ownerObj = match.Players[0];

        ChangeColors(oppsColor);
    }

    [ServerRpc(RequireOwnership = false)]
    protected override void SetBallHitServerRpc(ulong owner, ulong ownerPlayerObject, float timestamp, Vector3 oldPos, Vector3 direction, float idleTime = -1, float speedMultiplier = 1f)
    {

        ChangeOwner(0);
        FindNewSpeed(speedMultiplier);

        transform.position = oldPos;

        this.direction = direction;
        speed = currSpeed * direction;

        SetBallHitClientRpc(this.owner, currSpeed, direction, speed);

        if (idleTime < 0f)
            SetIdleTime();
        else
            SetFixedIdleTime(idleTime);

        SetIdleTimeClientRpc(endIdle, idleTime, startIdleTime);
    }
    internal void SetUpOffset(BallOffset offset)
    {
        transform.position += offset.PositionOffset;
        speed = offset.SpeedOffset;
        currSpeed = offset.SpeedOffset.magnitude;
    }


}
