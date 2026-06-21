using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovablePlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public bool pingPong = false;
    public float defaultSpeed = 2f;
    public float inheritLVgrace = 0.5f;

    public List<MovingPlatformTarget> targets = new List<MovingPlatformTarget>();

    private Rigidbody2D rb;
    private int currentIndex = 0;
    private bool goingForward = true;
    private bool tweenin = false;
    private Vector2 previousPos;
    private Vector2 highestLV = Vector2.zero;

    private bool playerOnPlatform = false;
    private bool playerJumped = false;
    private PlayerController player = null;
    private Coroutine resetVelocityCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        previousPos = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        player = collision.gameObject.GetComponent<PlayerController>();
        playerOnPlatform = true;
        playerJumped = false;

        if (resetVelocityCoroutine != null)
        {
            StopCoroutine(resetVelocityCoroutine);
            resetVelocityCoroutine = null;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (playerJumped && player != null)
        {
            var gumbo = new Vector2(
                Mathf.Max(player.FrameVelocity.x, highestLV.x), 
                Mathf.Max(player.FrameVelocity.y, highestLV.y));
            player.SetFrameVelocity(gumbo);
        }

        playerOnPlatform = false;
        playerJumped = false;
        player = null;
    }

    private void Start()
    {
        if (targets.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}'s MovablePlatform script has no targets!");
            enabled = false;
            return;
        }

        rb.position = targets[0].target.position;

        currentIndex = 0;

        StartCoroutine(MoveToNextTarget());
    }

    private void FixedUpdate()
    {
        if (!tweenin) return;

        Vector2 current = rb.position;
        rb.linearVelocity = (current - previousPos) / Time.fixedDeltaTime;
        previousPos = current;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (playerOnPlatform)
                playerJumped = true;
        }
    }

    private IEnumerator ResetHighestLVAfterDelay()
    {
        yield return new WaitForSeconds(inheritLVgrace);
        highestLV = Vector2.zero;
        resetVelocityCoroutine = null;
    }

    private IEnumerator MoveToNextTarget()
    {
        rb.position = targets[0].target.position;
        currentIndex = 0;

        while (true)
        {
            int nextIndex;

            if (pingPong)
            {
                nextIndex = goingForward ? currentIndex + 1 : currentIndex - 1;

                if (nextIndex >= targets.Count)
                {
                    goingForward = false;
                    nextIndex = targets.Count - 2;
                }
                else if (nextIndex < 0)
                {
                    goingForward = true;
                    nextIndex = 1;
                }
            }
            else
            {
                nextIndex = (currentIndex + 1) % targets.Count;
            }

            MovingPlatformTarget movement = targets[currentIndex];
            Transform destination = targets[nextIndex].target;

            Vector2 startPos = rb.position;
            Vector2 endPos = destination.position;
            float distance = Vector2.Distance(startPos, endPos);

            float speed = movement.speed > 0 ? movement.speed : defaultSpeed;

            float duration = distance / speed;

            float elapsed = 0f;

            if (movement.waitTime > 0f)
                yield return new WaitForSeconds(movement.waitTime);

            if (movement.instant)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = destination.position;
            }
            else
            {
                while (elapsed < duration)
                {
                    elapsed += Time.fixedDeltaTime;

                    float normalizedTime = Mathf.Clamp01(elapsed / duration);

                    float curveT = movement.easing.Evaluate(normalizedTime);

                    Vector2 newPos = Vector2.Lerp(startPos, endPos, curveT);

                    Vector2 delta = newPos - rb.position;

                    var newLV = delta / Time.fixedDeltaTime;
                    rb.linearVelocity = newLV;
                    rb.MovePosition(newPos);

                    var hi = Mathf.Abs(Vector2.SqrMagnitude(newLV)); 
                    var hello = Mathf.Abs(Vector2.SqrMagnitude(highestLV));

                    if (hi > hello)
                    {
                        highestLV = newLV;
                    }

                    yield return new WaitForFixedUpdate();
                }

                rb.linearVelocity = Vector2.zero;
                rb.MovePosition(endPos);

                if (resetVelocityCoroutine != null)
                    StopCoroutine(resetVelocityCoroutine);
                resetVelocityCoroutine = StartCoroutine(ResetHighestLVAfterDelay());
            }

            currentIndex = nextIndex;
        }
    }
}