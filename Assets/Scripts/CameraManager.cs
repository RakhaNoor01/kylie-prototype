using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    public void MoveToRoom(Vector3 newPosition)
    {
        targetPosition = new Vector3(
            newPosition.x,
            newPosition.y,
            transform.position.z
        );
    }
}
