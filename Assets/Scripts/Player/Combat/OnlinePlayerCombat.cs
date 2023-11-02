using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class OnlinePlayerCombat : NetworkBehaviour
{
    [SerializeField] Rigidbody body;

    [SerializeField] private NetworkVariable<bool> attacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<bool> lobbing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> Attacking { get => attacking; set => attacking = value; }
    public NetworkVariable<bool> Lobbing { get => lobbing; set => lobbing = value; }


    public void Attack(bool Attacking)
    {
        if (IsOwner)
        {
            SetAttacking(Attacking);
            if (Attacking)
                StartCoroutine(ResetAttackBoolIfInavailable());
        }
    }

    public void SetAttacking(bool Attacking)
    {
        this.attacking.Value = Attacking;
    }
    IEnumerator ResetAttackBoolIfInavailable()
    {
        yield return new WaitForSeconds(.2f);
        SetAttacking(false);
    }
    public bool GetAttacking()
    {
        return enabled ? attacking.Value : false;
    }
    public void Lob(bool lobbing)
    {
        if (IsOwner)
        {
            SetLobbing(lobbing);
            if (lobbing)
                StartCoroutine(ResetLobBoolIfInavailable());
        }
    }
    public void SetLobbing(bool lobbing)
    {
        this.lobbing.Value = lobbing;
    }
    IEnumerator ResetLobBoolIfInavailable()
    {
        yield return new WaitForSeconds(.2f);
        SetLobbing(false);
    }
    public bool GetLobbing()
    {
        return enabled ? lobbing.Value : false;
    }

}
