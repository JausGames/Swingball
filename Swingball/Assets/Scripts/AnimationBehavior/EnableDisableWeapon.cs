using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableDisableWeapon : StateMachineBehaviour
{
    [SerializeField] WeaponCollider weapon;
    [SerializeField] bool enable;
    [SerializeField] bool enabled;
    [SerializeField] bool disable;
    [SerializeField] bool disabled;
    float startTime;
    [SerializeField] private float waitEnableTime;
    [SerializeField] private float waitDisableTime;
    [SerializeField] private int slashEffectNb = 0;
    [SerializeField] private List<AudioClip> clips;
    [SerializeField] private AudioSource source;
    [SerializeField] private WeaponCollider.State state = WeaponCollider.State.Normal;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        weapon = animator.GetComponentInChildren<WeaponCollider>();
        source = animator.GetComponent<PlayerAnimationEvent>().Source;
        startTime = Time.time;
        enabled = false;
        disabled = false;
    }

    void EnableWeapon(bool value)
    {
        weapon.IsActive(value, state, slashEffectNb);
        Debug.Log("EnableWeapon : val = " + value);
        if(value) clips.ForEach(c => source.PlayOneShot(c));
    }
    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        if (enable && !enabled && startTime + waitEnableTime < Time.time)
        {
            EnableWeapon(true);
            enabled = true;
        }

        if (disable && !disabled && startTime + waitDisableTime < Time.time)
        {
            EnableWeapon(false);
            disabled = true;
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!disable) return;
        weapon.IsActive(false);
        disabled = true;
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
