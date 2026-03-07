using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public float moveSpeed = 5f; // lerp fast
    public float roomWidth = 12f; // Room Width sesuai desain level yawh
    public float deadzoneWidth = 2f; // Width deadzone X only
    public Transform player; // Player.

    private Vector3 targetPosition;
    private float currentRoomCenterX;
    private float fixedY; // Nilai Y for camera fixed

    public event System.Action OnRoomChanged;

    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        targetPosition = transform.position;
        fixedY = transform.position.y; // Lock Y to the initial Y position of the camera
        currentRoomCenterX = CalculateRoomCenterX(transform.position.x);
    }

    void Update()
    {
        // Hitung bounds kamera fo r X only
        Vector3 camPos = transform.position;
        float camHalfWidth = Camera.main.orthographicSize * (Screen.width / Screen.height);
        
        // Bounds X: left, right (with deadzone)
        float leftBound = camPos.x - camHalfWidth + deadzoneWidth / 2;
        float rightBound = camPos.x + camHalfWidth - deadzoneWidth / 2;

        float playerX = player.position.x;

        // Cek outOfBounds
        bool outOfBounds = playerX < leftBound || playerX > rightBound;

        if (outOfBounds)
        {
            // counting room based X only, snap ke center room X
            float newRoomCenterX = CalculateRoomCenterX(playerX);
            if (newRoomCenterX != currentRoomCenterX)
            {
                currentRoomCenterX = newRoomCenterX;
                MoveToRoom(new Vector3(newRoomCenterX, fixedY, transform.position.z));
            }
            else
            {
                // Adjust target X only if in the same room
                targetPosition.x = Mathf.Clamp(playerX, currentRoomCenterX - roomWidth / 2 + camHalfWidth, currentRoomCenterX + roomWidth / 2 - camHalfWidth);
                targetPosition.y = fixedY; // Making sure Y always FIXED
            }
        }

        // Lerp to target (Y always fixed)
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    public void MoveToRoom(Vector3 newPosition)
    {
        targetPosition = newPosition;
        OnRoomChanged?.Invoke();
    }

    private float CalculateRoomCenterX(float positionX)
    {
        // Snap ke center room X only (grid mulai dari 0)
        return Mathf.Round(positionX / roomWidth) * roomWidth;
    }
}