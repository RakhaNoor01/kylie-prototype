using UnityEngine;

public class JacobNode : MonoBehaviour
{

    public enum NodeType
    {
        Run,
        Jump,
        Wait,
        Finish
    }


    [Header("Node")]
    public NodeType Type;

    public JacobNode NextNode;

    [Header("Jump")]
    public float JumpHeight = 3f;
    public float JumpDuration = 0.5f;

    [Header("Wait")]
    public float WaitTime = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
