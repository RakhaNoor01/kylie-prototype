using System.Threading;
using TarodevController;
using UnityEngine;

[ExecuteAlways]
public class AirCurrent : MonoBehaviour
{
    public float slowfall = 1f;
    public float glideAccel = 5f;
    public float maxGlide = 10f;
    public bool horizontal = false;
    public float particleRate = 1;
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

    private void Start()
    {
        UpdateParticle();
    }

    private void Update()
    {
#if UNITY_EDITOR
        UpdateParticle();
#endif
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var pc = collision.GetComponent<PlayerController>();
        if (pc != null && internalGlide > 0f)
        {
            Vector2 vel = pc.FrameVelocity;
            float velocityAlongCurrent = Vector2.Dot(vel, CurrentDirection);
            float deficit = internalGlide - velocityAlongCurrent;
            if (deficit > 0f)
            {
                vel += CurrentDirection * deficit;
                pc.SetFrameVelocity(vel);
            }
        }

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

            // Only push the Deficit: the gap between where they are and the target speed
            // This prevents accumulating external velocity on top of frameVelocity
            float deficit = internalGlide - velocityAlongCurrent;
            if (deficit > 0f)
            {
                pc.AddExternalVelocity(dir * deficit);
            }
            return;
        }

        if (internalGlide > 0f)
        {
            float deficit = internalGlide - velocityAlongCurrent;
            if (deficit > 0f)
            {
                vel += dir * deficit;
                pc.SetFrameVelocity(vel);
            }
            internalGlide = 0f;
            return;
        }

        if (horizontal)
        {
            // Pushing Current
            if (velocityAlongCurrent < slowfall)
            {
                float push = slowfall;

                // Player has a at least 1 walk speed to fight against the aircurrent
                if (velocityAlongCurrent < 0f)
                {
                    // Player's walk speed in their own direction (positive value)
                    float playerWalkSpeed = Mathf.Abs(velocityAlongCurrent);
                    float maxAllowedPush = Mathf.Max(0f, pc.Stats.MaxSpeed - 1.5f);
                    push = Mathf.Min(push, maxAllowedPush);
                }

                pc.AddExternalVelocity(dir * push);
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

    private void UpdateParticle()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        var col = GetComponent<Collider2D>();
        float emitterSize = 1f;
        if (col != null)
        {
            Bounds b = col.bounds;
            emitterSize = b.size.x * b.size.y / 4;
        }

        var emission = ps.emission;
        emission.rateOverTime = emitterSize * particleRate;
    }
}