using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public class TrainingBall : Ball
{
    private bool canHit;

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
    protected override Player ChangeOwner(int nb)
    {
        canHit = false;
        victims.Clear();
        if (state == State.NoOwner)
            IgnoreFloor(true);

        target = match.Players[0];
        owner = match.Players[0].OwnerClientId;
        //ownerObj = match.Players[0];

        ChangeColors(oppsColor);

        StartCoroutine(WaitToHit());

        return target;
    }
    IEnumerator WaitToHit()
    {
        yield return new WaitForSeconds(1f);
        canHit = true;
    }
    protected override bool CheckColision(Collider[] cols)
    {
        if (!canHit) return false;
        return base.CheckColision(cols);
    }
    internal void SetUpOffset(BallOffset offset)
    {
        transform.position += offset.PositionOffset;
        speed = offset.SpeedOffset;
        currSpeed = offset.SpeedOffset.magnitude;
    }


}
