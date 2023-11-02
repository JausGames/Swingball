using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    [SerializeField] Animator animator;
    OnlinePlayerController controller;
    OnlinePlayer player;
    OnlinePlayerCombat combat;

    private void Awake()
    {
        controller = GetComponent<OnlinePlayerController>();
        combat = GetComponent<OnlinePlayerCombat>();
        player = GetComponent<OnlinePlayer>();
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!player.IsOwner) return;
        if (!animator || !controller || !player || !combat) return;

        if (player.SlowMotion && animator.speed != 0f)
            animator.speed = 0f;
        else if (animator.speed != 1f)
            animator.speed = 1f;

        if(player.IsDying)
        {
            animator.SetTrigger("Dying");
            player.IsDying = false;
        }
        if (player.IsResurecting)
        {
            animator.SetTrigger("Resurecting");
            player.IsResurecting = false;
        }
        if (player.IsHurt)
        {
            Debug.Log("IsHurt");
            animator.SetTrigger("GetHit");
            player.IsHurt = false;
        }

        if (combat.Attacking.Value && !player.IsDead)
        {
            combat.Attack(false);
            animator.SetTrigger("Attack");

        }
        if (combat.Lobbing.Value && !player.IsDead)
        {
            combat.Lob(false);
            animator.SetTrigger("Lob");
        }

        animator.SetFloat("Speed", 
                controller.OnSlope() ? 
                controller.GetSlopeSpeed().magnitude / (controller.MAX_SPEED * 2f)
                : 
                VectorOperation.GetFlatVector(controller.Body.velocity).magnitude / (controller.MAX_SPEED * 2f) * Mathf.Sign(Vector3.Dot(transform.forward, controller.Body.velocity))
            );

        if (controller.StartJumping)
        {
            controller.StartJumping = false;
            animator.SetTrigger("Jump");
        }

        animator.SetBool("InAir", !controller.Grounded);
        animator.SetBool("Slide", controller.IsCrouching);
        animator.SetBool("WallRight", controller.WallRight);
        animator.SetBool("WallLeft", controller.WallLeft);

    }
}
