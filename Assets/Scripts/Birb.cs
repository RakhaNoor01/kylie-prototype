using UnityEngine;

public class BirbController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private bool isFlying = false;

    [Header("Fly Settings")]
    public float flySpeedX = 3f;   // kecepatan horizontal
    public float flySpeedY = 2f;   // kecepatan naik
    public float waveAmplitude = 1f;  // besar gelombang naik turun
    public float waveFrequency = 2f;  // kecepatan gelombang

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
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
            rb.gravityScale = 0f; // gravity tetap 0 biar ga jatuh
        }
    }

    void FixedUpdate()
    {
        if (!isFlying) return;

        // Gerak horizontal + naik turun seperti burung terbang
        float verticalWave = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
        rb.linearVelocity = new Vector2(-flySpeedX, flySpeedY + verticalWave);

        // Hapus burung kalau udah keluar layar
        if (transform.position.x < -20f || transform.position.x > 20f)
            Destroy(gameObject);
    }
}