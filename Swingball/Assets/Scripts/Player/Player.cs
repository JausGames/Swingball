using Cinemachine;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class PlayerGameInfo
{
    public string name;
    public Guid id;
    public float elo;
    public string gameVersion;
    // Add other fields as necessary
}

public class Player : NetworkBehaviour
{
    PlayerController controller;
    PlayerCombat combat;
    [SerializeField] Arrow arrow;

    [SerializeField] ParticleSystem bloodParticle;
    float maxHealth = 100f;
    public NetworkVariable<float> health = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> special = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool isFalling;
    private bool isDying;
    private UnityEvent dieEvent = new UnityEvent();
    private UnityEvent resurectEvent = new UnityEvent();
    [SerializeField] private HealthBar healthbar;
    [SerializeField] private HealthBar specialbar;
    [SerializeField] HealthBar moveActionBar;
    [SerializeField] Transform lookAtTransform;


    [SerializeField] private bool isResurecting = false;
    internal bool isHurt;
    private bool deadFromFalling;
    private bool slowMotion;
    private bool invincible = false;

    public bool IsFalling { get => isFalling; set => isFalling = value; }
    public bool IsDying { get => isDying; set => isDying = value; }
    public bool IsDead { get => isDead.Value; set => isDead.Value = value; }
    public UnityEvent DieEvent { get => dieEvent; set => dieEvent = value; }
    public UnityEvent ResurectEvent { get => resurectEvent; set => resurectEvent = value; }
    public bool IsResurecting { get => isResurecting; set => isResurecting = value; }
    public HealthBar Healthbar { get => healthbar; set => healthbar = value; }
    public float MaxHealth { get => maxHealth; set => maxHealth = value; }
    public bool IsHurt { get => isHurt; set => isHurt = value; }

    /// <summary>
    /// Special is server write only, call the method from server RPC
    /// </summary>
    /// <param name="value">Amount to remove from special points</param>
    internal void RemoveSpecialPoints(float value)
    {
        if(!isTraining)
            special.Value = Mathf.Max(0, special.Value - value);
    }

    [SerializeField] private AudioSource audioSource;


    [SerializeField] private AudioLowPassFilter lowPass;
    private Ball ball;
    private bool isTraining = false;
    private bool gettingUp;

    [HideInInspector]
    public UnityEvent GetHitEvent = new UnityEvent();

    [Header("Selector ui")]
    [SerializeField] private Sprite image;
    [SerializeField] public GameObject DummySelector;

    public Vector3 hitDirection
    {
        get
        {
            return Camera.main.transform.forward;
        }
    }

    public PlayerController Controller { get => controller; set => controller = value; }

    internal void SetSlowMo(bool v)
    {
        controller.SetSlowMo(v);
        slowMotion = v;
    }

    public bool DeadFromFalling { get => deadFromFalling; set => deadFromFalling = value; }
    public bool SlowMotion { get => slowMotion; set => slowMotion = value; }
    public AudioSource AudioSource { get => audioSource; set => audioSource = value; }
    public AudioLowPassFilter LowPass { get => lowPass; set => lowPass = value; }
    public bool InHurtAnim { get; internal set; }
    public Ball Ball { get => ball; set => ball = value; }
    public bool Invincible { get => invincible; set => invincible = value; }
    public PlayerCombat Combat { get => combat; set => combat = value; }
    public bool GettingUp { get => gettingUp; set => gettingUp = value; }
    public bool IsTraining { get => isTraining; set => isTraining = value; }
    public Sprite Image { get => image; set => image = value; }

