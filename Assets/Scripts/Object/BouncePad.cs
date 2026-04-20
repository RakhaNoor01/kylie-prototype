using TarodevController;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BouncePad : MonoBehaviour
{
    [Tooltip("Launch strength applied in the pad's up direction.")]
    public float force = 10f;
    public bool airCurrent = false;
    [Tooltip("How horizontal the pad can be (0=sideways, 1=upright) before a ForceJump upkick is added.")]
    [Range(0f, 1f)]
    public float horizontalThreshold = 0.5f;
    public float slowfall = 1f;
    public float glideAccel = 5f;

    private float _internalGlide = 0f;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        _internalGlide = 0f;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        _internalGlide = 0f;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        var pc = collision.GetComponent<PlayerController>();
        if (pc == null) return;

        if (airCurrent)
        {
            AirCurrent(pc);
            return;
        }

        pc.CancelDash();
        Vector2 launchVelocity = transform.up * force;
        pc.SetFrameVelocity(launchVelocity);
        float uprightness = Vector2.Dot(transform.up, Vector2.up);
        if (uprightness < horizontalThreshold)
            pc.ForceJump();
        pc.RechargeDash();
    }

    private void AirCurrent(PlayerController pc)
    {
        Vector2 vel = pc.Velocity;

        // Locally determine if the player "wants to glide" in the air current:
        // jump button held, not grounded. No dependency on pc.IsGliding at all.
        bool jumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C);
        bool wantsGlide = jumpHeld && !pc.Grounded;

        if (wantsGlide)
        {
            // Ramp up the upward push over time, cap at force
            _internalGlide = Mathf.Min(_internalGlide + glideAccel * Time.deltaTime, force);
            vel.y = _internalGlide;
        }
        else
        {
            // Slow the fall but don't actively push up; bleed off the ramp
            _internalGlide = 0f;
            if (vel.y < -slowfall)
                vel.y = -slowfall;
        }

        pc.SetFrameVelocity(vel);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = airCurrent ? Color.blue : Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + transform.up * force * 0.25f;
        Gizmos.DrawLine(start, end);
    }
}