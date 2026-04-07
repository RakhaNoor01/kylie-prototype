using DG.Tweening;
using System.Collections;
using UnityEngine;

public class BWFallingPlatform : MonoBehaviour
{
    [Tooltip("Amount of time the platform stays before falling, when touched by the player")]
    public float stayTime = 1f;

    [Tooltip("Amount of time the platform stays solid after it starts falling")]
    public float solidTime = 1f;

    [Tooltip("Amount of time it takes for the platform to respawn after going unsolid")]
    public float respawnWait = 3f;

    [Tooltip("Optional object that cancels the solid time")]
    public GameObject stopCollision;

    public ParticleSystem particle;

    [Header("Linear Fall")]
    // If true, the platform falls at a constant speed instead of accelerating
    public bool linearFall = false;
    // Speed of the linear fall (units per second)
    public float linearFallSpeed = 3f;

    private Rigidbody2D rb;
    private Collider2D coll;
    private SpriteRenderer sprite;
    private Vector2 ogPos;
    private Quaternion ogRot;
    private bool doingAThing = false;
    private bool waitingForStopCollision = false;
    private bool isFallingLinearly = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        coll = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        ogPos = transform.position;
        ogRot = transform.rotation;
    }

    private void Update()
    {
        if (rb.bodyType == RigidbodyType2D.Static)
        {
            transform.position = ogPos;
            transform.rotation = ogRot;
        }
    }

    private void FixedUpdate()
    {
        if (isFallingLinearly)
        {
            Vector2 newPos = rb.position + Vector2.down * linearFallSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (doingAThing) return;
            StartCoroutine(WaitFallRespawn());
        }

        if (waitingForStopCollision && stopCollision != null &&
            collision.gameObject == stopCollision)
        {
            waitingForStopCollision = false;
        }
    }

    private IEnumerator WaitFallRespawn()
    {
        doingAThing = true;
        if (particle) particle.Play();
        yield return new WaitForSeconds(stayTime);

        // --- BEGIN FALL ---
        rb.bodyType = RigidbodyType2D.Dynamic;

        if (linearFall)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            isFallingLinearly = true;
        }

        // --- SOLID PHASE ---
        if (stopCollision == null)
        {
            yield return new WaitForSeconds(solidTime);
        }
        else
        {
            waitingForStopCollision = true;
            yield return new WaitUntil(() => !waitingForStopCollision);
        }

        // --- UNSOLID / DISAPPEAR ---
        coll.isTrigger = true;
        sprite.DOFade(0.5f, 0);
        if (particle) particle.Stop();
        yield return new WaitForSeconds(respawnWait);

        // --- RESET ---
        rb.bodyType = RigidbodyType2D.Static;
        rb.gravityScale = 1f;
        transform.position = ogPos;
        transform.rotation = ogRot;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        coll.isTrigger = false;
        isFallingLinearly = false;
        sprite.DOFade(1f, 0);
        doingAThing = false;
    }
}