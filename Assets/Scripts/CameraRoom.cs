using UnityEngine;

public class CameraRoom : MonoBehaviour
{
    public Vector3 cameraPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Camera.main.GetComponent<CameraManager>().MoveToRoom(cameraPosition);
        }
    }
}
