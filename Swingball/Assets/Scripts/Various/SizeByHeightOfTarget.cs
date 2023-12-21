using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeByHeightOfTarget : MonoBehaviour
{
    float maxSize;
    float maxHeight = 10f;
    float heightOffset = 1f;
    [SerializeField] Transform target;
    // Start is called before the first frame update
    void Start()
    {
        maxSize = transform.localScale.x;
    }

    // Update is called once per frame
    void Update()
    {
        var height = Mathf.Abs(target.position.y - heightOffset);
        var value = (maxHeight / (height + maxHeight)) * maxSize;
        transform.localScale = new Vector3(value, value, value);
    }
}
