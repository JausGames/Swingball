using UnityEditor;
using UnityEngine;

public class VectorOperation
{
    static internal Vector3 GetFlatVector(Vector3 vector)
    {
        return new Vector3(vector.x, 0f, vector.z);
    }
    static internal Vector3 GetFlatVector(Vector2 vector)
    {
        return new Vector3(vector.x, 0f, vector.y);
    }
    static internal Vector3 Mult(Vector3 vector, Vector3 orthBaseX, Vector3 orthBaseZ)
    {
        return vector.x * orthBaseX + vector.z * orthBaseZ;
    }
    static internal Vector3 Mult(Vector2 vector, Vector3 orthBaseX, Vector3 orthBaseZ)
    {
        return vector.x * orthBaseX + vector.y * orthBaseZ;
    }
}
