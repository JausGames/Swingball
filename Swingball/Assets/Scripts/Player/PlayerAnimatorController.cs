using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerAnimatorController : MonoBehaviour
{
    protected Animator animator;
    protected PlayerController controller;
    Player player;
    protected PlayerCombat combat;
    [SerializeField] bool[] enableLayer = new bool[4] { true, true, true, true };

    Dictionary<int, float> layerActions = new Dictionary<int, float>();



    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();
        player = GetComponent<Player>();
        animator = GetComponentInChildren<Animator>();

        combat.Attacking.OnValueChanged += OnAttackingChanged;
        combat.Lobbing.OnValueChanged += OnControlChanged;
        combat.DefensiveMove.OnValueChanged += OnDefensiveChanged;
        combat.OffensiveMove.OnValueChanged += OnOffensiveChanged;
        combat.Moving.OnValueChanged += OnMoveChanged;
    }

    internal void ToIdle()
    {
        animator.SetTrigger("Idle");
    }
    private void OnAttackingChanged(bool previous, bool current)
    {
        animator.SetBool("Attack", current);
    }
    protected virtual void OnControlChanged(bool previous, bool current)
    {
        animator.SetBool("Control", current);
    }
    private void OnDefensiveChanged(bool previous, bool current)
    {
        animator.SetBool("Defensive", current);
    }
    private void OnOffensiveChanged(bool previous, bool current)
    {
        animator.SetBool("Offensive", current);
    }
    private void OnMoveChanged(bool previous, bool current)
    {
        animator.SetBool("Move", current);
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
        if (player.IsFalling)
        {
            animator.SetTrigger("Fall");
            player.IsFalling = false;
        }
        if (player.GettingUp)
        {
            animator.SetTrigger("GetUp");
            player.GettingUp = false;
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

        var actions = layerActions;
        actions.Keys.ToList().ForEach(k =>
        {
            if (enableLayer[k])
            {
                var currVal = animator.GetLayerWeight(k);
                if (currVal != actions[k])
                    animator.SetLayerWeight(k, Mathf.MoveTowards(currVal, actions[k], 5f * Time.deltaTime));
                else
                    layerActions.Remove(k);
            }
        });

    }

    public void EnableLayer(int nb, bool enable)
    {
        enableLayer[nb] = enable;
    }
    public void SetLayer(int nb, float value)
    {
        if (!(enableLayer.Length > nb)) return;
        if (layerActions.ContainsKey(nb))
            layerActions[nb] = value;
        else
            layerActions.Add(nb, value);
    }
    protected virtual bool CanAttack()
    {
        return !player.IsDead && !player.InHurtAnim;
    }
}
