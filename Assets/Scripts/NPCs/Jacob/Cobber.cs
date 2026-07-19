using UnityEngine;

public class Cobber : MonoBehaviour
{
    public enum JacobState
    {
        Idle,
        Running,
        Waiting,
        Jumping
    }

    [Header("Movement")]
    [SerializeField] private float runSpeed = 5f;

    private JacobState _state = JacobState.Idle;

    private JacobNode currentNode;
    private JacobNode targetNode;

    public JacobState State => _state;
    public Vector2 Velocity { get; private set; }



    private float waitTimer;



    private Vector3 jumpStart;
    private Vector3 jumpEnd;

    private float jumpHeight;
    private float jumpDuration;
    private float jumpTimer;


    private void Update()
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
                HandleWaiting();
                break;

            case JacobState.Jumping:
                HandleJump();
                break;
        }
    }


    public void StartRun(JacobNode firstNode)
    {
        transform.position = firstNode.transform.position;
        ArriveNode(firstNode);
    }


    private void ArriveNode(JacobNode node)
    {
        currentNode = node;

        Debug.Log($"Arrived at {currentNode?.name}");

        if (currentNode == null)
        {
            _state = JacobState.Idle;
            return;
        }

        switch (currentNode.Action)
        {
            case JacobNode.NodeAction.None:
                TravelToNext();
                break;

            case JacobNode.NodeAction.Wait:
                BeginWait();
                break;

            case JacobNode.NodeAction.Finish:
                _state = JacobState.Idle;
                break;
        }
    }

    
    private void TravelToNext()
    {
        targetNode = currentNode.NextNode;

        if (targetNode == null)
        {
            _state = JacobState.Idle;
            return;
        }

        switch (currentNode.TravelToNext)
        {
            case JacobNode.TravelType.Run:
                BeginRun();
                break;

            case JacobNode.TravelType.Jump:
                BeginJump();
                break;
        }
    }

    
    // RUN
    

    private void BeginRun()
    {
        _state = JacobState.Running;
    }

    private void HandleRunning()
    {
        Vector3 target = targetNode.transform.position;

        Vector3 direction =
            (target - transform.position).normalized;

        Velocity = direction * runSpeed;

        transform.position +=
            (Vector3)(Velocity * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.05f)
        {
            transform.position = target;
            ArriveNode(targetNode);
        }
    }
    
    // JUMP
    

    private void BeginJump()
    {
        jumpStart = currentNode.transform.position;
        jumpEnd = targetNode.transform.position;

        jumpHeight = currentNode.jumpHeight;
        jumpDuration = currentNode.jumpDuration;

        jumpTimer = 0f;

        _state = JacobState.Jumping;
    }

    private void HandleJump()
    {
        jumpTimer += Time.deltaTime;

        float t = Mathf.Clamp01(jumpTimer / jumpDuration);

        Vector3 pos =
            Vector3.Lerp(jumpStart, jumpEnd, t);

        pos.y +=
            4f * t * (1f - t) * jumpHeight;

        Velocity =
            (pos - transform.position) / Time.deltaTime;

        transform.position = pos;

        if (t >= 1f)
        {
            transform.position = jumpEnd;
            ArriveNode(targetNode);
        }
    }

   
    // WAIT
   
    private void BeginWait()
    {
        waitTimer = currentNode.waitTime;

        Velocity = Vector2.zero;

        _state = JacobState.Waiting;
    }

    private void HandleWaiting()
    {
        Velocity = Vector2.zero;

    }

    public void ContinuePath()
    {
        if (_state != JacobState.Waiting)
            return;

        TravelToNext();
    }
}