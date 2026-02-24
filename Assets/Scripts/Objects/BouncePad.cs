using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BouncePad : MonoBehaviour
{
    [Tooltip("Vertical strength applied to a PlayerController when the player touches the pad.")]
    public float force = 10f;

    private void Reset()
    {
        // make sure the pad has a trigger collider by default
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // optional tag check if you want to restrict bounce to objects marked as "Player"
        if (!collision.CompareTag("Player")) return;

        var pc = collision.GetComponent<TarodevController.PlayerController>();
        if (pc == null) return;

        // use the controller's bounce method instead of fiddling with the rigidbody directly
        pc.ApplyBounce(force);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 start = transform.position;
        Vector3 end = start + transform.up * force * 0.25f; // scale down for visualization
        Gizmos.DrawLine(start, end);
    }
}
