using UnityEngine;

public class PlayerKnockback : MonoBehaviour
{
    [Header("Knockback Settings")]
    public float knockbackForce = 12f;
    public float knockbackDuration = 0.3f;
    public float upwardBoost = 0.5f; // How much upward arc
    public AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Direction Detection")]
    public bool useFacingDirection = true; // Use player's facing direction
    public bool useHitDirection = false;    // Use direction from hazard to player

    private Rigidbody2D rb;
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector2 knockbackDirection;

    // Reference to your player's sprite or facing component
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerTransform = transform;
    }

    private void Update()
    {
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isKnockedBack && rb != null)
        {
            float t = 1 - (knockbackTimer / knockbackDuration);
            float curveValue = knockbackCurve.Evaluate(t);
            rb.linearVelocity = knockbackDirection * knockbackForce * curveValue;
        }
    }

    // Called by DeathZone - uses facing direction
    public void ApplyKnockback()
    {
        Vector2 direction = GetKnockbackDirection();
        ApplyKnockbackWithDirection(direction);
    }

    // Called by DeathZone - uses hit direction (from hazard)
    public void ApplyKnockback(Vector2 hitDirection)
    {
        Vector2 direction;

        if (useHitDirection)
        {
            // Use the direction from the hazard
            direction = hitDirection.normalized;
        }
        else
        {
            // Use facing direction instead
            direction = GetKnockbackDirection();
        }

        ApplyKnockbackWithDirection(direction);
    }

    private Vector2 GetKnockbackDirection()
    {
        Vector2 direction = Vector2.zero;

        if (useFacingDirection && spriteRenderer != null)
        {
            // Check if sprite is flipped (facing left)
            if (spriteRenderer.flipX)
            {
                // Facing left → knock to the RIGHT
                direction = new Vector2(1, upwardBoost);
                Debug.Log("Facing left - knocking right");
            }
            else
            {
                // Facing right → knock to the LEFT
                direction = new Vector2(-1, upwardBoost);
                Debug.Log("Facing right - knocking left");
            }
        }
        else
        {
            // Default knockback (up)
            direction = new Vector2(0, 1);
        }

        return direction.normalized;
    }

    private void ApplyKnockbackWithDirection(Vector2 direction)
    {
        knockbackDirection = direction;
        knockbackTimer = knockbackDuration;
        isKnockedBack = true;

        Debug.Log($"Knockback applied! Direction: {direction}");
    }

    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }
}