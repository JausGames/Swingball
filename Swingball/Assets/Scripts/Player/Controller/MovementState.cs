using System.Collections;
using UnityEngine;


[System.Serializable]
abstract public class MovementState
{
    [SerializeField] protected MovementStatus status;

    protected MovementState(MovementStatus status)
    {
        this.status = status;
    }

    public MovementStatus Status { get => status; }

    abstract public MovementStatus CheckTransitionState(PlayerController motor);
    abstract public void OnEnterState(PlayerController motor);
    abstract public void UpdatePosition(PlayerController motor);
    abstract public void UpdateRotation(PlayerController motor);
    abstract public void ControlSpeed(PlayerController motor);
    abstract public void OnExitState(PlayerController motor);
}

public class WalkState : MovementState
{
    private float redirectMultiplier = .65f;

    public WalkState(MovementStatus status) : base(status)
    {
    }

    #region Movement
    public override void UpdatePosition(PlayerController motor)
    {
        if (motor.acceleration.Value != Vector2.zero)
        {
            var moveDirection = motor.OnSlope() ? motor.GetSlopeMoveDirection() : motor.MoveDirection();

            motor.Body.velocity +=
                motor.acceleration.Value.magnitude
                * motor.Settings.SPEED
                * motor.Settings.ACCELERATION_CURVE.Evaluate(VectorOperation.GetFlatVector(motor.Body.velocity).magnitude / motor.Settings.MAX_SPEED)
                * moveDirection;

            motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, moveDirection * motor.Body.velocity.magnitude, redirectMultiplier);
        }
        else
        {
            motor.Body.velocity -= VectorOperation.GetFlatVector(motor.Body.velocity) * motor.Settings.STOP_FORCE;
        }
    }
    public override void UpdateRotation(PlayerController motor)
    {
        var cam = Camera.main;

        var desiredDir = VectorOperation.GetFlatVector(cam.transform.forward).normalized;
        var direction = VectorOperation.GetFlatVector(motor.transform.forward).normalized;

        var angle = Mathf.Clamp(Vector3.SignedAngle(direction, desiredDir, Vector3.up) * motor.Settings.ANGLE_MULTIPLIER, -motor.Settings.ROTATION_SPEED, motor.Settings.ROTATION_SPEED);

        motor.transform.Rotate(Vector3.up, angle);


        //motor.Vcam.m_FollowOffset.y = Mathf.Clamp(motor.Vcam.m_FollowOffset.y - motor.rotation.Value.y * .5f, -motor.maxYOrbit, motor.maxYOrbit);
    }
    public override void ControlSpeed(PlayerController motor)
    {
        // stay glue to the floor
        if (motor.OnSlope())
        {
            motor.Body.AddForce(-motor.SlopeHit.normal * 50f, ForceMode.Acceleration);
        }
        else if (motor.Grounded)
        {
            {
                motor.Body.AddForce(Vector3.down * 50f, ForceMode.Acceleration);
            }

            // check max speed
            if (VectorOperation.GetFlatVector(motor.Body.velocity).magnitude > motor.Settings.MAX_SPEED)
            {
                if (motor.OnSlope())
                {
                    motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, motor.GetSlopeSpeed().normalized * motor.Settings.MAX_SPEED, .5f);
                }
                else
                {
                    var velY = motor.Body.velocity.y;
                    motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, VectorOperation.GetFlatVector(motor.Body.velocity).normalized * motor.Settings.MAX_SPEED + velY * Vector3.up, .5f);
                }
            }
        }
    }
    #endregion
    #region In & Out
    public override void OnEnterState(PlayerController motor) { }
    public override void OnExitState(PlayerController motor) { }
    #endregion
    public override MovementStatus CheckTransitionState(PlayerController motor)
    {
        if (motor.IsJumping)
            return MovementStatus.Jump;

        else if (!motor.Grounded)
            return MovementStatus.InAir;

        else if (motor.IsCrouching)
            return MovementStatus.Slide;

        else if (motor.IsSprinting)
            return MovementStatus.Sprint;

        return status;
    }

}

public class SprintState : MovementState
{
    private float speedMultiplier = 1.5f;
    private float redirectMultiplier = .2f;

    public SprintState(MovementStatus status) : base(status)
    {
    }

