using UnityEngine;

public class JacobNode : MonoBehaviour
{
    [Header("Path")]
    public JacobNode NextNode;

    public TravelType TravelToNext = TravelType.Run;
    public NodeAction Action = NodeAction.None;

    public enum TravelType
    {
        Run,
        Jump
    }

    public enum NodeAction
    {
        None,
        Wait,
        Finish
    }

    [Header("Jump")]
    public float jumpHeight = 3f;
    public float jumpDuration = 0.5f;

    [Header("Wait")]
    public float waitTime = 1f;
}