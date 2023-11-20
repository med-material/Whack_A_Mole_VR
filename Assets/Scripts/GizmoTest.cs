using UnityEngine;
using System.Collections;

public class ExampleClass : MonoBehaviour
{
    [SerializeField]
    public Transform target;

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            // Draws a blue line from this transform to the target
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawCube(target.position, new Vector3(1f, 1f, 1f));
        }
    }
}