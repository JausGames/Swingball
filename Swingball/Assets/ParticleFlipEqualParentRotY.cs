using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleFlipEqualParentRotY : MonoBehaviour
{
    ParticleSystem particle;
    [SerializeField] Transform parent;
    [SerializeField] float offset;
    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }
    private void Update()
    {
        if (!particle.isPlaying) return;

        var main = particle.main;
        var rotY = main.startRotationY;

        rotY.constant = parent.eulerAngles.y * (Mathf.PI / 180f);
        main.startRotationY = rotY;
    }
}
