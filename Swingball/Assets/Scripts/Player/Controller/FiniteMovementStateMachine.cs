using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum MovementStatus
{
    Walk,
    Sprint,
    Jump,
    InAir,
    WallRun,
    WallJump,
    Slide,
}
public class FiniteMovementStateMachine
{
    PlayerController motor;

    [SerializeField] Dictionary<MovementStatus, MovementState> states;
    [SerializeField] MovementState currState;

    public MovementState CurrState { get => currState; set => currState = value; }

    public FiniteMovementStateMachine(PlayerController motor)
    {
        this.motor = motor;
        states = new Dictionary<MovementStatus, MovementState>() 
        { 
            { MovementStatus.Walk, new WalkState(MovementStatus.Walk) }, 
            { MovementStatus.Sprint, new SprintState(MovementStatus.Sprint) }, 
            { MovementStatus.Jump, new JumpState(MovementStatus.Jump) }, 
            { MovementStatus.InAir, new FallState(MovementStatus.InAir) }, 
            { MovementStatus.WallRun, new WallRunState(MovementStatus.WallRun) },
            { MovementStatus.WallJump, new WallJumpState(MovementStatus.WallJump) }, 
            { MovementStatus.Slide, new SlideState(MovementStatus.Slide) } 
        };

        states.TryGetValue(MovementStatus.Walk, out currState);
    }

    public void CheckTransitionState()
    {
        var result = currState.CheckTransitionState(motor);
        if (result != currState.Status)
        {
            currState.OnExitState(motor);
            states.TryGetValue(result, out currState);
            currState.OnEnterState(motor);
        }
    }
    public void OnEnterState() => currState.OnEnterState(motor);
    public void UpdatePosition() => currState.UpdatePosition(motor);
    public void UpdateRotation() => currState.UpdateRotation(motor);
    public void ControlSpeed() => currState.ControlSpeed(motor);
    public void OnExitState() => currState.OnExitState(motor);
}

