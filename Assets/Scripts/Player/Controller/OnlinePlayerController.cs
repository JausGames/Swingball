using Cinemachine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OnlinePlayerController : NetworkBehaviour
{
    [Header("Network")]
    public NetworkVariable<Vector2> acceleration = new NetworkVariable<Vector2>(new Vector3(0,0,0), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> rotation = new NetworkVariable<Vector2>(new Vector3(0, 0, 0), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    [Header("Movement")]
    [SerializeField] public AnimationCurve ACCELERATION_CURVE;
    [SerializeField] LayerMask walkable;
    [SerializeField] public float SPEED = 80f;
    [SerializeField] public float MAX_SPEED = 15f;
    [SerializeField] public float STOP_FORCE = 5f;
    private float maxSlopeAngle = 45f;
    private bool isSprinting;


    [Header("Rotation")]
    [SerializeField] public float ROTATION_SPEED = 10f;
    [SerializeField] public float ANGLE_MULTIPLIER = 100f;


    [Header("Jumping")]
    [SerializeField] public AnimationCurve JUMP_CURVE;
    [SerializeField] public float JUMP_LENGTH = .5f;
    [SerializeField] public float JUMP_FORCE = 50f;
    [SerializeField] public float JUMP_IMPULSE = 150f;
    [SerializeField] private bool isJumping;
    [SerializeField] private bool startJumping;
    public bool IsJumping { get => isJumping; set => isJumping = value;  }


    [Header("Falling")]
    [SerializeField] public AnimationCurve FALLING_CURVE;
    [SerializeField] public float FALL_LENGTH = .5f;
    [SerializeField] public float FALL_FORCE = 50f;
    [SerializeField] bool grounded = false;
    [SerializeField] Transform floorCheck;

    [Header("Wall run")]
    private RaycastHit slopeHit;
    private RaycastHit rightWallhit;
    private RaycastHit leftWallhit;
    private float wallCheckDistance = 0.4f;


    private bool wallRight;
    private bool wallLeft;
    private bool isWallJumping;


    [Header("Wall jump")]
    [SerializeField] public float WALL_JUMP_SIDE_FORCE = 5f;
    [SerializeField] public float WALL_JUMP_SIDE_IMPULSE = 15f;

    [Header("Slide")]
    [SerializeField] private bool isCrouching;
    [SerializeField] public float SLIDE_LENGTH = 1f;

    new Transform camera;
    FiniteMovementStateMachine machineState;

    Rigidbody body;
    private CinemachineOrbitalTransposer vcam;
    internal float maxYOrbit = 45f;

    public Vector3 CameraFlatRight { get => new Vector3(camera.right.x, 0f, camera.right.z).normalized; }
    public Vector3 CameraFlatForward { get => new Vector3(camera.forward.x, 0f, camera.forward.z).normalized; }
    public Rigidbody Body { get => body; set => body = value; }
    public RaycastHit SlopeHit { get => slopeHit; set => slopeHit = value; }
    public bool Grounded { get => grounded; set => grounded = value; }
    public bool IsSprinting { get => isSprinting; set => isSprinting = value; }
    public bool WallRight { get => wallRight; set => wallRight = value; }
    public bool WallLeft { get => wallLeft; set => wallLeft = value; }
    public RaycastHit RightWallhit { get => rightWallhit; set => rightWallhit = value; }
    public RaycastHit LeftWallhit { get => leftWallhit; set => leftWallhit = value; }
    public bool IsWallJumping { get => isWallJumping; set => isWallJumping = value; }
    internal bool IsCrouching { get => isCrouching; set => isCrouching = value; }
    public bool StartJumping { get => startJumping; set => startJumping = value; }
    internal CinemachineOrbitalTransposer Vcam { get => vcam; set => vcam = value; }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            Grounded = Physics.CheckSphere(floorCheck.position, .1f, walkable);

            machineState.CheckTransitionState();
            machineState.UpdatePosition();
            //machineState.UpdateRotation();
            machineState.ControlSpeed();
        }
    }
    private void LateUpdate()
    {
        if (IsOwner)
        {
            //Grounded = Physics.CheckSphere(floorCheck.position, .1f, walkable);

            //machineState.CheckTransitionState();
            //machineState.UpdatePosition();
            machineState.UpdateRotation();
            //machineState.ControlSpeed();
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        camera = FindObjectOfType<Camera>().transform;
        vcam = FindObjectOfType<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineOrbitalTransposer>();

        machineState = new FiniteMovementStateMachine(this);
    }

    #region Move
    //Methods use to move player
    public void Move(Vector2 direction)
    {
        if (IsOwner)
            ChangeAcceleration(direction);
    }

    void ChangeAcceleration(Vector2 direction)
    {
        //check controller dead zone  
        if (direction.magnitude > 0.2f) acceleration.Value = direction;
        else acceleration.Value = Vector2.zero;
    }
    public Vector3 MoveDirection()
    {
        return VectorOperation.Mult(acceleration.Value, CameraFlatRight, CameraFlatForward).normalized;
    }
    #endregion

    #region Rotation
    // Methods use to rotate player
    public void Look(Vector2 direction)
    {
        if (IsOwner)
            ChangeRotation(direction);
    }
    void ChangeRotation(Vector2 direction)
    {
        //check controller dead zone  
        if (direction.magnitude > 0.05f) rotation.Value = direction;
        else rotation.Value = Vector2.zero;
    }
    #endregion

    #region Movement state trigger
    internal void Jump(bool value)
    {
        if (IsOwner)
        {
            if(grounded && value)
                IsJumping = true;

            else if(machineState.CurrState.Status == MovementStatus.WallRun && value)
                isWallJumping = true;

            else if(!value)
            {
                isWallJumping = false;
                IsJumping = false;
            }
        }
    }
    internal void Sprint(bool value)
    {
        if (IsOwner)
        {
            if (value && grounded)
            {
                isSprinting = true;
            }
            else
                isSprinting = false;
        }
    }
    internal void Crouch(bool value)
    {
        IsCrouching = value;
    }
    #endregion

    #region Slope


    public bool OnSlope()
    {
        if (grounded && Physics.Raycast(transform.position + .2f * Vector3.up, Vector3.down, out slopeHit, .3f, walkable))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }
    public Vector3 GetSlopeMoveDirection()
    {
        var result = Vector3.ProjectOnPlane(MoveDirection(), slopeHit.normal).normalized;
        Debug.DrawLine(transform.position, transform.position + result * 5f, Color.yellow);
        return result;
    }
    public Vector3 GetSlopeSpeed()
    {
        var result = Vector3.ProjectOnPlane(body.velocity, slopeHit.normal);
        Debug.DrawLine(transform.position, transform.position + result.normalized * 5f, Color.green);
        return result;
    }
    #endregion

    public bool CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallhit, wallCheckDistance, walkable);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallhit, wallCheckDistance, walkable);

        Debug.Log($"PlayerController, CheckForWall : wallRight {wallRight}, wallLeft {wallLeft}");
        Debug.DrawRay(transform.position, transform.right * wallCheckDistance, wallRight ? Color.red : Color.green);
        Debug.DrawRay(transform.position, -transform.right * wallCheckDistance, wallRight ? Color.red : Color.green);

        return wallRight || wallLeft;
    }
    internal void SetSlowMo(bool value)
    {
        body.isKinematic = value;
    }
}
