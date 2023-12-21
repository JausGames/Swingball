using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public Transform Ball { get; set; }

    private void LateUpdate()
    {
        if(Ball)
            transform.LookAt(Ball, Vector3.up);
    }
}
