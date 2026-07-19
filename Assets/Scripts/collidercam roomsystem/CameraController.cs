using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    [Range(0f, 20f)]
    public float followSpeed = 5f;
    public Vector2 offset = new Vector2(0f, 0f);

    [Header("Lock Axes")]
    public bool lockX = false;
    public bool lockY = false;

    [Header("Clamp")]
    // Set by CameraCollider — clamps camera edges to these world-space bounds
    public bool clampEnabled = false;
    public Bounds clampBounds;

    public Vector3 ShakeOffset { get; set; }

    void LateUpdate()
    {
        if (target == null) return;

        float desiredX = lockX ? transform.position.x : target.position.x + offset.x;
        float desiredY = lockY ? transform.position.y : target.position.y + offset.y;

        Vector3 desiredPosition = new Vector3(desiredX, desiredY, transform.position.z);

        if (clampEnabled)
        {
            float camHalfH = Camera.main.orthographicSize;
            float camHalfW = camHalfH * Camera.main.aspect;
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, clampBounds.min.x + camHalfW, clampBounds.max.x - camHalfW);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, clampBounds.min.y + camHalfH, clampBounds.max.y - camHalfH);
        }

        Vector3 followPos = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.position = followPos + ShakeOffset;
    }
}