    #region Movement
    public override void UpdatePosition(PlayerController motor)
    {
        if (motor.acceleration.Value != Vector2.zero)
        {
            var moveDirection = motor.OnSlope() ? motor.GetSlopeMoveDirection() : motor.MoveDirection();

            motor.Body.velocity += 
                motor.acceleration.Value.magnitude 
                * motor.Settings.SPEED
                * motor.Settings.ACCELERATION_CURVE.Evaluate(VectorOperation.GetFlatVector(motor.Body.velocity).magnitude / (motor.Settings.MAX_SPEED * speedMultiplier))
                * moveDirection 
                * speedMultiplier;

            motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, moveDirection * motor.Body.velocity.magnitude, redirectMultiplier);
        }
        else
        {
            motor.Body.velocity -= VectorOperation.GetFlatVector(motor.Body.velocity) * motor.Settings.STOP_FORCE * speedMultiplier;
        }
    }
    public override void UpdateRotation(PlayerController motor)
    {
        var cam = Camera.main;

        var desiredDir = VectorOperation.GetFlatVector(cam.transform.forward).normalized;
        var direction = VectorOperation.GetFlatVector(motor.transform.forward).normalized;

        var angle = Mathf.Clamp(Vector3.SignedAngle(direction, desiredDir, Vector3.up) * motor.Settings.ANGLE_MULTIPLIER, -motor.Settings.ROTATION_SPEED, motor.Settings.ROTATION_SPEED);

        motor.transform.Rotate(Vector3.up, angle);
    }
    public override void ControlSpeed(PlayerController motor)
    {
        // stay glue to the floor
        if (motor.OnSlope())
        {
            motor.Body.AddForce(-motor.SlopeHit.normal * 50f, ForceMode.Acceleration);
        }
        else if (motor.Grounded)
        {
            motor.Body.AddForce(Vector3.down * 50f, ForceMode.Acceleration);
        }

        // check max speed
        if (VectorOperation.GetFlatVector(motor.Body.velocity).magnitude > motor.Settings.MAX_SPEED * speedMultiplier)
        {
            if (motor.OnSlope())
            {
                motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, motor.GetSlopeSpeed().normalized * motor.Settings.MAX_SPEED * speedMultiplier, .1f);
            }
            else
            {
                var velY = motor.Body.velocity.y;
                motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, VectorOperation.GetFlatVector(motor.Body.velocity).normalized * motor.Settings.MAX_SPEED * speedMultiplier + velY * Vector3.up, .1f);
            }
        }
    }
    #endregion
    #region In & Out
    public override void OnEnterState(PlayerController motor) { }
    public override void OnExitState(PlayerController motor) { }
    #endregion
    public override MovementStatus CheckTransitionState(PlayerController motor)
    {
        if (motor.IsJumping)
            return MovementStatus.Jump;

        else if (!motor.Grounded)
            return MovementStatus.InAir;

        else if (motor.IsCrouching)
            return MovementStatus.Slide;

        else if (!motor.IsSprinting)
            return MovementStatus.Walk;

        return status;
    }

}

public class JumpState : MovementState
{
    private float speedMultiplier = .1f;
    private float jumpStartTime = 0f;

    public JumpState(MovementStatus status) : base(status)
    {
    }

    #region Movement
    public override void UpdatePosition(PlayerController motor)
    {
        if (motor.acceleration.Value != Vector2.zero)
        {
            motor.Body.velocity += motor.acceleration.Value.magnitude * motor.Settings.SPEED * motor.MoveDirection() * speedMultiplier;
        }
        else
        {
            motor.Body.velocity -= VectorOperation.GetFlatVector(motor.Body.velocity) * motor.Settings.STOP_FORCE * speedMultiplier;
        }

        var timeInJumpRatio = (Time.time - jumpStartTime) / motor.Settings.JUMP_LENGTH;

        if (timeInJumpRatio >= 1) motor.IsJumping = false;
        else motor.Body.AddForce(Vector3.up * motor.Settings.JUMP_FORCE * motor.Settings.JUMP_CURVE.Evaluate(timeInJumpRatio) - Physics.gravity, ForceMode.Acceleration);

    }
    public override void UpdateRotation(PlayerController motor)
    {
        var cam = Camera.main;

        var desiredDir = VectorOperation.GetFlatVector(cam.transform.forward).normalized;
        var direction = VectorOperation.GetFlatVector(motor.transform.forward).normalized;

        var angle = Mathf.Clamp(Vector3.SignedAngle(direction, desiredDir, Vector3.up) * motor.Settings.ANGLE_MULTIPLIER, -motor.Settings.ROTATION_SPEED, motor.Settings.ROTATION_SPEED);

        motor.transform.Rotate(Vector3.up, angle);
    }
    public override void ControlSpeed(PlayerController motor){ }
    #endregion
    #region In & Out
    public override void OnEnterState(PlayerController motor) 
    { 
        jumpStartTime = Time.time;
        motor.StartJumping= true;
        motor.Body.velocity = VectorOperation.GetFlatVector(motor.Body.velocity);
        motor.Body.AddForce(Vector3.up * motor.Settings.JUMP_IMPULSE, ForceMode.VelocityChange);
    }
    public override void OnExitState(PlayerController motor) { motor.IsJumping = false; }
    #endregion
    public override MovementStatus CheckTransitionState(PlayerController motor)
    {
        if (!motor.IsJumping && !motor.Grounded)
            return MovementStatus.InAir;

        else if (!motor.IsJumping && motor.Grounded)
            if (motor.IsSprinting)
                return MovementStatus.Sprint;
            else
                return MovementStatus.Walk;

        return status;
    }
}

