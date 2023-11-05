using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StayAtSpecificHeight : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float height = 0f;
    Quaternion baseRot;
    // Start is called before the first frame update
    void Start()
    {
        baseRot = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(target.position.x, height, target.position.z);
        transform.rotation = baseRot;
    }
}
