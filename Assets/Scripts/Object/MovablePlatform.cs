using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovablePlatform : ButtonTarget
{
    [Header("Platform Settings")]
    public bool pingPong = false;
    public float defaultSpeed = 2f;
    public float inheritLVGrace = 0.25f;
    public float inheritLVMult = 0.75f;

    public List<MovingPlatformTarget> targets = new List<MovingPlatformTarget>();

    private Rigidbody2D rb;
    private int currentIndex = 0;
    private bool goingForward = true;
    private Vector2 highestLV = Vector2.zero;

    private bool playerJumped = false;
    private PlayerController player = null;
    
    private bool initted = false;
    private bool ogState;

    private Coroutine resetVelocityCoroutine;
    private Coroutine clingCoyoteRoutine;

    public override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        player = collision.gameObject.GetComponent<PlayerController>();
        player.Jumped += OnJumpa;
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
            ApplyVelocity();
        }
        else
        {
            if (clingCoyoteRoutine != null)
                StopCoroutine(clingCoyoteRoutine);

            clingCoyoteRoutine = StartCoroutine(ClingCoyoteInheritance());
        }
    }

    private IEnumerator ClingCoyoteInheritance()
    {
        while (player.ImWallCoyoting)
        { 
            if (player != null && playerJumped)
            {
                ApplyVelocity();
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        clingCoyoteRoutine = null;
    }

    private void ApplyVelocity()
    {
        player.Jumped -= OnJumpa;
        playerJumped = false;

        var gumbo = new Vector2(
            Mathf.Max(player.FrameVelocity.x, highestLV.x),
            Mathf.Max(player.FrameVelocity.y, highestLV.y));
        player.SetFrameVelocity(gumbo * inheritLVMult);

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

        currentIndex = 0;
        rb.position = targets[0].target.position;
        transform.rotation = targets[0].target.rotation;

        if (button == null)
        {
            StartCoroutine(MovingRoutine());
        }
    }

    private IEnumerator MovingRoutine()
    {
        while (true)
        {
            var nextIndex = NextIndex();

            yield return StartCoroutine(MoveToTarget(nextIndex));

            currentIndex = nextIndex;
        }
    }

    public override void SetState(bool newState)
    {
        base.SetState(newState);

        if (!initted && button.type == ButtonType.Toggle)
        {
            initted = true;
            ogState = state;
            return;
        }

        if (button == null) return;

        switch (button.type)
        {
            case ButtonType.OneTime:
            case ButtonType.Timed:
                HandleOneTime();
                break;
            case ButtonType.Toggle:
                HandleToggle();
                break;
        }
    }

    private void HandleOneTime()
    {
        if (targets.Count < 2)
        {
            Debug.LogWarning($"{gameObject.name}: OneTime/Timed button needs at least 2 targets.");
            return;
        }

        int destination = state ? 1 : 0;

        StopAllCoroutines();
        StartCoroutine(ToTargetYum(destination));
    }

    private void HandleToggle()
    {
        var nextIndex = NextIndex();
        currentIndex = nextIndex;
        StopAllCoroutines();
        StartCoroutine(ToTargetYum(nextIndex));
    }

    private void OnJumpa()
    {
        playerJumped = true;
    }

    private IEnumerator ResetHighestLV()
    {
        yield return new WaitForSeconds(inheritLVGrace);
        highestLV = Vector2.zero;
        resetVelocityCoroutine = null;
    }

    private int NextIndex()
    {
        int next;

        if (pingPong)
        {
            next = goingForward ? currentIndex + 1 : currentIndex - 1;

            if (next >= targets.Count)
            {
                goingForward = false;
                next = targets.Count - 2;
            }
            else if (next < 0)
            {
                goingForward = true;
                next = 1;
            }
        }
        else
        {
            next = (currentIndex + 1) % targets.Count;
        }

        return next;
    }

    private IEnumerator ToTargetYum(int index)
    {
        yield return StartCoroutine(MoveToTarget(index));
        currentIndex = index;
    }

    private IEnumerator MoveToTarget(int index)
    {
        blocked = true;

        MovingPlatformTarget movement = targets[currentIndex];
        Transform destination = targets[index].target;

        Vector2 startPos = rb.position;
        Vector2 endPos = destination.position;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = destination.rotation;

        float distance = Vector2.Distance(startPos, endPos);

        float speed = movement.speed > 0 ? movement.speed : defaultSpeed;

        float duration = movement.useDuration ? movement.duration : distance / speed;

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
                Quaternion newRot = Quaternion.Lerp(startRot, endRot, curveT);

                Vector2 delta = newPos - rb.position;

                var newLV = delta / Time.fixedDeltaTime;
                rb.linearVelocity = newLV;
                rb.MovePosition(newPos);
                rb.MoveRotation(newRot);

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

            if (button != null)
            {
                var dontUnblock = button.type == ButtonType.Timed && state != ogState;
                if (!dontUnblock)
                {
                    blocked = false;
                }
            }

            if (resetVelocityCoroutine != null)
                StopCoroutine(resetVelocityCoroutine);
            resetVelocityCoroutine = StartCoroutine(ResetHighestLV());

            yield return null;
        }
    }
}