public class FallState : MovementState
{
    private float forceMultiplier = .1f;
    private float fallStartTime = 0f;

    public FallState(MovementStatus status) : base(status)
    {
    }

    #region Movement
    public override void UpdatePosition(PlayerController motor)
    {
        if (motor.acceleration.Value != Vector2.zero)
        {
            motor.Body.velocity += motor.acceleration.Value.magnitude * motor.Settings.SPEED * motor.MoveDirection() * forceMultiplier;
        }
        else
        {
            motor.Body.velocity -= VectorOperation.GetFlatVector(motor.Body.velocity) * motor.Settings.STOP_FORCE * forceMultiplier;
        }



        if (fallStartTime == 0f && !motor.NoGravity) fallStartTime = Time.time;
        else if (motor.NoGravity) fallStartTime = 0f;

         var timeInfallRatio = Mathf.Min((Time.time - fallStartTime) / motor.Settings.FALL_LENGTH, 1f);

        if(!motor.NoGravity)
            motor.Body.AddForce(-Vector3.up * motor.Settings.FALL_FORCE * motor.Settings.FALLING_CURVE.Evaluate(timeInfallRatio), ForceMode.Acceleration);
    }
    public override void UpdateRotation(PlayerController motor)
    {
        var cam = Camera.main;

        var desiredDir = VectorOperation.GetFlatVector(cam.transform.forward).normalized;
        var direction = VectorOperation.GetFlatVector(motor.transform.forward).normalized;

        var angle = Mathf.Clamp(Vector3.SignedAngle(direction, desiredDir, Vector3.up) * motor.Settings.ANGLE_MULTIPLIER, -motor.Settings.ROTATION_SPEED, motor.Settings.ROTATION_SPEED);

        motor.transform.Rotate(Vector3.up, angle);
    }
    public override void ControlSpeed(PlayerController motor) { }
    #endregion
    #region In & Out
    public override void OnEnterState(PlayerController motor)
    {
        if(!motor.NoGravity)
            fallStartTime = Time.time;
    } 
    public override void OnExitState(PlayerController motor) { }
    #endregion
    public override MovementStatus CheckTransitionState(PlayerController motor)
    {
        if (motor.Grounded)
            if (motor.IsSprinting)
                return MovementStatus.Sprint;
            else
                return MovementStatus.Walk;
        else if (motor.CheckForWall()
            //&& motor.acceleration.Value != Vector2.zero
            )
                return MovementStatus.WallRun;

        return status;
    }
}

public class WallRunState : MovementState
{
    private float wallRunStartTime = 0f;
    private float wallRunMaxTime = .7f;
    private float speedMultiplier = 1.35F;

    public WallRunState(MovementStatus status) : base(status)
    {
    }

    #region Movement
    public override void UpdatePosition(PlayerController motor)
    {

        //motor.Body.velocity = new Vector3(motor.Body.velocity.x, 0f, motor.Body.velocity.z);

        Vector3 wallNormal = motor.WallRight ? motor.RightWallhit.normal : motor.LeftWallhit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, motor.transform.up);

        if ((motor.MoveDirection() - wallForward).magnitude > (motor.MoveDirection() - -wallForward).magnitude)
            wallForward = -wallForward;

        var multiplier =
            Vector3.Dot(VectorOperation.GetFlatVector(motor.Body.velocity), wallForward) < 0f
            &&
            Vector3.Dot(VectorOperation.GetFlatVector(motor.MoveDirection()), wallForward) < 0f 
            ? 
            5f : 1f;

        // forward force
        motor.Body.velocity +=
            motor.acceleration.Value.magnitude
            * motor.Settings.SPEED
            * motor.Settings.ACCELERATION_CURVE.Evaluate(VectorOperation.GetFlatVector(motor.Body.velocity).magnitude / (motor.Settings.MAX_SPEED))
            * wallForward
            * multiplier;


