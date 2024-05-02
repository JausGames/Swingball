
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class OnlinePlayerInputs : NetworkBehaviour
{
    PlayerController controller;
    PlayerCombat combat;
    bool special;

    public UnityEvent StartAttackEvent = new UnityEvent();
    public UnityEvent StopAttackEvent = new UnityEvent();
     
    public UnityEvent StartLobEvent = new UnityEvent();
    public UnityEvent StopLobEvent = new UnityEvent();
     
    public UnityEvent StartOffensiveEvent = new UnityEvent();
    public UnityEvent StopOffensiveEvent = new UnityEvent();
     
    public UnityEvent StartDefensiveEvent = new UnityEvent();
    public UnityEvent StopDefensiveEvent = new UnityEvent();
     
    public UnityEvent StartMoveEvent = new UnityEvent();
    public UnityEvent StopMoveEvent = new UnityEvent();

    public void Start()
    {
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();
        if (!IsOwner) return;

        OnlineInputManager.Controls.Player.Move.performed += ctx => controller.Move(ctx.ReadValue<Vector2>());
        OnlineInputManager.Controls.Player.Move.canceled += _ => controller.Move(Vector2.zero);

        OnlineInputManager.Controls.Player.Look.performed += ctx => controller.Look(ctx.ReadValue<Vector2>());
        OnlineInputManager.Controls.Player.Look.canceled += _ => controller.Look(Vector2.zero);

        OnlineInputManager.Controls.Player.MouseLook.performed += ctx => controller.Look(ctx.ReadValue<Vector2>());
        OnlineInputManager.Controls.Player.MouseLook.canceled += _ => controller.Look(Vector2.zero);

        OnlineInputManager.Controls.Player.Special.performed += _ => special = true;
        OnlineInputManager.Controls.Player.Special.canceled += _ => special = false;

        OnlineInputManager.Controls.Player.Attack.performed += _ => SetAttack(true);
        OnlineInputManager.Controls.Player.Attack.canceled += _ => SetAttack(false);

        OnlineInputManager.Controls.Player.DefensiveMove.performed += _ => StartDefensiveEvent.Invoke();
        OnlineInputManager.Controls.Player.DefensiveMove.canceled += _ => StopDefensiveEvent.Invoke();

        OnlineInputManager.Controls.Player.OffensiveMove.performed += _ => StartOffensiveEvent.Invoke();
        OnlineInputManager.Controls.Player.OffensiveMove.canceled += _ => StopOffensiveEvent.Invoke();

        OnlineInputManager.Controls.Player.Lob.performed += _ => SetDefense(true);
        OnlineInputManager.Controls.Player.Lob.canceled += _ => SetDefense(false);

        OnlineInputManager.Controls.Player.MoveAction.performed += _ => StartMoveEvent.Invoke();
        OnlineInputManager.Controls.Player.MoveAction.canceled += _ => StopMoveEvent.Invoke();

        OnlineInputManager.Controls.Player.Jump.performed += _ => controller.Jump(true);
        OnlineInputManager.Controls.Player.Jump.canceled += _ => controller.Jump(false);

        OnlineInputManager.Controls.Player.Sprint.performed += _ => controller.Sprint(true);
        OnlineInputManager.Controls.Player.Sprint.canceled += _ => controller.Sprint(false);

        OnlineInputManager.Controls.Player.Crouch.performed += _ => controller.Crouch(true);
        OnlineInputManager.Controls.Player.Crouch.canceled += _ => controller.Crouch(false);
    }

    private void SetAttack(bool v)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Cursor.lockState = CursorLockMode.None;

        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (special)
            {
                if (v)
                    StartOffensiveEvent.Invoke();
                else
                    StopOffensiveEvent.Invoke();
            }
            //combat.SpecialOffensive(v);
            else
            {
                if (v)
                    StartAttackEvent.Invoke();
                else
                    StopAttackEvent.Invoke();
            }
            //combat.Attack(v);
        }
    }
    private void SetDefense(bool v)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (special)
            {
                if (v)
                    StartDefensiveEvent.Invoke();
                else
                    StopDefensiveEvent.Invoke();
            }
            //combat.SpecialDefensive(v);
            else
            {
                if (v)
                    StartLobEvent.Invoke();
                else
                    StopLobEvent.Invoke();
            }
        }

        //combat.Lob(v);
    }
}
