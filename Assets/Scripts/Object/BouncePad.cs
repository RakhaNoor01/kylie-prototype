using TarodevController;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BouncePad : MonoBehaviour
{
    public float force = 10f;

    [Header("Horizontal bounce pad slop")]
    public bool horizontal = false;
    [Tooltip("Set to negative to use the air/ground deceleration from player stats")]
    public float horizontalDecel = -1;
    public float decelMult = 0.75f;
    [Tooltip("Immunity timer against player velocity getting zeroed when crashing into a wall")]
    public float wallImmunityTimer = 0.1f;

    public float internalVelocity = 0;
    public float decel;
    private PlayerController pc;
    private bool boioioing;

    private float getOffThatWallDawg = 0;

    public bool gbug;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (pc == null)
        {
            pc = FindFirstObjectByType<PlayerController>();
        }

        if (horizontalDecel  == 0)
        {
            horizontalDecel = -1;
        }
    }

    private void FixedUpdate()
    {
        HandleHorizontalPads();
    }

    private void Update()
    {
        if (gbug) Debug.Log($"{internalVelocity} {decel}");
    }

    private void HandleHorizontalPads()
    {
        if (pc.IsDashing || (pc.blockExVel && getOffThatWallDawg < 0))
        {
            internalVelocity = 0;
        }

        if (internalVelocity > 0)
        {
            var stats = pc.Stats;
            var airRatio = stats.GroundDeceleration / stats.AirDeceleration * decelMult;

            var toReduce = horizontalDecel > -1 ? 
                (pc.Grounded ? horizontalDecel * airRatio : horizontalDecel) :
                (pc.Grounded ? pc.Stats.GroundDeceleration : pc.Stats.AirDeceleration);
            decel = toReduce;

            internalVelocity -= toReduce * Time.fixedDeltaTime;
            internalVelocity = Mathf.Max(0, internalVelocity);
            Vector2 launchVelocity = transform.up * internalVelocity;
            pc.AddExternalVelocity(launchVelocity);
        }

        if (getOffThatWallDawg > 0)
        {
            getOffThatWallDawg -= Time.fixedDeltaTime;
        }

        if (boioioing)
        {
            pc.SetFrameVelocity(new Vector2(0, pc.Stats.JumpPower));
            boioioing = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || pc == null) return;

        // Launch
        pc.CancelDash();

        if (horizontal)
        {
            internalVelocity = force;
            boioioing = true;
            getOffThatWallDawg = wallImmunityTimer;
        } 
        else
        {
            Vector2 launchVelocity = transform.up * force;
            pc.SetFrameVelocity(launchVelocity);
        }

        // Refresh Mobility
        pc.RechargeDash();
        pc.ResetGlide();
        var rang = pc.gameObject.GetComponentInChildren<Boomerang>();
        if (rang != null)
        {
            rang.hasTped = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + transform.up * force * 0.25f;
        Gizmos.DrawLine(start, end);
    }
}