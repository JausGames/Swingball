using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerAnimationEvent : MonoBehaviour
{
    private PlayerCombat combat;
    private WeaponCollider weapon;


    public UnityEvent OffensiveEnabledEvent = new UnityEvent();
    public UnityEvent DefensiveEnabledEvent = new UnityEvent();
    public UnityEvent OffensiveDisabledEvent = new UnityEvent();
    public UnityEvent DefensiveDisabledEvent = new UnityEvent();


    [SerializeField] AudioSource source;
    [SerializeField] AudioClip swingNormal;
    [SerializeField] AudioClip swingLob;
    [SerializeField] AudioClip swingSmash;

    public AudioSource Source { get => source; set => source = value; }


    // Start is called before the first frame update
    void Start()
    {
        weapon = GetComponentInChildren<WeaponCollider>();
        combat = GetComponentInParent<PlayerCombat>();
    }

    public void MoveActionEvent()
    {
        combat.PerformMoveAction();
    }
    public void OffensiveActionEvent()
    {
        OffensiveEnabledEvent.Invoke();
    }
    public void DefensiveActionEvent()
    {
        DefensiveEnabledEvent.Invoke();
    }
    public void OffensiveStopEvent()
    {
        OffensiveDisabledEvent.Invoke();
    }
    public void DefensiveStopEvent()
    {
        DefensiveDisabledEvent.Invoke();
    }
}
