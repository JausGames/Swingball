using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveForward : MonoBehaviour
{
    [SerializeField] float speed;
    void LateUpdate()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
