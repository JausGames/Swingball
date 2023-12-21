using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    protected Animator animator;
    protected PlayerController controller;
    Player player;
    protected PlayerCombat combat;
    [SerializeField] bool[] enableLayer = new bool[3] { true, true, true };

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();
        player = GetComponent<Player>();
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!player.IsOwner) return;
        if (!animator || !controller || !player || !combat) return;

        if (player.SlowMotion)
            animator.speed = 0f;
        else
            animator.speed = 1f;

        if (player.IsDying)
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
            animator.SetTrigger("GetHit");
            player.IsHurt = false;
            player.InHurtAnim = true;
        }

        if (combat.Moving.Value && CanAttack())
        {
            combat.Move(false);
            animator.SetTrigger("MoveAction");

        }
        if (combat.DefensiveMove.Value && CanAttack())
        {
            combat.SpecialDefensive(false);
            animator.SetTrigger("DefensiveMove");

        }
        else if (combat.Lobbing.Value && CanAttack())
        {
            combat.Lob(false);
            animator.SetTrigger("Lob");
        }
        else if (combat.OffensiveMove.Value && CanAttack())
        {
            combat.SpecialOffensive(false);
            animator.SetTrigger("OffensiveMove");
        }
        else if (combat.Attacking.Value && CanAttack())
        {
            combat.Attack(false);
            animator.SetTrigger("Attack");

        }

        animator.SetFloat("Speed",
                controller.OnSlope() ?
                controller.GetSlopeSpeed().magnitude / (controller.Settings.MAX_SPEED * 1.5f)
                :
                VectorOperation.GetFlatVector(controller.Body.velocity).magnitude / (controller.Settings.MAX_SPEED * 1.5f) * Vector3.Dot(transform.forward, controller.Body.velocity.normalized)
            );
        animator.SetFloat("SpeedX",
                VectorOperation.GetFlatVector(controller.Body.velocity).magnitude / (controller.Settings.MAX_SPEED * 1.5f) * Vector3.Dot(transform.right, controller.Body.velocity.normalized)
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

    public void EnableLayer(int nb, bool enable)
    {
        enableLayer[nb] = enable;
    }
    public void SetLayer(int nb, float value)
    {
        if (!enableLayer[nb]) return;
        animator.SetLayerWeight(nb, value);
    }
    protected virtual bool CanAttack()
    {
        return !player.IsDead && !player.InHurtAnim;
    }
}
