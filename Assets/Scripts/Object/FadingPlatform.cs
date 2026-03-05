using DG.Tweening;
using System.Collections;
using UnityEngine;

public class FadingPlatform : MonoBehaviour
{
    //amount of time the platform stays before falling, when touched by the player
    public float stayTime = 1f;
    //amount of time the platform stays solid after it starts falling
    public float solidTime = 1f;
    //amoount of time it takes for the platform to respawn after going unsolid
    public float respawnWait = 3f;
    public ParticleSystem particle;

    //optional object that cancels the solid time
    public GameObject stopCollision;

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
        if (collision.gameObject.CompareTag("Player"))
        {
            if (doingAThing) return;
            StartCoroutine(WaitFallRespawn());
        }

        // NEW: Detect stopCollision contact
        if (waitingForStopCollision && stopCollision != null &&
            collision.gameObject == stopCollision)
        {
            waitingForStopCollision = false;
        }
    }

    private IEnumerator WaitFallRespawn()
    {
        doingAThing = true;

        yield return new WaitForSeconds(stayTime);

        rb.bodyType = RigidbodyType2D.Dynamic;

        // --- SOLID PHASE ---
        if (stopCollision == null)
        {
            // Default mode
            yield return new WaitForSeconds(solidTime);
        }
        else
        {
            // Alternative mode
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

        transform.position = ogPos;
        transform.rotation = ogRot;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;

        coll.isTrigger = false;
        sprite.DOFade(1f, 0);
        if (particle) particle.Play();

        doingAThing = false;
    }
}