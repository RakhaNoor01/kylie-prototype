using TarodevController;
using UnityEngine;

public class AirCurrent : MonoBehaviour
{
    public float slowfall = 1f;
    public float glideAccel = 5f;
    public float maxGlide = 10f;
    private float internalGlide = 0f;

    // The current's direction in world space, derived from the object's rotation
    private Vector2 CurrentDirection => transform.up;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        internalGlide = 0f;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        internalGlide = 0f;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        var pc = collision.GetComponent<PlayerController>();
        if (pc == null) return;

        Vector2 dir = CurrentDirection;
        Vector2 vel = pc.Velocity;

        bool jumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C);
        bool wantsGlide = jumpHeld && !pc.Grounded;

        if (wantsGlide)
        {
            // Ramp up the push along the current's direction
            internalGlide = Mathf.Min(internalGlide + glideAccel * Time.deltaTime, maxGlide);

            // Project current velocity onto current direction
            float velocityAlongCurrent = Vector2.Dot(vel, dir);

            // Only push if we haven't reached max push along that axis
            if (velocityAlongCurrent < internalGlide)
            {
                pc.AddExternalVelocity(dir * internalGlide);
            }
        }
        else
        {
            internalGlide = 0f;

            // Slow movement against the current direction (generalised "slowfall")
            float velocityAlongCurrent = Vector2.Dot(vel, dir);

            // If moving opposite to the current direction beyond the slowfall threshold, clamp it
            if (velocityAlongCurrent < -slowfall)
            {
                vel -= dir * velocityAlongCurrent;      // strip the component
                vel += dir * (-slowfall);               // replace with clamped value
            }
        }

        pc.SetFrameVelocity(vel);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 start = transform.position;
        Vector3 end = start + transform.up * maxGlide * 0.25f;
        Gizmos.DrawLine(start, end);
    }
}