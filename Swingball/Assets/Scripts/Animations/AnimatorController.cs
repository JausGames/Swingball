using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    Animator animator;
    [SerializeField] bool[] enableLayer = new bool[3] { true, true, true };

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
}
