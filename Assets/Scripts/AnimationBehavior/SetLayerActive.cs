using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetLayerActive : StateMachineBehaviour
{
    [SerializeField] int[] layersNb = { 2 };
    [SerializeField] bool onStart = true;
    [SerializeField] bool onStay = false;
    [SerializeField] bool onEnd = false;
    // OnStateEnter is called when a tr
    // ansition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (onStart)
            foreach (int nb in layersNb)
                animator.SetLayerWeight(nb, 1f);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        /*if (onStay)
            foreach (int nb in layersNb)
            {
                AnimatorTransitionInfo currentTransition = animator.GetAnimatorTransitionInfo(nb);
                if (currentTransition.duration == 0) //you are in a transition.
                    animator.SetLayerWeight(nb, 1f);
            }*/

    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (onEnd)
            foreach (int nb in layersNb)
                animator.SetLayerWeight(nb, 1f);
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