        // upwards/downwards force
        /*if (upwardsRunning)
            motor.Body.velocity = new Vector3(motor.Body.velocity.x, wallClimbSpeed, motor.Body.velocity.z);
        if (downwardsRunning)
            motor.Body.velocity = new Vector3(motor.Body.velocity.x, -wallClimbSpeed, motor.Body.velocity.z);*/


    }
    public override void UpdateRotation(PlayerController motor)
    {
        var desiredDir = motor.MoveDirection().normalized;
        var direction = VectorOperation.GetFlatVector(motor.transform.forward).normalized;

        var angle = Mathf.Clamp(Vector3.SignedAngle(direction, desiredDir, Vector3.up) * motor.Settings.ANGLE_MULTIPLIER, -motor.Settings.ROTATION_SPEED, motor.Settings.ROTATION_SPEED);

        motor.transform.Rotate(Vector3.up, angle);
    }
    public override void ControlSpeed(PlayerController motor)
    {
        Vector3 wallNormal = motor.WallRight ? motor.RightWallhit.normal : motor.LeftWallhit.normal;

        // push to wall force   not exactly good in my case, should be acceleration dot wallNormal
        if (!(motor.WallLeft && motor.acceleration.Value.x >= 0) && !(motor.WallRight && motor.acceleration.Value.x <= 0))
            motor.Body.AddForce(-wallNormal * 50, ForceMode.Acceleration);

        // check max speed
        if (VectorOperation.GetFlatVector(motor.Body.velocity).magnitude > motor.Settings.MAX_SPEED * speedMultiplier)
        {
            var velY = motor.Body.velocity.y;
            motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, VectorOperation.GetFlatVector(motor.Body.velocity).normalized * motor.Settings.MAX_SPEED * speedMultiplier + velY * Vector3.up, .1f);
            
        }
    }
    #endregion
    #region In & Out
    public override void OnEnterState(PlayerController motor)
    {
        motor.Body.velocity = new Vector3(motor.Body.velocity.x, motor.Body.velocity.y * .1f, motor.Body.velocity.z);
        wallRunStartTime = Time.time;
    }
    public override void OnExitState(PlayerController motor) { }
    #endregion
    public override MovementStatus CheckTransitionState(PlayerController motor)
    {
        if (motor.Grounded)
            if (motor.IsSprinting)
                return MovementStatus.Sprint;
            else
                return MovementStatus.Walk;

        else if (!motor.CheckForWall())
            return MovementStatus.InAir;

        else if (motor.IsWallJumping) 
            return MovementStatus.WallJump;

        return status;
    }
}

public class WallJumpState : MovementState
{
    private float speedMultiplier = .1f;
    private float wallJumpStartTime = 0f;
    private Vector3 wallNormal;

    public WallJumpState(MovementStatus status) : base(status)
    {
    }

    #region Movement
    public override void UpdatePosition(PlayerController motor)
    {
        if (motor.acceleration.Value != Vector2.zero)
        {
            motor.Body.velocity += motor.acceleration.Value.magnitude * motor.Settings.SPEED * motor.MoveDirection() * speedMultiplier;
        }
        else
        {
            motor.Body.velocity -= VectorOperation.GetFlatVector(motor.Body.velocity) * motor.Settings.STOP_FORCE * speedMultiplier;
        }

        var timeInJumpRatio = (Time.time - wallJumpStartTime) / motor.Settings.JUMP_LENGTH;

        if (timeInJumpRatio >= 1) motor.IsWallJumping = false;
        else motor.Body.AddForce((Vector3.up * motor.Settings.JUMP_FORCE + wallNormal * motor.Settings.WALL_JUMP_SIDE_FORCE) * motor.Settings.JUMP_CURVE.Evaluate(timeInJumpRatio) - Physics.gravity, ForceMode.Acceleration);

    }
    public override void UpdateRotation(PlayerController motor)
    {
        var desiredDir = motor.MoveDirection().normalized;
        var direction = VectorOperation.GetFlatVector(motor.transform.forward).normalized;

        var angle = Mathf.Clamp(Vector3.SignedAngle(direction, desiredDir, Vector3.up) * motor.Settings.ANGLE_MULTIPLIER, -motor.Settings.ROTATION_SPEED, motor.Settings.ROTATION_SPEED);

        motor.transform.Rotate(Vector3.up, angle);
    }
    public override void ControlSpeed(PlayerController motor) { }
    #endregion
    #region In & Out
    public override void OnEnterState(PlayerController motor)
    {
        wallJumpStartTime = Time.time;

        wallNormal = motor.WallRight ? motor.RightWallhit.normal : motor.LeftWallhit.normal;

        Vector3 forceToApply = Vector3.up * motor.Settings.JUMP_IMPULSE * 0.4f + wallNormal * motor.Settings.WALL_JUMP_SIDE_IMPULSE * 0.6f;

        // reset y velocity and add force
        motor.Body.velocity = VectorOperation.GetFlatVector(motor.Body.velocity);
        motor.Body.AddForce(forceToApply, ForceMode.Impulse);
    }
    public override void OnExitState(PlayerController motor) { motor.IsJumping = false; }
    #endregion
    public override MovementStatus CheckTransitionState(PlayerController motor)
    {

        if (!motor.IsWallJumping && !motor.Grounded && motor.IsCrouching)
            return MovementStatus.Slide;

        else if (!motor.IsWallJumping && !motor.Grounded)
            return MovementStatus.InAir;

        else if (!motor.IsWallJumping && motor.Grounded)
            if (motor.IsSprinting)
                return MovementStatus.Sprint;
            else
                return MovementStatus.Walk;

        return status;
    }
}
public class SlideState : MovementState
{
    private float slideStartTime = 0f;
    private float speedMultiplier = .6f;
    private float redirectMultiplier = .2f;

