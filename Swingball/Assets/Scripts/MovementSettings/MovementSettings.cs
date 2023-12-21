using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "Character Settings/Movement", order = 1)]
public class MovementSettings : ScriptableObject
{

    [Header("Movement")]
    [SerializeField] public AnimationCurve ACCELERATION_CURVE;
    [SerializeField] public float SPEED = 80f;
    [SerializeField] public float MAX_SPEED = 15f;
    [SerializeField] public float STOP_FORCE = 5f;


    [Header("Rotation")]
    [SerializeField] public float ROTATION_SPEED = 10f;
    [SerializeField] public float ANGLE_MULTIPLIER = 100f;


    [Header("Jumping")]
    [SerializeField] public AnimationCurve JUMP_CURVE;
    [SerializeField] public float JUMP_LENGTH = .5f;
    [SerializeField] public float JUMP_FORCE = 50f;
    [SerializeField] public float JUMP_IMPULSE = 150f;


    [Header("Falling")]
    [SerializeField] public AnimationCurve FALLING_CURVE;
    [SerializeField] public float FALL_LENGTH = .5f;
    [SerializeField] public float FALL_FORCE = 50f;

    [Header("Wall run")]
    public float wallCheckDistance = 0.4f;


    [Header("Wall jump")]
    [SerializeField] public float WALL_JUMP_SIDE_FORCE = 5f;
    [SerializeField] public float WALL_JUMP_SIDE_IMPULSE = 15f;

    [Header("Slide")]
    [SerializeField] public float SLIDE_LENGTH = 1f;
}
