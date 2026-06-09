using UnityEngine;
using DG.Tweening;
using TarodevController;

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
    public bool resetOnExit = true;
    public int priority = 0;
    [Header("Lock Axes")]
    public bool lockX = true;
    public bool lockY = false;
    // Keeps camera edges within the bounds of this collider
    public bool clampToCollider = false;

    private Camera mainCam;
    private CameraController camCtrl;
    private Collider2D col;
    private Bounds cachedBounds;

    // Tracks which zone currently owns the camera — shared across all instances
    private static CameraCollider activeZone = null;

    // All zones the player is currently overlapping, in entry order.
    // Used to fall back to a previous zone when exiting the active one.
    private static readonly System.Collections.Generic.List<CameraCollider> overlappingZones
        = new System.Collections.Generic.List<CameraCollider>();

    // Snapshot of CameraController's state before any zone was entered, restored on full exit
    private Transform defaultTarget;
    private Vector2 defaultOffset;
    private float defaultFollowSpeed;
    private bool defaultLockX;
    private bool defaultLockY;
    private float defaultZoom;

    private void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[CameraCollider] No Main Camera found in the scene.");
            return;
        }

        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        cachedBounds = col.bounds;
        camCtrl = mainCam.GetComponent<CameraController>();

        if (camCtrl == null)
        {
            Debug.LogError("[CameraCollider] No CameraController found on Main Camera.");
            return;
        }

        // Snapshot the camera's default state so we can restore it when the player leaves all zones
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

        cachedBounds = col.bounds;

        if (!overlappingZones.Contains(this))
            overlappingZones.Add(this);

        // Only take control if there is no active zone
        // or this zone has a higher priority than the active one
        if (activeZone == null || priority > activeZone.priority)
        {
            activeZone = this;
            ApplyZone();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || camCtrl == null) return;

        overlappingZones.Remove(this);

        if (activeZone != this) return;

        CameraCollider highestPriority = null;

        foreach (var zone in overlappingZones)
        {
            if (highestPriority == null || zone.priority > highestPriority.priority)
                highestPriority = zone;
        }

        if (highestPriority != null)
        {
            activeZone = highestPriority;
            highestPriority.ApplyZone();
        }
        else
        {
            if (!resetOnExit) return;

            activeZone = null;
            RestoreDefaults();
        }
    }

    // Applies this zone's camera settings: zoom, clamping, offset, lock axes, and optional fixed position
    void ApplyZone()
    {
        float tweenDuration = 1f / Mathf.Max(lerpSpeed, 0.01f);

        // Configure clamping before any tween starts
        camCtrl.clampEnabled = clampToCollider;
        if (clampToCollider)
            camCtrl.clampBounds = cachedBounds;

        // Kill any in-progress tweens to avoid conflicts before starting new ones
        mainCam.DOKill();
        mainCam.transform.DOKill();
        mainCam.DOOrthoSize(zoom, tweenDuration).SetEase(Ease.InOutSine);

        if (cameraTo != null)
        {
            // Move the camera to a fixed point, then re-enable following once it arrives
            Vector3 destination = new Vector3(
                cameraTo.position.x + offset.x,
                cameraTo.position.y + offset.y,
                mainCam.transform.position.z
            );

            // Detach from the follow target during the tween so CameraController doesn't fight DOTween
            camCtrl.target = null;
            mainCam.transform.DOMove(destination, tweenDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    // Bail if another zone took over while this tween was running
                    if (activeZone != this) return;

                    camCtrl.lockX = lockX;
                    camCtrl.lockY = lockY;
                    camCtrl.target = defaultTarget;
                    camCtrl.offset = offset;
                    camCtrl.followSpeed = defaultFollowSpeed;
                });
        }
        else
        {
            // No fixed target — just update the controller's follow parameters immediately
            camCtrl.offset = offset;
            camCtrl.lockX = lockX;
            camCtrl.lockY = lockY;
        }
    }

    // Restores the camera to the state it was in before any zone was entered
    void RestoreDefaults()
    {
        camCtrl.clampEnabled = false;
        mainCam.transform.DOKill();
        mainCam.DOOrthoSize(defaultZoom, 1f / Mathf.Max(camCtrl.followSpeed, 0.01f));
        camCtrl.target = defaultTarget;
        camCtrl.offset = defaultOffset;
        camCtrl.followSpeed = defaultFollowSpeed;
        camCtrl.lockX = defaultLockX;
        camCtrl.lockY = defaultLockY;
    }
}