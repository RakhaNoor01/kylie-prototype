using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovablePlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;

    private Rigidbody2D _rb;
    private Vector2 _target;
    private Vector2 _lastPosition;

    public Vector2 Delta { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Start()
    {
        _target = pointB.position;
        _lastPosition = _rb.position;
    }

    private void FixedUpdate()
    {
        Move();
        CalculateDelta();
    }

    private void Move()
    {
        Vector2 newPos = Vector2.MoveTowards(
            _rb.position,
            _target,
            speed * Time.fixedDeltaTime
        );

        _rb.MovePosition(newPos);

        if (Vector2.Distance(newPos, _target) < 0.01f)
        {
            _target = (Vector2)_target == (Vector2)pointA.position
                ? pointB.position
                : pointA.position;
        }
    }

    private void CalculateDelta()
    {
        Delta = _rb.position - _lastPosition;
        _lastPosition = _rb.position;
    }
}