using TarodevController;
using UnityEngine;

public class AirCurrent : MonoBehaviour
{
    public float slowfall = 1f;
    public float glideAccel = 5f;
    public float maxGlide = 10f;
    public bool horizontal = false;
    private float internalGlide = 0f;

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

        if (pc.IsDashing)
        {
            internalGlide = 0; return;
        }

        Vector2 dir = CurrentDirection;
        Vector2 vel = pc.FrameVelocity;

        bool jumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C);
        bool wantsGlide = jumpHeld && !pc.Grounded && pc.Glider;
        float velocityAlongCurrent = Vector2.Dot(vel, dir);

        pc.ResetGlide();

        if (wantsGlide)
        {
            internalGlide = Mathf.Min(internalGlide + glideAccel * Time.fixedDeltaTime, maxGlide);

            if (velocityAlongCurrent < internalGlide)
            {
                pc.AddExternalVelocity(dir * internalGlide);
            }
            return;
        }

        internalGlide = 0f;

        if (horizontal)
        {
            // Pushing Current
            if (velocityAlongCurrent < slowfall)
            {
                pc.AddExternalVelocity(dir * slowfall);
            }

            // Guarantee minimum walk speed of 1 when grounded and walking against the current
            if (pc.Grounded)
            {
                Vector2 moveInput = pc.FrameInput;
                // Player is walking against the current direction
                if (moveInput.x != 0 && Mathf.Sign(moveInput.x) != Mathf.Sign(dir.x))
                {
                    float resultingX = vel.x + (dir * slowfall).x;
                    float walkDir = Mathf.Sign(moveInput.x);
                    // If the current is overpowering the walk, clamp to minimum speed 1 in walk direction
                    if (Mathf.Sign(resultingX) != walkDir || Mathf.Abs(resultingX) < 1f)
                    {
                        vel.x = walkDir * 1f;
                    }
                }
            }
        }
        else
        {
            // Slowfall
            if (velocityAlongCurrent < -slowfall)
            {
                vel.y = -slowfall;
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