using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TarodevController; // for player controller detection

[System.Serializable]
public class PlatformTarget
{
    [Tooltip("The transform the platform will move towards.")]
    public Transform target;

    [Tooltip("How long the platform pauses at this point before moving on.")]
    public float waitTime = 0f;

    [Tooltip("Custom speed for this leg. Zero means use the platform's default.")]
    public float speed = 0f;
}

public class MovingPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public List<PlatformTarget> targets = new List<PlatformTarget>();
    public bool pingPong = false;
    public float defaultSpeed = 3f;

    [Header("Activation Settings")]
    [Tooltip("If true the platform will only start moving when the player lands on (or collides with) it.")]
    public bool requirePlayerActivation = false;

    [Tooltip("If true the platform will only start moving when a Boomerang hits it.")]
    public bool requireBoomerangActivation = false;

    [Tooltip("Tag used to detect a boomerang hit (required if requireBoomerangActivation is true)")]
    public string boomerangTag = "Goonerang";

    // internal state
    private bool _pendingActivation = false; // set when one of the triggers occurs
    private bool _isMovingTrip = false;      // true while the platform is traversing a full circuit

    private Rigidbody2D rb;
    private int currentIndex = 0;
    private bool goingForward = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        if (targets.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}'s MovingPlatform script has no targets!");
            enabled = false;
            return;
        }

        StartCoroutine(MoveToNextTarget());
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ignore collisions while already traversing a trip
        if (_isMovingTrip)
            return;

        // player landing
        if (requirePlayerActivation)
        {
            var player = collision.collider.GetComponent<PlayerController>();
            if (player != null)
            {
                foreach (var contact in collision.contacts)
                {
                    if (contact.normal.y > 0.5f)
                    {
                        _pendingActivation = true;
                        return;
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // catch boomerang hits even if it's a trigger collider
        if (_isMovingTrip)
            return;

        if (requireBoomerangActivation && other.gameObject.CompareTag(boomerangTag))
        {
            _pendingActivation = true;
        }
    }

    private IEnumerator MoveToNextTarget()
    {
        while (true)
        {
            bool requiresActivation = requirePlayerActivation || requireBoomerangActivation;

            if (requiresActivation && !_pendingActivation)
            {
                yield return null;
                continue;
            }

            // mark the start of movement
            _isMovingTrip = true;

            if (!requiresActivation)
            {
                // simple case: just move one leg and then pause
                PlatformTarget targetData = targets[currentIndex];
                float speed = targetData.speed > 0 ? targetData.speed : defaultSpeed;

                if (targetData.waitTime > 0f)
                    yield return new WaitForSeconds(targetData.waitTime);

                while (Vector2.Distance(rb.position, targetData.target.position) > 0.01f)
                {
                    Vector2 newPos = Vector2.MoveTowards(rb.position, targetData.target.position, speed * Time.fixedDeltaTime);
                    Vector2 deltaMove = newPos - rb.position;
                    MovePlatform(deltaMove);
                    yield return new WaitForFixedUpdate();
                }

                rb.position = targetData.target.position;

                // advance one index/direction
                if (pingPong)
                {
                    if (goingForward)
                    {
                        currentIndex++;
                        if (currentIndex >= targets.Count - 1)
                            goingForward = false;
                    }
                    else
                    {
                        currentIndex--;
                        if (currentIndex <= 0)
                            goingForward = true;
                    }
                }
                else
                {
                    currentIndex = (currentIndex + 1) % targets.Count;
                }

                _isMovingTrip = false;
                continue;
            }

            // activation required -> perform full circuit back to start
            _pendingActivation = false;
            int startIndex = currentIndex;
            bool startForward = goingForward;

            do
            {
                PlatformTarget targetData = targets[currentIndex];
                float speed = targetData.speed > 0 ? targetData.speed : defaultSpeed;

                if (targetData.waitTime > 0f)
                    yield return new WaitForSeconds(targetData.waitTime);

                while (Vector2.Distance(rb.position, targetData.target.position) > 0.01f)
                {
                    Vector2 newPos = Vector2.MoveTowards(rb.position, targetData.target.position, speed * Time.fixedDeltaTime);
                    Vector2 deltaMove = newPos - rb.position;
                    MovePlatform(deltaMove);
                    yield return new WaitForFixedUpdate();
                }

                rb.position = targetData.target.position;

                if (pingPong)
                {
                    if (goingForward)
                    {
                        currentIndex++;
                        if (currentIndex >= targets.Count - 1)
                            goingForward = false;
                    }
                    else
                    {
                        currentIndex--;
                        if (currentIndex <= 0)
                            goingForward = true;
                    }
                }
                else
                {
                    currentIndex = (currentIndex + 1) % targets.Count;
                }

            } while (currentIndex != startIndex || goingForward != startForward);

            _isMovingTrip = false;
        }
    }

    private void MovePlatform(Vector2 deltaMove)
    {
        // Move the platform itself
        rb.position += deltaMove;

        // Move or influence objects on top
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            rb.position,
            GetComponent<Collider2D>().bounds.size,
            0f,
            Vector2.up,
            0.01f,
            LayerMask.GetMask("Default")
        );

        foreach (var hit in hits)
        {
            Rigidbody2D hitRb = hit.collider.attachedRigidbody;
            if (hitRb != null && hitRb.transform.position.y > rb.position.y)
            {
                // if the object is the player, give it velocity so it rides along
                var player = hitRb.GetComponent<PlayerController>();
                bool boomerangHit = hit.collider.gameObject.CompareTag(boomerangTag);

                // check for activation triggers while object rides on top
                if (!_isMovingTrip)
                {
                    if (player != null && requirePlayerActivation)
                        _pendingActivation = true;
                    if (boomerangHit && requireBoomerangActivation)
                        _pendingActivation = true;
                }

                if (player != null)
                {
                    // convert the delta movement into a horizontal velocity and hand it
                    // off to the controller, which will mix it in with its own logic.
                    float platformVelX = deltaMove.x / Time.fixedDeltaTime;
                    player.AddPlatformVelocity(new Vector2(platformVelX, 0f));
                }
                else
                {
                    // fallback for generic rigidbodies: directly move them to avoid clipping
                    hitRb.position += deltaMove;
                }
            }
        }
    }
}

