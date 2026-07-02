using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Jacob NPC — idle → dialogue → waypoints → diam selamanya.
/// FINAL FIX: Pendaratan physics yang presisi & pergerakan natural.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JacobNPC : MonoBehaviour
{
    private enum State  { Idle, InDialogue, Moving, Passed }
    private enum Moving { Run, JumpPending, JumpArc, Done }

    // =========================================================================
    //  INSPECTOR
    // =========================================================================

    [Header("── Components ──────────────────────────────────────────")]
    public Rigidbody2D    rb;
    public Animator       animator;
    public SpriteRenderer sr;

    [Header("── Dialogue ────────────────────────────────────────────")]
    public DialogueData dialogue;
    public Sprite       npcPortrait;
    public Sprite       dialogueBoxSprite;

    [Header("── Interaction Indicator ──────────────────────────────")]
    public SpriteRenderer interactionIndicator;
    public float          bobHeight = 0.10f;
    public float          bobSpeed  = 3.00f;

    [Header("── Facing ───────────────────────────────────────────────")]
    public bool defaultFacingRight = false;

    [Header("── Path ─────────────────────────────────────────────────")]
    public JacobPathPoint[] pathPoints;
    public float moveSpeed             = 4f;
    public float waypointReachDistance = 0.3f;

    [Header("── Ground Check ────────────────────────────────────────")]
    public Transform groundCheck;
    public float     groundCheckRadius = 0.12f;
    public LayerMask groundLayer;

    [Header("── Animator ────────────────────────────────────────────")]
    public string animRun  = "IsRunning";
    public string animJump = "Jump";

    [Header("── Events ───────────────────────────────────────────────")]
    public UnityEvent onSequenceComplete;

    [Header("── Persistence ─────────────────────────────────────────")]
    public bool hasAlreadyPassed = false;

    // =========================================================================
    //  PRIVATE
    // =========================================================================

    private State  _state     = State.Idle;
    private Moving _moving    = Moving.Done;

    private Transform _player;
    private bool      _playerInRange;
    private Vector3   _indicatorOrigin;

    // Run
    private float _runTargetX;
    private float _runDir;

    // Jump
    private Vector2 _jumpEnd;
    private float   _jumpArc;
    private bool    _jumpFired;      
    private bool    _jumpPeakPassed; 

    // Signal dari FixedUpdate ke coroutine
    private bool _sigRunDone;
    private bool _sigJumpFired;
    private bool _sigLanded;

    // =========================================================================
    //  LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (rb       == null) rb       = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (sr       == null) sr       = GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
        rb.gravityScale   = 4f;
        rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
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
            EnterPassed(false);
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
        }
        if (_state == State.Idle) UpdateIdle();
    }

    // =========================================================================
    //  FIXED UPDATE 
    // =========================================================================

    private void FixedUpdate()
    {
        bool grounded = Physics2D.OverlapBox(
            groundCheck != null ? groundCheck.position : transform.position,
            new Vector2(0.4f, 0.1f),
            0f, groundLayer);

        switch (_state)
        {
            case State.Idle:
            case State.InDialogue:
            case State.Passed:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
        }

        switch (_moving)
        {
            case Moving.Run:
            {
                float remaining = (_runTargetX - rb.position.x) * _runDir;
                if (remaining <= waypointReachDistance)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    _sigRunDone = true;
                }
                else
                {
                    rb.linearVelocity = new Vector2(_runDir * moveSpeed, rb.linearVelocity.y);
                }
                break;
            }

            case Moving.JumpPending:
            {
                if (!_jumpFired)
                {
                    Vector2 start = rb.position;
                    float grav    = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
                    float h       = Mathf.Max(_jumpArc, _jumpEnd.y - start.y + 0.5f);
                    float vy      = Mathf.Sqrt(2f * grav * h);
                    float tUp     = vy / grav;
                    float tDown   = Mathf.Sqrt(2f * Mathf.Max(h - (_jumpEnd.y - start.y), 0.001f) / grav);
                    float vx      = (_jumpEnd.x - start.x) / (tUp + tDown);

                    rb.linearVelocity = new Vector2(vx, vy);
                    Flip(vx > 0);

                    _jumpFired      = true;
                    _jumpPeakPassed = false;
                    _moving         = Moving.JumpArc;
                    _sigJumpFired   = true;

                    Debug.Log($"[Jacob] Jump fired! start={start} end={_jumpEnd} vx={vx:F2} vy={vy:F2}");
                }
                break;
            }

            case Moving.JumpArc:
            {
                // FIX: Memastikan deteksi puncak tetap bekerja meskipun Unity 
                // me-reset velocity.y menjadi 0 ketika menyentuh tanah keras
                if (rb.linearVelocity.y <= 0.1f) _jumpPeakPassed = true;

                // Mendarat: sudah lewat puncak & collider nyentuh tanah
                if (_jumpPeakPassed && grounded)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    _sigLanded        = true;
                    Debug.Log($"[Jacob] Landed safely at {rb.position}");
                }
                break;
            }
        }
    }

    // =========================================================================
    //  IDLE
    // =========================================================================

    private void UpdateIdle()
    {
        FacePlayer();
        BobIndicator();

        if (_playerInRange && Input.GetButtonDown("Submit"))
        {
            _state = State.InDialogue;
            HideIndicator();
            BeginDialogue();
        }
    }

    // =========================================================================
    //  DIALOGUE
    // =========================================================================

    private void BeginDialogue()
    {
        if (DialogueManager.Instance == null) { OnDialogueFinished(); return; }
        DialogueManager.Instance.StartDialogue(
            dialogue, npcPortrait, dialogueBoxSprite,
            onComplete: OnDialogueFinished);
    }

    private void OnDialogueFinished()
    {
        if (pathPoints == null || pathPoints.Length == 0) { EnterPassed(); return; }
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _state = State.Moving;
        StartCoroutine(Co_RunPath());
    }

    // =========================================================================
    //  CO_RUNPATH
    // =========================================================================

    private IEnumerator Co_RunPath()
    {
        for (int i = 0; i < pathPoints.Length; i++)
        {
            JacobPathPoint pt = pathPoints[i];
            if (pt == null) continue;

            switch (pt.action)
            {
                case JacobAction.Run:
                    yield return StartCoroutine(Co_MoveTo(pt.transform.position.x));
                    break;

                case JacobAction.Jump:
                    yield return StartCoroutine(Co_MoveTo(pt.transform.position.x));

                    if (pt.landingPoint == null)
                    {
                        Debug.LogWarning($"[Jacob] '{pt.name}' tidak punya landingPoint!");
                        break;
                    }

                    yield return StartCoroutine(Co_Jump(pt.landingPoint.position, pt.arcHeight));
                    break;

                case JacobAction.Wait:
                    SetAnim(animRun, false);
                    yield return new WaitForSeconds(pt.waitTime);
                    break;

                case JacobAction.IdleEnd:
                    EnterPassed();
                    yield break;
            }
        }

        EnterPassed();
    }

    // =========================================================================
    //  CO_MOVETO
    // =========================================================================

    private IEnumerator Co_MoveTo(float targetX)
    {
        float dir = Mathf.Sign(targetX - rb.position.x);

        // Kalau waypoint tumpang tindih (seperti Landing1 & Jump2), 
        // proses lari akan langsung di-skip dan langsung mengeksekusi lompatan
        if (Mathf.Abs(targetX - rb.position.x) <= waypointReachDistance)
            yield break;

        Flip(dir > 0);
        SetAnim(animRun, true);

        _runTargetX = targetX;
        _runDir     = dir;
        _sigRunDone = false;
        _moving     = Moving.Run;

        // Berjalan natural sampai sinyal selesai dikirimkan dari FixedUpdate
        while (!_sigRunDone)
            yield return new WaitForFixedUpdate();

        _moving = Moving.Done;
        SetAnim(animRun, false);
    }

    // =========================================================================
    //  CO_JUMP
    // =========================================================================

    private IEnumerator Co_Jump(Vector2 end, float arcHeight)
    {
        _jumpEnd      = end;
        _jumpArc      = arcHeight;
        _jumpFired    = false;
        _sigJumpFired = false;
        _sigLanded    = false;
        _moving       = Moving.JumpPending;

        while (!_sigJumpFired)
            yield return new WaitForFixedUpdate();

        TriggerAnim(animJump);
        SetAnim(animRun, false);

        while (!_sigLanded)
            yield return new WaitForFixedUpdate();

        _moving = Moving.Done;
    }

    // =========================================================================
    //  HELPERS
    // =========================================================================

    private void EnterPassed(bool fireEvent = true)
    {
        _state            = State.Passed;
        _moving           = Moving.Done;
        hasAlreadyPassed  = true;
        rb.linearVelocity = Vector2.zero;
        rb.constraints    = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        SetAnim(animRun, false);
        HideIndicator();
        if (fireEvent) onSequenceComplete?.Invoke();
    }

    private void SnapToLastWaypoint()
    {
        if (pathPoints == null) return;
        for (int i = pathPoints.Length - 1; i >= 0; i--)
        {
            if (pathPoints[i] == null) continue;
            var p = pathPoints[i].transform.position;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            return;
        }
    }

    private void FacePlayer()
    {
        if (_player == null || sr == null) return;
        Flip(_player.position.x > transform.position.x);
    }

    private void Flip(bool wantRight)
    {
        if (sr == null) return;
        sr.flipX = defaultFacingRight ? !wantRight : wantRight;
    }

    private void SetAnim(string param, bool val)
    {
        if (animator == null || string.IsNullOrEmpty(param)) return;
        animator.SetBool(param, val);
    }

    private void TriggerAnim(string param)
    {
        if (animator == null || string.IsNullOrEmpty(param)) return;
        animator.SetTrigger(param);
    }

    private void ShowIndicator()
    {
        if (interactionIndicator != null) interactionIndicator.enabled = true;
    }

    private void HideIndicator()
    {
        if (interactionIndicator != null) interactionIndicator.enabled = false;
    }

    private void BobIndicator()
    {
        if (interactionIndicator == null || !interactionIndicator.enabled) return;
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        interactionIndicator.transform.localPosition = _indicatorOrigin + new Vector3(0, y, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (_state == State.Idle) ShowIndicator();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        HideIndicator();
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundCheck.position, new Vector3(0.4f, 0.1f, 0f));
    }
}