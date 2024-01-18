using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableController : StateMachineBehaviour
{
    private PlayerCombat combat;
    private PlayerController controller;
    [SerializeField] bool disableCombat = false;
    [SerializeField] bool disableController = false;
    [SerializeField] bool atStart = true;
    [SerializeField] bool atEnd = true;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        combat = animator.GetComponentInParent<PlayerCombat>();
        controller = animator.GetComponentInParent<PlayerController>();
        if (!atStart) return;
        if (disableCombat) combat.enabled = true;
        if (disableController) controller.SetEnabled(true);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!atEnd) return;
        if (disableCombat) combat.enabled = true;
        if (disableController) controller.SetEnabled(true);
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
