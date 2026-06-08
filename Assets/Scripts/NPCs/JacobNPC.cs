using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// =============================================================================
//  JACOB NPC  —  Unity 6, Rigidbody2D Dynamic
//
//  COLLIDER SETUP (wajib):
//  ┌─────────────────────────────────────────────────────────────────────┐
//  │  Root GameObject "Jacob"                                            │
//  │  ├─ Rigidbody2D   Body=Dynamic, FreezeRotZ=true, CollDet=Cont.    │
//  │  ├─ BoxCollider2D  IsTrigger=FALSE  ← physics / berdiri di tanah  │
//  │  └─ BoxCollider2D  IsTrigger=TRUE   ← interaction area dialogue   │
//  │                                                                     │
//  │  Child "GroundCheck"  ← Transform tepat di bawah kaki Jacob        │
//  └─────────────────────────────────────────────────────────────────────┘
//
//  LAYER SETUP (wajib agar trigger dan physics tidak konflik):
//  • Jacob     → Layer "NPC"
//  • Player    → Layer "Player"
//  • Tilemap   → Layer "Ground"
//  Physics 2D Matrix: NPC ↔ Ground = COLLIDE, NPC ↔ Player = NO COLLIDE
//    (Agar trigger dialogue tetap jalan, pastikan NPC & Player ada di
//     Collision Matrix yang TIDAK collide secara physics, tapi TETAP
//     bisa detect trigger — Unity pisahkan physics collision vs trigger.)
// =============================================================================

[RequireComponent(typeof(Rigidbody2D))]
public class JacobNPC : MonoBehaviour
{
    // =========================================================================
    //  STATE
    // =========================================================================
    private enum JacobState { Idle, InDialogue, TraversingPath, Passed }
    private JacobState _state = JacobState.Idle;

    // =========================================================================
    //  INSPECTOR — COMPONENTS
    // =========================================================================
    [Header("── Components ──────────────────────────────────────────")]
    [Tooltip("Auto-fetched jika kosong.")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    // =========================================================================
    //  INSPECTOR — DIALOGUE
    // =========================================================================
    [Header("── Dialogue ────────────────────────────────────────────")]
    public DialogueData dialogue;
    public Sprite npcPortrait;
    public Sprite dialogueBoxSprite;

    // =========================================================================
    //  INSPECTOR — INDICATOR
    // =========================================================================
    [Header("── Interaction Indicator ──────────────────────────────")]
    public SpriteRenderer interactionIndicator;
    public float bobHeight = 0.10f;
    public float bobSpeed = 3.00f;

    // =========================================================================
    //  INSPECTOR — FACING
    // =========================================================================
    [Header("── Facing ───────────────────────────────────────────────")]
    [Tooltip("Centang jika sprite DEFAULT Jacob menghadap KANAN.")]
    public bool defaultFacingRight = false;

    // =========================================================================
    //  INSPECTOR — PATH
    // =========================================================================
    [Header("── Path Waypoints ──────────────────────────────────────")]
    [Tooltip("Urutan waypoint. Terakhir HARUS action = IdleEnd.")]
    public JacobPathPoint[] pathPoints;

    [Tooltip("Kecepatan lari horizontal (m/s).")]
    public float runSpeed = 5f;

    [Tooltip("Jarak horizontal (m) untuk dianggap tiba di waypoint.")]
    public float arrivalThreshold = 0.2f;

    [Tooltip("Jarak (m) Jacob mulai ngerem sebelum waypoint. Naikkan jika masih overshoot.")]
    public float brakeDistance = 1.5f;

    [Tooltip("Timeout (detik) sebelum RunToPoint menyerah jika Jacob stuck.")]
    public float stuckTimeout = 3f;

    // =========================================================================
    //  INSPECTOR — GROUND CHECK
    // =========================================================================
    [Header("── Ground Check ────────────────────────────────────────")]
    [Tooltip("Child GO di posisi kaki Jacob. Buat child kosong bernama GroundCheck.")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.10f;
    public LayerMask groundLayer;

    // =========================================================================
    //  INSPECTOR — ANIMATOR PARAMS
    // =========================================================================
    [Header("── Animator Parameters ────────────────────────────────")]
    public string animParamRunning = "IsRunning";
    public string animParamJump = "Jump";
    [Tooltip("Bool parameter 'IsGrounded' di Animator. Kosongkan jika tidak dipakai.")]
    public string animParamGrounded = "IsGrounded";

    // =========================================================================
    //  INSPECTOR — EVENTS & PERSISTENCE
    // =========================================================================
    [Header("── Events ───────────────────────────────────────────────")]
    public UnityEvent onSequenceComplete;

    [Header("── Persistence ─────────────────────────────────────────")]
    [Tooltip("Set true dari SaveSystem agar Jacob langsung Passed saat scene load.")]
    public bool hasAlreadyPassed = false;

    // =========================================================================
    //  PRIVATE FIELDS
    // =========================================================================
    private Transform _player;
    private bool _playerInRange;
    private Vector3 _indicatorOrigin;

    // Target velocity yang diset oleh movement coroutine
    // dan dikonsumsi di FixedUpdate — ini kunci agar tidak konflik
    private float _targetVelX = 0f;
    private bool _applyHorizontal = false;   // true hanya saat TraversingPath

    // =========================================================================
    //  LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        if (interactionIndicator != null)
        {
            _indicatorOrigin = interactionIndicator.transform.localPosition;
            interactionIndicator.enabled = false;
        }

        if (hasAlreadyPassed)
        {
            SnapToLastWaypoint();
            EnterPassed(fireEvent: false);
        }
    }

    private void Update()
    {
        // Cache player
        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
            return;
        }

        if (_state == JacobState.Idle)
            UpdateIdle();
    }

