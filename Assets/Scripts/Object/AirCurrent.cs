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

        Vector2 dir = CurrentDirection;
        Vector2 vel = pc.Velocity;

        bool jumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C);
        bool wantsGlide = jumpHeld && !pc.Grounded;

        if (wantsGlide)
        {
            internalGlide = Mathf.Min(internalGlide + glideAccel * Time.deltaTime, maxGlide);

            float velocityAlongCurrent = Vector2.Dot(vel, dir);

            if (velocityAlongCurrent < internalGlide)
            {
                pc.AddExternalVelocity(dir * internalGlide);
            }
        }
        else
        {
            internalGlide = 0f;

            float velocityAlongCurrent = Vector2.Dot(vel, dir);

            if (horizontal)
            {
                // Pushing Current
                if (velocityAlongCurrent < slowfall)
                {
                    pc.AddExternalVelocity(dir * slowfall);
                }
            }
            else
            {
                // Slowfall
                if (velocityAlongCurrent < -slowfall)
                {
                    float correction = (-slowfall) - velocityAlongCurrent;
                    pc.AddExternalVelocity(dir * correction);
                }
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