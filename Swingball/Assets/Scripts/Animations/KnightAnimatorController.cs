using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightAnimatorController : PlayerAnimatorController
{

    internal void Parry()
    {
        animator.SetTrigger("Parry");
        //animator.ResetTrigger("Block");
       // ResetTrigger("Parry");
    }
    internal void Block()
    {
        animator.SetTrigger("Block");
       // animator.SetTrigger("Parry");
        //ResetTrigger("Block");
    }

    IEnumerator ResetTrigger(string action)
    {
        yield return new WaitForSeconds(.1f);
        animator.ResetTrigger(action);
    }
}
