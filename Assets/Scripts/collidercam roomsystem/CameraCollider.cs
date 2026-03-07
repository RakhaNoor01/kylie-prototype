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

    void Awake()
    {
        mainCam = Camera.main;
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        camCtrl = mainCam.GetComponent<CameraController>();

        if (camCtrl == null)
            Debug.LogWarning("[CameraCollider] No CameraController found on Main Camera.");

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

        // Register this zone as one the player is currently inside
        if (!overlappingZones.Contains(this))
            overlappingZones.Add(this);

        // This zone takes ownership of the camera
        activeZone = this;
        ApplyZone();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || camCtrl == null) return;

        // Unregister this zone
        overlappingZones.Remove(this);

        // If we weren't in control, nothing else to do
        if (activeZone != this) return;

        if (overlappingZones.Count > 0)
        {
            // Player is still inside another zone — hand control to the most recently entered one.
            // This handles the case where the player entered zoneB without fully leaving zoneA:
            // exiting zoneB should restore zoneA's settings, not the global defaults.
            CameraCollider fallback = overlappingZones[overlappingZones.Count - 1];
            activeZone = fallback;
            fallback.ApplyZone();
        }
        else
        {
            // Player has left all zones — restore the original camera defaults
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
            camCtrl.clampBounds = col.bounds;

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
                    camCtrl.followSpeed = lerpSpeed;
                });
        }
        else
        {
            // No fixed target — just update the controller's follow parameters immediately
            camCtrl.offset = offset;
            camCtrl.followSpeed = lerpSpeed;
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