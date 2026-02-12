using TarodevController;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Boomerang : MonoBehaviour
{
    public GameObject player;

    [Header("Throw")]
    public float throwPower = 15f;

    [Header("Return")]
    public float returnLerpStrength = 1f;
    public float maxSpeed = 25f;
    public float distanceMult = 10f;

    [Header("Aiming")]
    public float slowDown = 0.25f;

    [Header("Visuals")]
    public GameObject directionIndicator;

    private Rigidbody2D rb;
    private Collider2D col;

    private bool isThrown = false;
    private bool hasDeflected = false;

    private SpriteRenderer rangSprite;
    private TrailRenderer rangTrail;

    private bool isCharging = false;
    private Vector2 cachedDirection;

    private float ogTime;
    private float ogDelta;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rangSprite = GetComponent<SpriteRenderer>();
        rangTrail = GetComponent<TrailRenderer>();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        col.isTrigger = true;

        if (directionIndicator != null)
            directionIndicator.SetActive(false);

        rangSprite.enabled = false;
        rangTrail.enabled = false;

        ogTime = Time.timeScale;
        ogDelta = Time.fixedDeltaTime;
    }

    void Update()
    {
        if (isThrown) return;

        // Start aiming
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;

            if (directionIndicator != null)
                directionIndicator.SetActive(true);
        }

        // While holding mouse
        if (isCharging)
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mouseWorld - (Vector2)transform.position).normalized;
            direction = SnapTo8Directions(direction);

            cachedDirection = direction;

            // Rotate indicator
            if (directionIndicator != null)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                directionIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Slow time down
            Time.timeScale = slowDown;
            Time.fixedDeltaTime = ogDelta*slowDown;
        }

        // Release to throw
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            isCharging = false;

            if (directionIndicator != null)
                directionIndicator.SetActive(false);

            Time.timeScale = ogTime;
            Time.fixedDeltaTime = ogDelta;

            Throw();
        }
    }

    void FixedUpdate()
    {
        if (!isThrown) return;

        Vector2 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = toPlayer.normalized;

        // Stronger pull when closer
        float distanceFactor = Mathf.Clamp01(1f / (distance + 0.1f));
        float scaledSpeed = maxSpeed * (1f + distanceFactor * distanceMult);

        // Steer velocity toward player direction
        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            direction * scaledSpeed,
            returnLerpStrength * Time.fixedDeltaTime
        );

        // Clamp
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    void Throw()
    {
        isThrown = true;
        hasDeflected = false;

        transform.parent = null;
        rb.bodyType = RigidbodyType2D.Dynamic;

        rb.linearVelocity = cachedDirection * throwPower;

        rangSprite.enabled = true;
        rangTrail.enabled = true;
    }

    Vector2 SnapTo8Directions(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angle / 45f) * 45f;
        float rad = snapped * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isThrown) return;

        // Catch player
        if (other.gameObject == player)
        {
            Catch();
            return;
        }

        // Deflect off first non-player collision
        if (!hasDeflected)
        {
            hasDeflected = true;
            Deflect(other);
        }
    }

    void Deflect(Collider2D other)
    {
        Vector2 toPlayer = (player.transform.position - transform.position).normalized;

        // Keep current speed but redirect toward player
        float currentSpeed = rb.linearVelocity.magnitude;
        rb.linearVelocity = toPlayer * currentSpeed;
    }

    void Catch()
    {
        // Check pogo condition BEFORE resetting velocity
        bool caughtFromBelow = false;

        Vector2 boomerangVelocity = rb.linearVelocity;
        float playerY = player.transform.position.y;
        float boomerangY = transform.position.y;

        // Was moving upward AND player was above?
        if (boomerangVelocity.y > 0f && playerY > boomerangY)
        {
            caughtFromBelow = true;
        }

        isThrown = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        transform.position = player.transform.position;
        transform.parent = player.transform;

        // Trigger pogo
        if (caughtFromBelow)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ActivatePogoWindow();
            }
        }

        rangSprite.enabled = false;
        rangTrail.enabled = false;
    }
}
