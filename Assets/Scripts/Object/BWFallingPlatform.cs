using DG.Tweening;
using System.Collections;
using Unity.VisualScripting;
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

    [Header("Behaviour Options")]
    [Tooltip("If true, the platform will never go unsolid or transparent after falling")]
    public bool noUnsolid = false;
    [Tooltip("If true, the platform will not respawn after falling")]
    public bool noRespawn = false;
    [Tooltip("If true, the platform will automatically fall when the player touches it")]
    public bool fallOnTouch = true;

    [Header("Linear Fall")]
    public bool linearFall = false;
    public float linearFallSpeed = 3f;

    private Rigidbody2D rb;
    private Collider2D coll;
    private SpriteRenderer sprite;
    private Vector2 ogPos;
    private Quaternion ogRot;
    private bool doingAThing = false;
    private bool waitingForStopCollision = false;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && fallOnTouch)
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (waitingForStopCollision && stopCollision != null &&
            collision.gameObject == stopCollision)
        {
            waitingForStopCollision = false;
        }
    }

    public void StartFalling()
    {
        if (doingAThing) return;
        StartCoroutine(WaitFallRespawn());
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
            rb.linearVelocity = Vector2.down * linearFallSpeed;
        }

        // --- SOLID PHASE ---
        if (!noUnsolid)
        {
            if (stopCollision == null)
            {
                yield return new WaitForSeconds(solidTime);
            }
            else
            {
                waitingForStopCollision = true;
                yield return new WaitUntil(() => !waitingForStopCollision);

                if (noRespawn)
                {
                    gameObject.SetActive(false);
                    yield break;
                }
            }

            // --- UNSOLID / DISAPPEAR ---
            coll.isTrigger = true;
            sprite.DOFade(0.5f, 0);
            if (particle) particle.Stop();
        }

        // --- RESPAWN ---
        if (!noRespawn)
        {
            yield return new WaitForSeconds(respawnWait);

            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 1f;
            transform.position = ogPos;
            transform.rotation = ogRot;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            coll.isTrigger = false;
            sprite.DOFade(1f, 0);
            doingAThing = false;
        }
    }
}