using UnityEngine;
using TarodevController;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("=== CLIPS ===")]
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip glideClip;
    [SerializeField] private AudioClip climbClip;
    [SerializeField] private AudioClip throwClip;
    [SerializeField] private AudioClip teleportClip;
    [SerializeField] private AudioClip swingClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip wallJumpClip;
    [SerializeField] private AudioClip wallSlideClip;

    [Header("=== FOOTSTEP SETTINGS ===")]
    [SerializeField] private float minStepInterval = 0.15f;
    [SerializeField] private float maxStepInterval = 0.45f;
    [SerializeField] private float walkThreshold = 0.5f;
    [SerializeField] private float runThreshold = 6f;

    [Header("=== PITCH VARIATION ===")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("=== REFERENCES ===")]
    [Tooltip("Kosongin biar otomatis cari di parent/child. Drag manual kalo mau explicit.")]
    [SerializeField] private PlayerController _player;
    [Tooltip("Kosongin biar otomatis cari di parent/child. Drag manual kalo mau explicit.")]
    [SerializeField] private Rigidbody2D _rb;
    [Tooltip("Aktifin kalo GameObject ini dipisah dari Player DAN spatialBlend diubah jadi 3D")]
    [SerializeField] private bool followPlayerPosition = false;

    // ========================
    // PRIVATE
    // ========================
    private AudioSource _sfx;
    private AudioSource _loop;

    private float _nextStepTime;
    private bool _wasGrounded;
    private float _currentPitch;

    private bool _wasDashing;
    private bool _wasClingingOrSliding;
    private float _clingGraceUntil;
    private const float ClingGraceDuration = 0.15f;
    private bool _isClimbing;
    private bool _isWallSliding;

    // ========================
    // LIFECYCLE
    // ========================

    private void Awake()
    {
        _sfx = GetComponent<AudioSource>();
        _loop = gameObject.AddComponent<AudioSource>();

        // 🔥 OTOSARIS: Cari PlayerController di parent/child kalo belum di-assign
        if (_player == null)
        {
            _player = GetComponentInParent<PlayerController>();
            if (_player == null)
            {
                _player = GetComponentInChildren<PlayerController>();
            }

            if (_player == null)
            {
                Debug.LogError($"[PlayerAudio] PlayerController tidak ditemukan di parent/child {gameObject.name}. Drag manual ke slot 'Player'.", this);
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"[PlayerAudio] PlayerController otomatis ditemukan di {(transform.IsChildOf(_player.transform) ? "parent" : "child")}: {_player.gameObject.name}");
            }
        }

        // 🔥 OTOSARIS: Cari Rigidbody2D di parent/child kalo belum di-assign
        if (_rb == null)
        {
            _rb = GetComponentInParent<Rigidbody2D>();
            if (_rb == null)
            {
                _rb = GetComponentInChildren<Rigidbody2D>();
            }

            if (_rb == null)
            {
                Debug.LogError($"[PlayerAudio] Rigidbody2D tidak ditemukan di parent/child {gameObject.name}. Drag manual ke slot 'Rigidbody'.", this);
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"[PlayerAudio] Rigidbody2D otomatis ditemukan di {(transform.IsChildOf(_rb.transform) ? "parent" : "child")}: {_rb.gameObject.name}");
            }
        }

        SetupAudioSource(_sfx, false, false);
        SetupAudioSource(_loop, false, true);

        _player.GroundedChanged += OnGroundedChanged;
        _player.Jumped += OnJumped;
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.GroundedChanged -= OnGroundedChanged;
            _player.Jumped -= OnJumped;
        }
    }

    private void SetupAudioSource(AudioSource source, bool playOnAwake, bool loop)
    {
        source.playOnAwake = playOnAwake;
        source.loop = loop;
        source.spatialBlend = 0f;
    }

    private void Update()
    {
        HandleClingAndSlideState();
        HandleFootstep();
        HandleWallSlide();
        HandleDashDetection();
    }

    private void LateUpdate()
    {
        if (followPlayerPosition && _player != null)
        {
            transform.position = _player.transform.position;
        }
    }

    // ========================
    // CLING / SLIDE STATE TRACKING
    // ========================

    private bool IsEffectivelyClinging =>
        _player.IsClinging || _player.IsWallSliding || Time.time < _clingGraceUntil;

    private void HandleClingAndSlideState()
    {
        bool isClingingOrSliding = _player.IsClinging || _player.IsWallSliding;

        if (isClingingOrSliding)
        {
            _clingGraceUntil = Time.time + ClingGraceDuration;
        }

        if (isClingingOrSliding && !_wasClingingOrSliding)
        {
            _nextStepTime = Time.time + 0.2f;

            if (!_isClimbing)
            {
                _isClimbing = true;
                if (climbClip)
                {
                    _sfx.pitch = Random.Range(0.9f, 1.1f);
                    _sfx.PlayOneShot(climbClip);
                }
            }
        }
        else if (!isClingingOrSliding && _wasClingingOrSliding && Time.time >= _clingGraceUntil)
        {
            _isClimbing = false;
        }

        _wasClingingOrSliding = isClingingOrSliding;
    }

    // ========================
    // DASH DETECTION (Polling)
    // ========================

    private void HandleDashDetection()
    {
        bool isDashing = _player.IsDashing;
        _wasDashing = isDashing;
    }

    // ========================
    // FOOTSTEP
    // ========================

    private void HandleFootstep()
    {
        if (IsEffectivelyClinging) return;

        float speed = Mathf.Abs(_rb.linearVelocity.x);
        bool isMoving = _player.Grounded && speed > walkThreshold;

        if (!isMoving) return;

        float speedFactor = Mathf.InverseLerp(walkThreshold, 12f, speed);
        float interval = Mathf.Lerp(maxStepInterval, minStepInterval, speedFactor);
        interval *= Random.Range(0.9f, 1.1f);

        if (Time.time >= _nextStepTime)
        {
            _currentPitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);
            _sfx.pitch = _currentPitch * Random.Range(0.97f, 1.03f);
            _sfx.PlayOneShot(walkClip);
            _nextStepTime = Time.time + interval;
        }
    }

    // ========================
    // WALL SLIDE
    // ========================

    private void HandleWallSlide()
    {
        bool isWallSliding = _player.IsWallSliding;

        if (isWallSliding && !_isWallSliding)
        {
            if (wallSlideClip != null)
            {
                _loop.clip = wallSlideClip;
                _loop.pitch = 0.8f;
                _loop.Play();
            }
            _isWallSliding = true;
        }
        else if (!isWallSliding && _isWallSliding)
        {
            _loop.Stop();
            _isWallSliding = false;
        }

        if (_isWallSliding && _loop.isPlaying)
        {
            float verticalSpeed = Mathf.Abs(_rb.linearVelocity.y);
            _loop.pitch = Mathf.Lerp(0.8f, 1.2f, verticalSpeed / 10f);
        }
    }

    // ========================
    // EVENT HANDLERS
    // ========================

    private void OnJumped()
    {
        if (jumpClip)
        {
            _sfx.pitch = Random.Range(0.95f, 1.05f);
            _sfx.PlayOneShot(jumpClip);
        }
    }

    private void OnGroundedChanged(bool grounded, float fallSpeed)
    {
        if (grounded && !_wasGrounded && !IsEffectivelyClinging)
        {
            if (landClip)
            {
                float landPitch = Mathf.Lerp(0.8f, 1.2f, Mathf.Abs(fallSpeed) / 15f);
                _sfx.pitch = Mathf.Clamp(landPitch, 0.7f, 1.3f);
                _sfx.PlayOneShot(landClip);
            }
            _nextStepTime = Time.time + 0.05f;
        }
        _wasGrounded = grounded;
    }

    // ========================
    // PUBLIC ONE-SHOT
    // ========================

    public void PlayThrow() => PlayOneShot(throwClip);
    public void PlayTeleport() => PlayOneShot(teleportClip);
    public void PlaySwing() => PlayOneShot(swingClip);
    public void PlayDeath() => PlayOneShot(deathClip);
    public void PlayWallJump() => PlayOneShot(wallJumpClip);

    public void PlayDash()
    {
        if (dashClip)
        {
            _sfx.pitch = Random.Range(0.9f, 1.1f);
            _sfx.PlayOneShot(dashClip);
        }
    }

    private void PlayOneShot(AudioClip clip, float pitchMult = 1f)
    {
        if (clip == null) return;
        _sfx.pitch = Random.Range(minPitch, maxPitch) * pitchMult;
        _sfx.PlayOneShot(clip);
    }

    // ========================
    // GLIDE / LOOP CONTROL
    // ========================

    public void StartGlide()
    {
        if (_loop.isPlaying || glideClip == null) return;
        _loop.clip = glideClip;
        _loop.pitch = 1f;
        _loop.Play();
    }

    public void StopGlide() => _loop.Stop();

    // No-op: biar PlayerController tetep bisa manggil tanpa error
    public void StartClimb() { }
    public void StopClimb() { }
}