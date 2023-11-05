using Cinemachine;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class OnlinePlayer : NetworkBehaviour
{
    OnlinePlayerController controller;
    OnlinePlayerCombat combat;

    [SerializeField] ParticleSystem bloodParticle;
    float maxHealth = 100f;
    public NetworkVariable<float> health = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool isDying;
    private UnityEvent dieEvent = new UnityEvent();
    private UnityEvent resurectEvent = new UnityEvent();
    [SerializeField] private HealthBar healthbar;
    [SerializeField] Transform lookAtTransform;


    [SerializeField] private bool isResurecting = false;
    internal bool isHurt;
    private bool deadFromFalling;
    private bool slowMotion;

    public bool IsDying { get => isDying; set => isDying = value; }
    public bool IsDead { get => isDead.Value; set => isDead.Value = value; }
    public UnityEvent DieEvent { get => dieEvent; set => dieEvent = value; }
    public UnityEvent ResurectEvent { get => resurectEvent; set => resurectEvent = value; }
    public bool IsResurecting { get => isResurecting; set => isResurecting = value; }
    public HealthBar Healthbar { get => healthbar; set => healthbar = value; }
    public float MaxHealth { get => maxHealth; set => maxHealth = value; }
    public bool IsHurt { get => isHurt; set => isHurt = value; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioLowPassFilter lowPass;
    public Vector3 hitDirection
    {
        get
        {
            return Camera.main.transform.forward;
        }
    }

    public OnlinePlayerController Controller { get => controller; set => controller = value; }

    internal void SetSlowMo(bool v)
    {
        Debug.Log("OnlinePlayer, SetSlowMo : v = " + v);
        controller.SetSlowMo(v);
        slowMotion = v;
    }

    public bool DeadFromFalling { get => deadFromFalling; set => deadFromFalling = value; }
    public bool SlowMotion { get => slowMotion; set => slowMotion = value; }
    public AudioSource AudioSource { get => audioSource; set => audioSource = value; }
    public AudioLowPassFilter LowPass { get => lowPass; set => lowPass = value; }

    private void Update()
    {
        if (!IsOwner) return;
        if (transform.position.y <= -5f && health.Value > 0 && !IsDead && !IsResurecting && controller.enabled && !deadFromFalling)
        {
            deadFromFalling = true;
            GetHit(health.Value);
        }
    }
    private void Start()
    {
        controller = GetComponent<OnlinePlayerController>();
        combat = GetComponent<OnlinePlayerCombat>();

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            CinemachineVirtualCameraBase camera = Camera.main.GetComponentInParent<CinemachineFreeLook>();
            camera.LookAt = lookAtTransform;
            camera.Follow = transform;
        }
    }

    [ClientRpc]
    internal void ReplacePlayerClientRpc(Vector3 position, Quaternion rotation)
    {
        if (IsOwner)
        {
            controller.Body.velocity = Vector3.zero;
            transform.position = position;
            transform.rotation = rotation;
            var cine = FindObjectOfType<CinemachineOrbitalTransposer>();
            cine.m_XAxis.Value = rotation.y;
        }
    }

    public void SetHealthBar(HealthBar healthBar)
    {
        healthbar = healthBar;
        healthbar.SetMaxHealth(maxHealth);
        health.OnValueChanged += UpdateHealthbar;
    }
    public void UpdateHealthbar(float previousValue, float newValue)
    {
        healthbar.SetHealth(newValue);
    }

    public void GetHit(float damage)
    {
        if (IsDead || IsDying || IsResurecting) return;
        if (health.Value > damage) IsHurt = true;

        Debug.Log("OnlinePlayer, GetHit : damage = " + damage);
        SubmitGetHitRequestServerRpc(damage);
    }

    /*[ServerRpc(RequireOwnership = false)]
    internal void TryResurect(bool withEvent = true)
    {
        Debug.Log("OnlinePlayer, TryResurectServerRpc");
        if (IsOwner)
            StartCoroutine(Resurect(withEvent));
        else
            ResurectPlayerClientRpc(withEvent);
    }*/

    private IEnumerator Resurect(bool withEvent)
    {
        yield return new WaitForSeconds(1.5f);

        Debug.Log("OnlinePlayer, Resurect");
        isResurecting = true;

        yield return new WaitForSeconds(1.5f);

        SetIsDead(false);

        if (IsOwner && withEvent)
            ResurectEvent.Invoke();
    }

    [ClientRpc]
    public void ResurectPlayerClientRpc(bool withEvent)
    {
        Debug.Log("OnlinePlayer, ResurectPlayerClientRpc");
        if (IsOwner)
            StartCoroutine(Resurect(withEvent));
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitGetHitRequestServerRpc(float damage, ServerRpcParams rpcParams = default)
    {
        //GetHit(damage);
        health.Value = Mathf.Max(health.Value - damage, 0f);
        if (health.Value == 0)
        {
            Die();
        }
        else
        {
            ClientPlayHitClientRpc();
        }
        ClientPlayBloodParticleClientRpc();
    }

    [ClientRpc]
    private void ClientPlayHitClientRpc()
    {
        if (IsOwner) return;
        IsHurt = true;
    }

    [ClientRpc]
    void ClientPlayBloodParticleClientRpc()
    {
        bloodParticle.Play();
    }

    public void Die()
    {
        SetIsDead(true);
        SubmitIsDeadRequestClientRpc();

    }

    [ClientRpc]
    private void SubmitIsDeadRequestClientRpc()
    {
        if (IsServer) return;
        SetIsDead(true);
    }

    public void SetIsDead(bool value)
    {
        if (value)
        {
            isDying = value;
            DieEvent.Invoke();
        }
        else
            SetHealthServerRpc(maxHealth);

        if (IsServer)
            isDead.Value = value;
        else
            SetIsDeadServerRpc(value);
        SetControls(!value);
        GetComponent<Collider>().enabled = !value;
        GetComponent<Rigidbody>().isKinematic = value;
    }

    public void SetControls(bool value)
    {
        Debug.Log("OnlinePlayer, SetControls : pl = " + this.name + ", value = " + value);
        controller.enabled = value;
        combat.enabled = value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetIsDeadServerRpc(bool value)
    {
        isDead.Value = value;
    }

    [ServerRpc]
    void SetHealthServerRpc(float health)
    {
        this.health.Value = health;
    }


}