    private void Update()
    {
        if (!IsOwner) return;
        if (transform.position.y <= -5f && health.Value > 0 && !IsDead && !IsResurecting && controller.enabled && !deadFromFalling)
        {
            deadFromFalling = true;
            GetHit(health.Value);
        }

        if (!moveActionBar) return;
        var v = Mathf.Clamp((combat.MoveActionCooldown - combat.NextMoveAction + Time.time) / combat.MoveActionCooldown, 0f, 1f) * 100f;
        if(v != moveActionBar.Value)
            moveActionBar.SetHealth(v);
    }
    private void Start()
    {
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();

        if (IsOwner)
        {
            arrow.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            CinemachineVirtualCameraBase camera = Camera.main.GetComponentInParent<CinemachineFreeLook>();
            camera.LookAt = lookAtTransform;
            camera.Follow = transform;
        }
    }


    internal void SetFall(float trapTime)
    {
        isFalling = true;

        StartCoroutine(WaitToGetUp(trapTime));
    }

    private IEnumerator WaitToGetUp(float trapTime)
    {
        yield return new WaitForSeconds(trapTime);


        gettingUp = true;
    }

    internal Vector3 GetLobDirection()
    {
        return combat.LobSettings.Direction(hitDirection);
    }

    internal bool CanSpecialMove(bool isOffensive)
    {
        if (isOffensive && combat.OffensiveMoveValue <= special.Value)
            return true;
        else if(!isOffensive && combat.DefensiveMoveValue <= special.Value)
            return true;

        return false;
    }

    internal float GetLobSpeed()
    {
        return combat.LobSettings.Speed;
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

    internal void PlayerTouched(Player player, WeaponCollider.BallState state)
    {
        combat.PlayerTouched(player, state);
    }

    public void SetHealthBar(HealthBar healthBar)
    {
        healthbar = healthBar;
        healthbar.SetMaxHealth(maxHealth);
        healthbar.SetHealth(maxHealth);
        health.OnValueChanged += UpdateHealthbar;
    }

    public void UpdateHealthbar(float previousValue, float newValue)
    {
        healthbar.SetHealth(newValue);
    }
    public void SetSpecialBar(HealthBar specialBar)
    {
        specialbar = specialBar;
        specialbar.SetMaxHealth(100f);
        specialbar.SetHealth(special.Value);
        special.OnValueChanged += UpdateSpecialBar;
    }
    public void UpdateSpecialBar(float previousValue, float newValue)
    {
        specialbar.SetHealth(newValue);
    }
    public void SetMoveActionBar(HealthBar healthBar)
    {
        moveActionBar = healthBar;
        moveActionBar.SetMaxHealth(1f);
        moveActionBar.SetHealth(1f);

    }

    public void GetHit(float damage)
    {
        if (IsDead || IsDying || IsResurecting) return;
        if (health.Value > damage) IsHurt = true;

        SubmitGetHitRequestServerRpc(damage);

        GetHitEvent.Invoke();
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

        isResurecting = true;

        yield return new WaitForSeconds(1.5f);

        SetIsDead(false);

        if (IsOwner && withEvent)
            ResurectEvent.Invoke();
    }

    [ClientRpc]
    public void ResurectPlayerClientRpc(bool withEvent)
    {
        if (IsOwner)
            StartCoroutine(Resurect(withEvent));
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitGetHitRequestServerRpc(float damage, ServerRpcParams rpcParams = default)
    {
        Debug.Log("SubmitGetHitRequestServerRpc");

        if (!isTraining)
            health.Value = Mathf.Max(health.Value - damage, 0f);
        if (health.Value == 0)
            Die();
        
        else
            ClientPlayHitClientRpc();
        
        ClientPlayBloodParticleClientRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    internal void SubmitAddSpecialRequestServerRpc(float point, ServerRpcParams rpcParams = default)
    {
        if (isTraining) return;
        special.Value = Mathf.Min(100f, special.Value + point);
    }



    [ClientRpc]
    internal void SetUpBallClientRpc(ulong networkObjectId)
    {
        var ballObj = GetNetworkObject(networkObjectId);
        ball = ballObj.GetComponent<Ball>();
        arrow.Ball = ballObj.transform;
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
