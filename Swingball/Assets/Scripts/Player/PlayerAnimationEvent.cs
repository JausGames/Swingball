using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationEvent : MonoBehaviour
{
    private PlayerCombat combat;
    private WeaponCollider weapon;


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
}
