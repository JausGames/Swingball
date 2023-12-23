using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaAnimatorController : PlayerAnimatorController
{
    public void OnBallTouchedDuringMoveAction()
    {
        if (!combat.IsOwner) return;
        controller.Body.velocity = controller.Body.velocity.normalized * 4f;
        ((NinjaCombat)combat).PlayMirageParticles(false);
        ((NinjaCombat)combat).Moving.Value = false;
        ((NinjaCombat)combat).PlayMirageParticlesServerRpc(false);
    }
}
