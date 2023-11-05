
using System;
using Unity.Netcode;
using UnityEngine;

public class OnlinePlayerInputs : NetworkBehaviour
{
    OnlinePlayerController controller;
    OnlinePlayerCombat combat;
    public void Start()
    {
        controller = GetComponent<OnlinePlayerController>();
        combat = GetComponent<OnlinePlayerCombat>();
        if (!IsOwner) return;

        OnlineInputManager.Controls.Player.Move.performed += ctx => controller.Move(ctx.ReadValue<Vector2>());
        OnlineInputManager.Controls.Player.Move.canceled += _ => controller.Move(Vector2.zero);

        OnlineInputManager.Controls.Player.Look.performed += ctx => controller.Look(ctx.ReadValue<Vector2>());
        OnlineInputManager.Controls.Player.Look.canceled += _ => controller.Look(Vector2.zero);

        OnlineInputManager.Controls.Player.MouseLook.performed += ctx => controller.Look(ctx.ReadValue<Vector2>());
        OnlineInputManager.Controls.Player.MouseLook.canceled += _ => controller.Look(Vector2.zero);

        OnlineInputManager.Controls.Player.Attack.performed += _ => combat.Attack(true);
        OnlineInputManager.Controls.Player.Attack.canceled += _ => combat.Attack(false);

        OnlineInputManager.Controls.Player.Lob.performed += _ => combat.Lob(true);
        OnlineInputManager.Controls.Player.Lob.canceled += _ => combat.Lob(false);

        OnlineInputManager.Controls.Player.Jump.performed += _ => controller.Jump(true);
        OnlineInputManager.Controls.Player.Jump.canceled += _ => controller.Jump(false);

        OnlineInputManager.Controls.Player.Sprint.performed += _ => controller.Sprint(true);
        OnlineInputManager.Controls.Player.Sprint.canceled += _ => controller.Sprint(false);

        OnlineInputManager.Controls.Player.Crouch.performed += _ => controller.Crouch(true);
        OnlineInputManager.Controls.Player.Crouch.canceled += _ => controller.Crouch(false);
    }
}
