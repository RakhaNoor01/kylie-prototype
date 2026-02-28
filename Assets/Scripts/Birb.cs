using UnityEngine;

public class BirbController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private bool isFlying = false;

    [Header("Fly Settings")]
    public float flySpeedX = 3f;
    public float flySpeedY = 2f; 
    public float waveAmplitude = 1f;
    public float waveFrequency = 2f;

    [Header("Sleep Settings")]
    public float sleepAnimOffset = -1f; // -1 = random, 0~1 = manual offset

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        // Sleeping Bird Offset
        float offset = sleepAnimOffset < 0f ? Random.Range(0f, 1f) : sleepAnimOffset;
        animator.Play("birbidle", 0, offset);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            TriggerFly();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            TriggerFly();
    }

    void TriggerFly()
    {
        if (!isFlying)
        {
            isFlying = true;
            animator.SetBool("isFlying", true);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 0f;
        }
    }

    void FixedUpdate()
    {
        if (!isFlying) return;

        float verticalWave = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
        rb.linearVelocity = new Vector2(-flySpeedX, flySpeedY + verticalWave);

        if (transform.position.x < -20f || transform.position.x > 20f)
            Destroy(gameObject);
    }
}