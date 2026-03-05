using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class CameraCollider : MonoBehaviour
{
    [Header("Camera Target & Offset")]
    // Drag a Transform here to override where the camera moves to when inside this zone
    public Transform cameraTo;
    public Vector2 offset;
    public float zoom = 8;

    [Header("Transition")]
    public float lerpSpeed = 5f;

    [Header("Lock Axes")]
    public bool lockX = true;
    public bool lockY = false;

    [Header("Clamp To Collider")]
    // Keeps camera edges within the bounds of this collider
    public bool clampToCollider = false;

    private Camera mainCam;
    private CameraController camCtrl;
    private Collider2D col;

    // Shared across all instances — tracks which zone "owns" the camera
    private static CameraCollider activeZone = null;

    // stored defaults from CameraController, restored on exit
    private Transform defaultTarget;
    private Vector2 defaultOffset;
    private float defaultFollowSpeed;
    private bool defaultLockX;
    private bool defaultLockY;
    private float defaultZoom;

    void Awake()
    {
        mainCam = Camera.main;
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        camCtrl = mainCam.GetComponent<CameraController>();
        if (camCtrl == null)
            Debug.LogWarning("[CameraCollider] No CameraController found on Main Camera.");

        // Store current CameraController state
        defaultTarget = camCtrl.target;
        defaultOffset = camCtrl.offset;
        defaultFollowSpeed = camCtrl.followSpeed;
        defaultLockX = camCtrl.lockX;
        defaultLockY = camCtrl.lockY;
        defaultZoom = mainCam.orthographicSize;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || camCtrl == null) return;

        // This zone now owns the camera
        activeZone = this;

        float tweenDuration = 1f / Mathf.Max(lerpSpeed, 0.01f);

        if (clampToCollider)
        {
            camCtrl.clampEnabled = true;
            camCtrl.clampBounds = col.bounds;
        }

        mainCam.DOKill();
        mainCam.transform.DOKill();
        mainCam.DOOrthoSize(zoom, tweenDuration).SetEase(Ease.InOutSine);

        if (cameraTo != null)
        {
            Vector3 destination = new Vector3(
                cameraTo.position.x + offset.x,
                cameraTo.position.y + offset.y,
                mainCam.transform.position.z
            );

            camCtrl.target = null;
            mainCam.transform.DOMove(destination, tweenDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    // Only apply if we're still the active zone
                    if (activeZone != this) return;
                    camCtrl.lockX = lockX;
                    camCtrl.lockY = lockY;
                    camCtrl.target = defaultTarget;
                    camCtrl.offset = offset;
                    camCtrl.followSpeed = lerpSpeed;
                });
        }
        else
        {
            camCtrl.offset = offset;
            camCtrl.followSpeed = lerpSpeed;
            camCtrl.lockX = lockX;
            camCtrl.lockY = lockY;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || camCtrl == null) return;

        if (clampToCollider && !activeZone.clampToCollider)
        {
            camCtrl.clampEnabled = false;
        }

        // If we're not the active zone, the player already entered another
        if (activeZone != this) return;

        activeZone = null;

        // Kill any in-progress tween
        mainCam.transform.DOKill();

        // Restore CameraController to its original state
        mainCam.DOOrthoSize(defaultZoom, 1/camCtrl.followSpeed);
        camCtrl.target = defaultTarget;
        camCtrl.offset = defaultOffset;
        camCtrl.followSpeed = defaultFollowSpeed;
        camCtrl.lockX = defaultLockX;
        camCtrl.lockY = defaultLockY;
    }
}