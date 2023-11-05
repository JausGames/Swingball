using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationEvent : MonoBehaviour
{
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
    }


    public void ActivateWeaponEvent()
    {
        Debug.Log("PlayerAnimationEvent, ActivateWeaponEvent");
        //weapon.IsActive(true);
        //source.PlayOneShot(swingNormal);
    }
    public void DeactivateWeaponEvent()
    {
        Debug.Log("PlayerAnimationEvent, DeactivateWeaponEvent");
        //weapon.IsActive(false);
    }
    public void ActivateSmashWeaponEvent()
    {
        Debug.Log("PlayerAnimationEvent, ActivateSmashWeaponEvent");
        //weapon.IsActive(true);
        //source.PlayOneShot(swingSmash);
    }
    public void DeactivateSmashWeaponEvent()
    {
        Debug.Log("PlayerAnimationEvent, DeactivateSmashWeaponEvent");
        //weapon.IsActive(false);
    }
    public void ActivateLobWeaponEvent()
    {
        Debug.Log("PlayerAnimationEvent, ActivateLobWeaponEvent");
        //weapon.IsActive(true, WeaponCollider.State.Lob);
        //source.PlayOneShot(swingLob);
    }
    public void DeactivateLobWeaponEvent()
    {
        Debug.Log("PlayerAnimationEvent, DeactivateLobWeaponEvent");
        //weapon.IsActive(false, WeaponCollider.State.Lob);
    }
}