    public SlideState(MovementStatus status) : base(status)
    {
    }

    #region Movement
    public override void UpdatePosition(PlayerController motor)
    {

        var moveDirection = motor.OnSlope() || motor.Body.velocity.y > -0.1f ? motor.GetSlopeMoveDirection() : motor.MoveDirection();

        motor.Body.velocity +=
            motor.acceleration.Value.magnitude
            * motor.Settings.SPEED
            * motor.Settings.ACCELERATION_CURVE.Evaluate(VectorOperation.GetFlatVector(motor.Body.velocity).magnitude / (motor.Settings.MAX_SPEED * speedMultiplier))
            * moveDirection
            * speedMultiplier;


        motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, moveDirection * motor.Body.velocity.magnitude, redirectMultiplier);

        var timeInJumpRatio = (Time.time - slideStartTime) / motor.Settings.SLIDE_LENGTH;

        if (timeInJumpRatio >= 1f) motor.IsCrouching = false;

    }
    public override void UpdateRotation(PlayerController motor)
    {
        var desiredDir = motor.MoveDirection().normalized;
        var direction = VectorOperation.GetFlatVector(motor.transform.forward).normalized;

        var angle = Mathf.Clamp(Vector3.SignedAngle(direction, desiredDir, Vector3.up) * motor.Settings.ANGLE_MULTIPLIER, -motor.Settings.ROTATION_SPEED, motor.Settings.ROTATION_SPEED);

        motor.transform.Rotate(Vector3.up, angle);
    }
    public override void ControlSpeed(PlayerController motor)
    {
        // stay glue to the floor
        /*if (motor.OnSlope())
        {
            motor.Body.AddForce(-motor.SlopeHit.normal * 50f, ForceMode.Acceleration);
        }
        else if (motor.Grounded)
        {
            motor.Body.AddForce(Vector3.down * 50f, ForceMode.Acceleration);
        }*/

        // check max speed
        if (VectorOperation.GetFlatVector(motor.Body.velocity).magnitude > motor.Settings.MAX_SPEED * speedMultiplier)
        {

            if (motor.OnSlope())
            {
                motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, motor.GetSlopeSpeed().normalized * motor.Settings.MAX_SPEED * speedMultiplier, .1f);
            }
            else
            {
                var velY = motor.Body.velocity.y;
                motor.Body.velocity = Vector3.MoveTowards(motor.Body.velocity, VectorOperation.GetFlatVector(motor.Body.velocity).normalized * motor.Settings.MAX_SPEED * speedMultiplier + velY * Vector3.up, .1f);
            }
        }
    }
    #endregion
    #region In & Out
    public override void OnEnterState(PlayerController motor)
    {
        slideStartTime = Time.time;
        motor.Body.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }
    public override void OnExitState(PlayerController motor) { }
    #endregion
    public override MovementStatus CheckTransitionState(PlayerController motor)
    {
        if (!motor.Grounded && (motor.WallLeft || motor.WallRight))
            return MovementStatus.WallRun;

        else if (!motor.Grounded)
            return MovementStatus.InAir;

        else if (motor.IsJumping)
            return MovementStatus.Jump;

        else if (!motor.IsCrouching)
            if (motor.IsSprinting)
                return MovementStatus.Sprint;
            else
                return MovementStatus.Walk;

        return status;
    }
}

