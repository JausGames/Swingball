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


    // Start is called before the first frame update
    void Start()
    {
        weapon = GetComponentInChildren<WeaponCollider>();
    }

    public void ActivateWeaponEvent()
    {
        weapon.IsActive(true);
        source.PlayOneShot(swingNormal);
    }
    public void DeactivateWeaponEvent()
    {
        weapon.IsActive(false);
    }
    public void ActivateSmashWeaponEvent()
    {
        weapon.IsActive(true);
        source.PlayOneShot(swingSmash);
    }
    public void DeactivateSmashWeaponEvent()
    {
        weapon.IsActive(false);
    }
    public void ActivateLobWeaponEvent()
    {
        weapon.IsActive(true, WeaponCollider.State.Lob);
        source.PlayOneShot(swingLob);
    }
    public void DeactivateLobWeaponEvent()
    {
        weapon.IsActive(false, WeaponCollider.State.Lob);
    }
}
