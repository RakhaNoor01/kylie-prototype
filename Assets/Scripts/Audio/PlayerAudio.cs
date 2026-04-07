using UnityEngine;
using TarodevController;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Clips")]
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

    [Header("Footstep")]
    [SerializeField] private float minStepInterval = 0.25f;
    [SerializeField] private float maxStepInterval = 0.5f;

    private AudioSource _sfx;   // jump, walk, dll
    private AudioSource _loop;  // glide loop

    private PlayerController _player;
    private Rigidbody2D _rb;

    private float _stepTimer;
    private bool _wasGrounded;

    private void Awake()
    {
        _sfx = GetComponent<AudioSource>();

        // bikin audio source kedua buat loop
        _loop = gameObject.AddComponent<AudioSource>();

        _player = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody2D>();

        // setup SFX
        _sfx.playOnAwake = false;
        _sfx.loop = false;
        _sfx.spatialBlend = 0f;

        // setup LOOP
        _loop.playOnAwake = false;
        _loop.loop = true;
        _loop.spatialBlend = 0f;

        // subscribe event
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

    private void Update()
    {
        HandleFootstep();
    }

    // ========================
    // EVENT
    // ========================

    private void OnJumped()
    {
        if (jumpClip)
            _sfx.PlayOneShot(jumpClip);
    }

    private void OnGroundedChanged(bool grounded, float fallSpeed)
    {
        if (grounded && !_wasGrounded)
        {
            if (landClip)
                _sfx.PlayOneShot(landClip);
        }

        _wasGrounded = grounded;
    }

    // ========================
    // FOOTSTEP
    // ========================

    private float _nextStepTime;

    private void HandleFootstep()
    {
        bool walking = _wasGrounded && Mathf.Abs(_rb.linearVelocity.x) > 0.2f;

        if (!walking) return;

        if (Time.time >= _nextStepTime)
        {
            if (walkClip)
            {
                _sfx.pitch = Random.Range(0.95f, 1.05f);
                _sfx.PlayOneShot(walkClip);
            }

            float speed = Mathf.Abs(_rb.linearVelocity.x);
            float interval = Mathf.Lerp(0.5f, 0.25f, speed / 10f);

            _nextStepTime = Time.time + interval; // 🔥 kunci utama
        }
    }

    // ========================
    // LOOP AUDIO
    // ========================

    public void StartGlide()
    {
        if (_loop.isPlaying) return;

        if (glideClip)
        {
            _loop.clip = glideClip;
            _loop.Play();
        }
    }

    public void StopGlide()
    {
        _loop.Stop();
    }

    // ========================
    // ONE SHOT SFX
    // ========================

    public void PlayThrow() => Play(throwClip);
    public void PlayTeleport() => Play(teleportClip);
    public void PlaySwing() => Play(swingClip);
    public void PlayDeath() => Play(deathClip);

    public void PlayDash()
    {
        if (dashClip)
            _sfx.PlayOneShot(dashClip);
    }

    public void StartClimb()
    {
        if (climbClip)
            _sfx.PlayOneShot(climbClip);
    }

    public void StopClimb() { }

    private void Play(AudioClip clip)
    {
        if (clip)
            _sfx.PlayOneShot(clip);
    }
}