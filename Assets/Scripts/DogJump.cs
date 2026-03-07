using UnityEngine;

public class JumpWaypoint : MonoBehaviour
{
    public Transform landingPoint;
    public float arcHeight = 3f; // tinggi puncak lompatan dari posisi anjing

    void OnDrawGizmos()
    {
        if (landingPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawSphere(landingPoint.position, 0.2f);
        Gizmos.DrawLine(transform.position, landingPoint.position);

        // Preview arc parabola
        Gizmos.color = Color.green;
        Vector3 start = transform.position;
        Vector3 end = landingPoint.position;
        int steps = 20;
        Vector3 prev = start;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = Mathf.Lerp(start.x, end.x, t);
            float y = Mathf.Lerp(start.y, end.y, t) + arcHeight * 4f * t * (1f - t);
            Vector3 curr = new Vector3(x, y, 0f);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
    }
}