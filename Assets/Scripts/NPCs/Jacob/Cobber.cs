using UnityEngine;

public class Cobber : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    public enum JacobState
    {
        Idle,
        Running,
        Waiting,
        Jumping
    }

    [SerializeField] private float runSpeed = 5f;

    [SerializeField]
    private JacobState _state = JacobState.Idle;

    [SerializeField]
    private JacobNode currentNode;

    public JacobNode CurrentNode => currentNode;

    public Vector2 Velocity { get; private set; }

    // Update is called once per frame
    void Update()
    {
        switch (_state)
        {
            case JacobState.Idle:
                Velocity = Vector2.zero;
                break;

            case JacobState.Running:
                HandleRunning();
                break;

            case JacobState.Waiting:
                Velocity = Vector2.zero;
                break;

            case JacobState.Jumping:
                Jump();
                break;
        }
    }

    public void HandleRunning()
    {
        if (currentNode == null) return;

        Vector3 target = CurrentNode.transform.position;
        Vector3 direction = (target - transform.position).normalized;
        Velocity = direction * runSpeed;

        transform.position += (Vector3)(Velocity * Time.deltaTime);

        if(Vector2.Distance(transform.position, target) < 0.05f)
        {
            currentNode = CurrentNode.NextNode;
        }
    }

    public void StartRun(JacobNode firstNode)
    {
        currentNode = firstNode;
        _state = JacobState.Running;
    }

    public void Wait()
    {
        _state = JacobState.Waiting;
    }

    public void Jump()
    {
        _state = JacobState.Jumping;
    }
    public JacobState State => _state;
}
