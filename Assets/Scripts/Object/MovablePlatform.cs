using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// defines how the platform should move towards the next target
[System.Serializable]
public class MovablePlatformTarget
{
    public Transform target;
    public float waitTime = 0f;
    public float speed = 0f; // 0 = default speed
    public bool useDuration = false;
    public float duration = 1f;
    public bool instant = false;
    public Ease ease = Ease.Linear;
}

[RequireComponent(typeof(Rigidbody2D))]
public class MovablePlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public List<MovablePlatformTarget> targets = new List<MovablePlatformTarget>();
    public bool pingPong = false;
    public float defaultSpeed = 2f;

    private Rigidbody2D rb;
    private int currentIndex = 0;
    private bool goingForward = true;
    private bool tweenin = false;
    private Vector2 previousPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        previousPos = transform.position;
    }

    private void Start()
    {
        if (targets.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}'s MovablePlatform script has no targets!");
            enabled = false;
            return;
        }

        StartCoroutine(MoveToNextTarget());
    }

    private void FixedUpdate()
    {
        if (!tweenin) return;

        Vector2 current = rb.position;
        rb.linearVelocity = (current - previousPos) / Time.fixedDeltaTime;
        previousPos = current;
    }

    private IEnumerator MoveToNextTarget()
    {
        rb.position = targets[0].target.position;
        currentIndex = 0;

        while (true)
        {
            tweenin = true;
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

            MovablePlatformTarget movement = targets[currentIndex];
            Vector3 destination = targets[nextIndex].target.position;

            if (movement.waitTime > 0)
                yield return new WaitForSeconds(movement.waitTime);

            if (movement.instant)
            {
                rb.position = destination;
            }
            else
            {
                Tween tween;

                if (movement.useDuration)
                {
                    tween = rb.DOMove(destination, movement.duration);
                }
                else
                {
                    float speed = movement.speed > 0 ? movement.speed : defaultSpeed;
                    float distance = Vector2.Distance(rb.position, destination);
                    float duration = distance / speed;

                    tween = rb.DOMove(destination, duration);
                }

                tween.SetEase(movement.ease).SetUpdate(UpdateType.Fixed);

                tween.OnComplete(() =>
                {
                    tweenin = false;
                });

                yield return tween.WaitForCompletion();
            }

            currentIndex = nextIndex;
        }
    }
}