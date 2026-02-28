using UnityEngine;

public class DogController : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;
    public float waypointReachDistance = 0.5f;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.08f;
    public LayerMask groundLayer;

    [Header("Player Distance")]
    public Transform player;
    public float resumeDistance = 10f;
    public float pauseDistance = 15f;

    [Header("Camera")]
    public CameraManager cameraManager;

    [Header("Idle After Teleport")]
    public float idleAfterTeleport = 2f;  // ← durasi standby di belakang wall (detik). Ubah di Inspector

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private int wpIndex = 0;
    private bool isGrounded;
    private bool isPaused;
    private bool reachedEnd;
    private bool isJumping;
    private bool headingToLanding;
    private Transform landingTarget;

    private bool checkPlayerAtWaypoint = false;
    private bool isWaitingForCameraMove = false;

    // Teleport Logic
    private bool isTeleportPending = false;
    private Transform teleportDestination;
    private int postTeleportWpIndex;

    // Idle setelah teleport
    private bool isIdlingAfterTeleport = false;
    private float idleTimer = 0f;

    private float lastDistToWaypoint = Mathf.Infinity;

    public bool showDebugLogs = true;

    static readonly int AnimWalk = Animator.StringToHash("isWalking");
    static readonly int AnimJump = Animator.StringToHash("Jump");

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (cameraManager == null) cameraManager = Camera.main?.GetComponent<CameraManager>();
    }

    void OnEnable()
    {
        if (cameraManager != null)
            cameraManager.OnRoomChanged += HandleCameraRoomChanged;
    }

    void OnDisable()
    {
        if (cameraManager != null)
            cameraManager.OnRoomChanged -= HandleCameraRoomChanged;
    }

    private void HandleCameraRoomChanged()
    {
        if (!isWaitingForCameraMove) return;

        if (showDebugLogs) Debug.Log($"[Dog] === KAMERA PINDAH ROOM! langsung proses ===");

        isWaitingForCameraMove = false;

        if (isTeleportPending && teleportDestination != null)
        {
            PerformTeleport(teleportDestination);
            wpIndex = postTeleportWpIndex;
            isTeleportPending = false;
            if (showDebugLogs) Debug.Log($"[Dog] TELEPORT → wpIndex sekarang {wpIndex}");
        }
        else
        {
            wpIndex++;
            if (showDebugLogs) Debug.Log($"[Dog] Lanjut normal waypoint {wpIndex}");
        }

        checkPlayerAtWaypoint = true;
        lastDistToWaypoint = Mathf.Infinity;

        if (wpIndex >= waypoints.Length) reachedEnd = true;
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (reachedEnd) { StopMoving(); return; }
        if (isPaused) { HandlePause(); return; }

        // Prioritas: Idle setelah teleport (diam di belakang wall)
        if (isIdlingAfterTeleport)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetBool(AnimWalk, false);

            if (player != null)
                sr.flipX = player.position.x < transform.position.x;

            idleTimer -= Time.fixedDeltaTime;
            if (idleTimer <= 0f)
            {
                isIdlingAfterTeleport = false;
                animator.SetBool(AnimWalk, true);
                if (showDebugLogs) Debug.Log("[Dog] Idle setelah teleport selesai → mulai jalan");
            }
            return;
        }

        if (isWaitingForCameraMove) { WaitForCamera(); return; }

        if (wpIndex >= waypoints.Length) { reachedEnd = true; return; }

        if (headingToLanding)
        {
            float dx = Mathf.Abs(transform.position.x - landingTarget.position.x);
            if (dx <= waypointReachDistance && isGrounded)
            {
                headingToLanding = false;
                isJumping = false;
                landingTarget = null;

                wpIndex++;
                checkPlayerAtWaypoint = true;
                lastDistToWaypoint = Mathf.Infinity;
                if (showDebugLogs) Debug.Log($"[Dog] Landing selesai → langsung wp {wpIndex}");
            }
            return;
        }

        Transform target = waypoints[wpIndex];
        if (target == null) { wpIndex++; return; }

        float distX = Mathf.Abs(transform.position.x - target.position.x);
        bool hasPassed = (lastDistToWaypoint < Mathf.Infinity) && (distX > lastDistToWaypoint + 0.01f);
        lastDistToWaypoint = distX;

        if (distX <= waypointReachDistance || hasPassed)
        {
            lastDistToWaypoint = Mathf.Infinity;

            JumpWaypoint jw = target.GetComponent<JumpWaypoint>();
            if (jw != null && jw.landingPoint != null)
            {
                TryParabolaJump(transform.position, jw.landingPoint.position, jw.arcHeight);
                landingTarget = jw.landingPoint;
                return;
            }

            TeleportWaypoint tw = target.GetComponent<TeleportWaypoint>();
            if (tw != null && tw.teleportDestination != null)
            {
                isWaitingForCameraMove = true;
                isTeleportPending = true;
                teleportDestination = tw.teleportDestination;
                postTeleportWpIndex = (tw.nextWaypointIndex >= 0) ? tw.nextWaypointIndex : wpIndex + 1;

                checkPlayerAtWaypoint = true;
                if (showDebugLogs) Debug.Log($"[Dog] Sampai TELEPORT waypoint → nunggu kamera lalu teleport");
                return;
            }

            isWaitingForCameraMove = true;
            checkPlayerAtWaypoint = true;
            if (showDebugLogs) Debug.Log($"[Dog] Sampai normal waypoint {wpIndex} → nunggu kamera");
            return;
        }

        MoveToward(target);
    }

    public void PerformTeleport(Transform destination)
    {
        transform.position = destination.position;
        rb.linearVelocity = Vector2.zero;
        isJumping = false;
        headingToLanding = false;

        sr.flipX = false;

        isIdlingAfterTeleport = true;
        idleTimer = idleAfterTeleport;  // durasi standby

        animator.SetBool(AnimWalk, false);  // idle dulu

        if (showDebugLogs) Debug.Log($"[Dog] TELEPORT berhasil → standby/idle {idleAfterTeleport} detik di belakang wall");
    }

    void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool(AnimWalk, false);
    }

    void HandlePause()
    {
        float dist = player != null ? Vector2.Distance(transform.position, player.position) : 0f;
        if (dist <= resumeDistance) isPaused = false;
        else StopMoving();
    }

    void WaitForCamera()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool(AnimWalk, false);
        if (player != null)
            sr.flipX = player.position.x < transform.position.x;
    }

    void MoveToward(Transform target)
    {
        float dir = target.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        sr.flipX = dir < 0;
        animator.SetBool(AnimWalk, true);
    }

    void TryParabolaJump(Vector2 start, Vector2 end, float arcHeight)
    {
        if (isJumping) return;

        float gravity = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float h = Mathf.Max(arcHeight, end.y - start.y + 0.5f);

        float vy = Mathf.Sqrt(2f * gravity * h);
        float timeUp = vy / gravity;
        float timeDown = Mathf.Sqrt(2f * (h - (end.y - start.y)) / gravity);
        float totalTime = timeUp + timeDown;
        float vx = (end.x - start.x) / totalTime;

        rb.linearVelocity = new Vector2(vx, vy);

        isJumping = true;
        headingToLanding = true;
        animator.SetTrigger(AnimJump);
        sr.flipX = vx < 0;
    }

    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            bool isJump = waypoints[i].GetComponent<JumpWaypoint>() != null;
            bool isTeleport = waypoints[i].GetComponent<TeleportWaypoint>() != null;
            Gizmos.color = isTeleport ? Color.magenta : (isJump ? Color.cyan : Color.yellow);
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}