using TarodevController;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BouncePad : MonoBehaviour
{
    [Tooltip("Launch strength applied in the pad's up direction.")]
    public float force = 10f;
    [Tooltip("How horizontal the pad can be (0=sideways, 1=upright) before a ForceJump upkick is added.")]
    [Range(0f, 1f)]
    public float horizontalThreshold = 0.5f;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        var pc = collision.GetComponent<PlayerController>();
        if (pc == null) return;

        pc.CancelDash();
        Vector2 launchVelocity = transform.up * force;
        pc.SetFrameVelocity(launchVelocity);
        float uprightness = Vector2.Dot(transform.up, Vector2.up);
        if (uprightness < horizontalThreshold)
            pc.ForceJump();
        pc.RechargeDash();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + transform.up * force * 0.25f;
        Gizmos.DrawLine(start, end);
    }
}