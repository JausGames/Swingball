using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public Collider[] colliders;
    // Start is called before the first frame update
    void Start()
    {
        foreach(Collider col1 in colliders)
        {
            foreach(Collider col2 in colliders)
            {
                if (col1 != col2) Physics.IgnoreCollision(col1, col2);
            }
        }
    }
}
