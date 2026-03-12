using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MovablePlatformTarget
{
    public Transform target;
    public float waitTime = 0f;
    public float speed = 0f; // 0 = use default
}

[RequireComponent(typeof(Rigidbody2D))]
public class MovablePlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public List<MovablePlatformTarget> targets = new List<MovablePlatformTarget>();
    public bool pingPong = false;
    public float defaultSpeed = 2f;

    private Rigidbody2D _rb;
    private Vector2 _lastPosition;
    private int _currentIndex = 0;
    private bool _goingForward = true;

    public Vector2 Delta { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.freezeRotation = true;
    }

    private void Start()
    {
        if (targets.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}'s MovablePlatform script has no targets!");
            enabled = false;
            return;
        }

        _lastPosition = _rb.position;
        StartCoroutine(MoveToNextTarget());
    }

    private IEnumerator MoveToNextTarget()
    {
        while (true)
        {
            MovablePlatformTarget targetData = targets[_currentIndex];
            float speed = targetData.speed > 0 ? targetData.speed : defaultSpeed;

            if (targetData.waitTime > 0f)
                yield return new WaitForSeconds(targetData.waitTime);

            while (Vector2.Distance(_rb.position, targetData.target.position) > 0.01f)
            {
                Vector2 newPos = Vector2.MoveTowards(
                    _rb.position,
                    targetData.target.position,
                    speed * Time.fixedDeltaTime
                );

                Delta = newPos - _rb.position;
                _rb.MovePosition(newPos);
                _lastPosition = newPos;

                yield return new WaitForFixedUpdate();
            }

            // Snap and finalize delta
            _rb.MovePosition(targetData.target.position);
            Delta = (Vector2)targetData.target.position - _lastPosition;
            _lastPosition = targetData.target.position;

            yield return new WaitForFixedUpdate();

            // Advance index
            if (pingPong)
            {
                if (_goingForward)
                {
                    _currentIndex++;
                    if (_currentIndex >= targets.Count - 1)
                        _goingForward = false;
                }
                else
                {
                    _currentIndex--;
                    if (_currentIndex <= 0)
                        _goingForward = true;
                }
            }
            else
            {
                _currentIndex = (_currentIndex + 1) % targets.Count;
            }
        }
    }
}