    private void FixedUpdate()
    {
        if (_applyHorizontal)
        {
            rb.linearVelocity = new Vector2(_targetVelX, rb.linearVelocity.y);
        }
        else
        {
            // Zero horizontal velocity di hampir semua state kecuali saat lagi di udara jump
            bool shouldZeroX = _state != JacobState.TraversingPath ||
                              (rb.linearVelocity.y > 0.1f); // masih naik

            if (shouldZeroX)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        SetGroundedAnim(IsGroundedBox());
    }

    // =========================================================================
    //  STATE: IDLE
    // =========================================================================
    private void UpdateIdle()
    {
        FlipToFacePlayer();
        UpdateIndicatorBob();

        if (_playerInRange && Input.GetButtonDown("Submit"))
        {
            _state = JacobState.InDialogue;
            HideIndicator();
            BeginDialogue();
        }
    }

    // =========================================================================
    //  DIALOGUE
    // =========================================================================
    private void BeginDialogue()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("[JacobNPC] DialogueManager tidak ditemukan — skip ke sequence.");
            OnDialogueFinished();
            return;
        }

        DialogueManager.Instance.StartDialogue(
            dialogue,
            npcPortrait,
            dialogueBoxSprite,
            onComplete: OnDialogueFinished
        );
    }

    private void OnDialogueFinished()
    {
        if (pathPoints == null || pathPoints.Length == 0)
        {
            Debug.LogWarning("[JacobNPC] pathPoints kosong — langsung Passed.");
            EnterPassed();
            return;
        }

        _state = JacobState.TraversingPath;
        StartCoroutine(Co_TraversePath());
    }

    // =========================================================================
    //  PATH TRAVERSAL
    // =========================================================================
    private IEnumerator Co_TraversePath()
    {
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (_state != JacobState.TraversingPath) yield break;

            JacobPathPoint pt = pathPoints[i];
            if (pt == null)
            {
                Debug.LogWarning($"[JacobNPC] pathPoints[{i}] null — dilewati.");
                continue;
            }

            // Untuk Jump: kita perlu tahu waypoint BERIKUTNYA sebagai target mendarat
            JacobPathPoint nextPt = (i + 1 < pathPoints.Length) ? pathPoints[i + 1] : null;

            yield return StartCoroutine(Co_ExecuteWaypoint(pt, nextPt));

            // Kalau waypoint ini Jump, kita sudah menuju nextPt — skip nextPt
            if (pt.action == MovementAction.Jump && nextPt != null)
                i++;   // lompat index karena nextPt sudah dikunjungi saat landing
        }

        EnterPassed();
    }

    /// <param name="pt">Waypoint saat ini.</param>
    /// <param name="nextPt">Waypoint berikutnya — dipakai sebagai target lompatan jika action=Jump.</param>
    private IEnumerator Co_ExecuteWaypoint(JacobPathPoint pt, JacobPathPoint nextPt)
    {
        switch (pt.action)
        {
            // ── RUN: lari lurus ke posisi X waypoint ini ──────────────────────
            case MovementAction.Run:
                ApplyFacing(pt.faceRight);
                yield return StartCoroutine(Co_RunToPoint(pt.transform.position.x));
                SetRunningAnim(false);   // stop idle di sini, bukan di dalam Co_RunToPoint
                break;

            // ── JUMP: tiba di waypoint ini → lompat menuju waypoint BERIKUTNYA ─
            //   Jacob dulu lari ke pt (titik tolak), lalu melompat ke nextPt.
            //   Jika nextPt null (jump adalah waypoint terakhir), lompat di tempat.
            case MovementAction.Jump:
                // 1. Lari ke waypoint tolak (pt)
                ApplyFacing(pt.faceRight);
                yield return StartCoroutine(Co_RunToPoint(pt.transform.position.x));

                // 2. Tentukan target pendaratan
                float landX = nextPt != null ? nextPt.transform.position.x : pt.transform.position.x;
                ApplyFacing(landX >= transform.position.x);

                // 3. Lompat parabola menuju target (arcHeight dari waypoint ini)
                yield return StartCoroutine(Co_Jump(pt.arcHeight, landX));
                break;

            // ── WAIT: diam selama waitTime ────────────────────────────────────
            case MovementAction.Wait:
                _applyHorizontal = false;
                _targetVelX = 0f;
                SetRunningAnim(false);
                ApplyFacing(pt.faceRight);
                yield return new WaitForSeconds(pt.waitTime);
                break;

            // ── IDLE END: berhenti total ──────────────────────────────────────
            case MovementAction.IdleEnd:
                EnterPassed();
                yield break;
        }
    }

    // =========================================================================
    //  MOVEMENT COROUTINES
    // =========================================================================

    /// <summary>
    /// Berlari ke targetX menggunakan WaitForFixedUpdate agar sync dengan physics.
    /// Velocity di-brake lebih awal (brakeDistance) untuk menghindari overshoot.
    /// </summary>
    private IEnumerator Co_RunToPoint(float targetX)
    {
        SetRunningAnim(true);

        float stuckTimer = 0f;
        float lastX = transform.position.x;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            float deltaX = targetX - transform.position.x;
            float absDist = Mathf.Abs(deltaX);

            if (absDist <= arrivalThreshold)
                break;

            // Stuck detection lebih longgar
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= 0.8f)  // naikkan sedikit
            {
                if (Mathf.Abs(transform.position.x - lastX) < 0.03f)
                {
                    Debug.LogWarning($"[JacobNPC] Stuck at RunToPoint. Distance left: {absDist}");
                    break;
                }
                lastX = transform.position.x;
                stuckTimer = 0f;
            }

            float speedFactor = absDist <= brakeDistance
                                ? Mathf.Clamp01(absDist / brakeDistance)
                                : 1f;

            float clampedSpeed = Mathf.Max(speedFactor * runSpeed, 0.8f); // naikkan min speed

            _targetVelX = Mathf.Sign(deltaX) * clampedSpeed;
            _applyHorizontal = true;
        }

        // Final stop yang lebih kuat
        _applyHorizontal = false;
        _targetVelX = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Snap
        Vector3 pos = transform.position;
        pos.x = targetX;
        rb.MovePosition(pos);

        yield return new WaitForFixedUpdate();
    }

    /// <summary>
    /// Lompat parabola menuju <paramref name="landX"/>.
    ///
    /// Fisika (identik DogController / JumpWaypoint):
    ///   g   = |Physics2D.gravity.y| × rb.gravityScale
    ///   vy  = sqrt(2 · g · arcHeight)    — kecepatan vertikal awal
    ///   T   = 2 · vy / g                 — total waktu terbang
    ///   vx  = Δx / T                     — horizontal konstan → mendarat TEPAT di landX
    ///
    /// Tidak ada jeda idle antara lari dan lompat:
    ///   Co_RunToPoint tidak lagi memanggil SetRunningAnim(false),
    ///   sehingga transisi Run→Jump langsung tanpa frame idle di antaranya.
    /// </summary>
    private IEnumerator Co_Jump(float arcHeight, float landX)
    {
        // ── 1. Stop running anim, nol-kan vx — langsung lompat di frame ini ──
        SetRunningAnim(false);
        _applyHorizontal = false;
        _targetVelX = 0f;
        // Nol-kan vx sekarang; vy biarkan (biasanya 0 karena baru landing dari run)
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Satu tick agar MovePosition Co_RunToPoint commit — tidak lebih
        yield return new WaitForFixedUpdate();

        // ── 2. Hitung vx & vy dari parabola ──────────────────────────────────
        float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        float h = Mathf.Max(arcHeight, 0.05f);

        float vy = Mathf.Sqrt(2f * g * h);
        float totalTime = 2f * vy / g;
        float dx = landX - transform.position.x;
        float vx = (totalTime > 0f) ? dx / totalTime : 0f;

        ApplyFacing(dx >= 0f);

        // ── 3. Trigger anim + terapkan velocity awal ─────────────────────────
        if (animator != null && !string.IsNullOrEmpty(animParamJump))
            animator.SetTrigger(animParamJump);
        SetGroundedAnim(false);

        rb.linearVelocity = new Vector2(vx, vy);
        _targetVelX = vx;
        _applyHorizontal = true;

        // ── 4. Tunggu puncak ─────────────────────────────────────────────────
        float riseTimer = 0f;
        while (rb.linearVelocity.y > 0f && riseTimer < totalTime + 0.5f)
        {
            riseTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // ── 5. Tunggu mendarat ────────────────────────────────────────────────
        float fallTimer = 0f;
        while (fallTimer < totalTime + 2f)
        {
            fallTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
            if (rb.linearVelocity.y <= 0f && IsGroundedBox()) break;
        }

        // ── 6. Landing ─────────────────────────────────────────────────────
        _applyHorizontal = false;
        _targetVelX = 0f;

        // Matikan velocity dulu
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForFixedUpdate();

        // Snap posisi dengan sedikit tolerance
        Vector3 pos = transform.position;
        pos.x = landX;

        // Pakai MovePosition + sedikit koreksi
        rb.MovePosition(pos);

        SetGroundedAnim(true);

        // Tambahan: pastikan benar-benar diam
        yield return new WaitForFixedUpdate();
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Ground check menggunakan BoxCast — lebih toleran di tepi platform
    /// dibanding OverlapCircle yang bisa miss saat Jacob di ujung tile.
    /// </summary>
    private bool IsGroundedBox()
    {
        if (groundCheck == null) return true;

        // Ukuran box cast: lebar sedikit lebih kecil dari collider, tinggi tipis
        Vector2 boxSize = new Vector2(0.3f, 0.05f);
        float castDist = groundCheckRadius + 0.05f;

        RaycastHit2D hit = Physics2D.BoxCast(
            groundCheck.position,
            boxSize,
            0f,
            Vector2.down,
            castDist,
            groundLayer
        );

        return hit.collider != null;
    }

    // =========================================================================
    //  GROUND CHECK
    // =========================================================================
    private bool IsGrounded() => IsGroundedBox();

    // =========================================================================
    //  STATE: PASSED
    // =========================================================================
    private void EnterPassed(bool fireEvent = true)
    {
        _state = JacobState.Passed;
        hasAlreadyPassed = true;
        _applyHorizontal = false;
        _targetVelX = 0f;

        rb.linearVelocity = Vector2.zero;
        SetRunningAnim(false);
        HideIndicator();

        if (fireEvent) onSequenceComplete?.Invoke();
    }

    private void SnapToLastWaypoint()
    {
        if (pathPoints == null || pathPoints.Length == 0) return;
        for (int i = pathPoints.Length - 1; i >= 0; i--)
        {
            if (pathPoints[i] == null) continue;
            Vector3 p = pathPoints[i].transform.position;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            return;
        }
    }

    // =========================================================================
    //  TRIGGER CALLBACKS  (dari BoxCollider2D IsTrigger=true di root Jacob)
    // =========================================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (_state == JacobState.Idle) ShowIndicator();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        HideIndicator();
    }

    // =========================================================================
    //  FACING
    // =========================================================================
    private void FlipToFacePlayer()
    {
        if (_player == null || spriteRenderer == null) return;
        ApplyFacing(_player.position.x > transform.position.x);
    }

    private void ApplyFacing(bool wantRight)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = defaultFacingRight ? !wantRight : wantRight;
    }

    // =========================================================================
    //  ANIMATOR
    // =========================================================================
    private void SetRunningAnim(bool running)
    {
        if (animator == null || string.IsNullOrEmpty(animParamRunning)) return;
        animator.SetBool(animParamRunning, running);
    }

    private void SetGroundedAnim(bool grounded)
    {
        if (animator == null || string.IsNullOrEmpty(animParamGrounded)) return;
        animator.SetBool(animParamGrounded, grounded);
    }

    // =========================================================================
    //  INDICATOR
    // =========================================================================
    private void ShowIndicator()
    {
        if (interactionIndicator != null) interactionIndicator.enabled = true;
    }

    private void HideIndicator()
    {
        if (interactionIndicator != null) interactionIndicator.enabled = false;
    }

    private void UpdateIndicatorBob()
    {
        if (interactionIndicator == null || !interactionIndicator.enabled) return;
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        interactionIndicator.transform.localPosition = _indicatorOrigin + new Vector3(0f, y, 0f);
    }

    // =========================================================================
    //  GIZMOS
    // =========================================================================
    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;
        Gizmos.color = Application.isPlaying && IsGrounded() ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}