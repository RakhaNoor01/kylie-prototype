using System;
using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// Hey!
    /// Tarodev here. I built this controller as there was a severe lack of quality & free 2D controllers out there.
    /// I have a premium version on Patreon, which has every feature you'd expect from a polished controller. Link: https://www.patreon.com/tarodev
    /// You can play and compete for best times here: https://tarodev.itch.io/extended-ultimate-2d-controller
    /// If you hve any questions or would like to brag about your score, come to discord: https://discord.gg/tarodev
    /// </summary>

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;

        private FrameInput _frameInput;
        private Vector2 _frameVelocity;

        private bool _cachedQueryStartInColliders;

        private Vector2 _dashDirection;
        private float _dashEndTime;
        private bool _dashAvailable = true;

        private MovablePlatform _groundedPlatform;

        private float _pogoWindowEndTime;
        private bool _pogoAvailable;

        //idk where else to put this variable tbh
        [SerializeField] private float _pogoWindowDuration = 0.25f;

        public Vector2 Velocity => _rb.linearVelocity;
        public ScriptableStats Stats => _stats;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

        private float _time;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                DashDown = Input.GetKeyDown(KeyCode.LeftShift)
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x =
                    Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold
                        ? 0
                        : Mathf.Sign(_frameInput.Move.x);

                _frameInput.Move.y =
                    Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold
                        ? 0
                        : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            if (_frameInput.DashDown)
            {
                _dashToConsume = true;
            }
        }

        private void FixedUpdate()
        {
            CheckCollisions();

            HandleDash();
            HandleJump();
            HandleDirection();
            HandleGravity();

            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;
        public bool Grounded => _grounded;

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Horror Code
            if (_grounded)
            {
                if (_stats.DashRefreshOnGround) _dashAvailable = true;
            }
        }

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            RaycastHit2D groundHit = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0,
                Vector2.down,
                _stats.GrounderDistance,
                ~_stats.PlayerLayer
            );

            bool isGrounded = groundHit;

            bool ceilingHit = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0,
                Vector2.up,
                _stats.GrounderDistance,
                ~_stats.PlayerLayer
            );

            // Hit a Ceiling
            if (ceilingHit)
                _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

            // Landed on the Ground
            if (!_grounded && isGrounded)
            {
                _grounded = true;

                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;

                if (_stats.DashRefreshOnGround)
                    _dashAvailable = true;

                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }

            // Left the Ground
            else if (_grounded && !isGrounded)
            {
                _grounded = false;
                _frameLeftGrounded = _time;

                GroundedChanged?.Invoke(false, 0);
            }

            // Detect moving platform
            if (isGrounded)
            {
                _groundedPlatform = groundHit.collider.GetComponent<MovablePlatform>();
            }
            else
            {
                _groundedPlatform = null;
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Dash

        private bool _dashToConsume;
        private bool _isDashing;
        public bool doWeDeserveDestruction;

        private void HandleDash()
        {
            if (doWeDeserveDestruction) return;

            // Update Dash State
            if (_time >= _dashEndTime)
            {
                if (_isDashing)
                {
                    //Preserve momentum from dash
                    _frameVelocity *= _stats.DashMomentumRetention;
                }

                _isDashing = false;
            }

            if (!_dashToConsume || !_dashAvailable)
            {
                _dashToConsume = false;
                return;
            }

            ExecuteDash();
            _dashToConsume = false;
        }

        private void ExecuteDash()
        {
            // disable any variable jump grav tweaks
            _endedJumpEarly = true;
            _bufferedJumpUsable = false;
            _jumpToConsume = false;
            _timeJumpWasPressed = float.MinValue;

            // Determine dash   direction based on input
            Vector2 inputDirection = _frameInput.Move;

            // If no input, dash in facing direction (based on last horizontal movement)
            if (inputDirection == Vector2.zero)
            {
                inputDirection = new Vector2(
                    Mathf.Sign(_frameVelocity.x != 0 ? _frameVelocity.x : _facingDirection),
                    0
                );
            }

            _coyoteUsable = false;


            _dashDirection = inputDirection.normalized;
            _frameVelocity = _dashDirection * _stats.DashSpeed;
            _isDashing = true;
            _dashEndTime = _time + _stats.DashDuration;
            _dashAvailable = false;
        }

        public bool IsDashing => _isDashing;

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;

        private float _timeJumpWasPressed;

        private bool HasBufferedJump =>
            _bufferedJumpUsable &&
            _time < _timeJumpWasPressed + _stats.JumpBuffer;

        private bool CanUseCoyote =>
            _coyoteUsable &&
            !_grounded &&
            _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (_isDashing)
            {
                _jumpToConsume = false;
                return;
            }

            if (_pogoAvailable && _time > _pogoWindowEndTime)
            {
                _pogoAvailable = false;
            }

            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _frameVelocity.y > 0)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump)
                return;

            if (_grounded || CanUseCoyote)
            {
                ExecuteJump();
            }
            else if (_pogoAvailable && _time <= _pogoWindowEndTime)
            {
                ExecuteJump();
                _pogoAvailable = false;
            }

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;

            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity.y = _stats.JumpPower;

            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private int _facingDirection = 1;

        private void HandleDirection()
        {
            // Skip direction handling during dash
            if (_isDashing)
                return;

            if (_frameInput.Move.x != 0)
                _facingDirection = (int)Mathf.Sign(_frameInput.Move.x);

            if (_frameInput.Move.x == 0)
            {
                var deceleration =
                    _grounded
                        ? _stats.GroundDeceleration
                        : _stats.AirDeceleration;

                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    0,
                    deceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    _frameInput.Move.x * _stats.MaxSpeed,
                    _stats.Acceleration * Time.fixedDeltaTime
                );
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            // Reduce gravity during dash for better control
            if (_isDashing)
            {
                //_frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, 0, _stats.FallAcceleration * Time.fixedDeltaTime);
                //commented the stuff above, shouldnt do anything
                return;
            }

            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;

                if (_endedJumpEarly && _frameVelocity.y > 0)
                    inAirGravity *= _stats.JumpEndEarlyGravityModifier;

                _frameVelocity.y = Mathf.MoveTowards(
                    _frameVelocity.y,
                    -_stats.MaxFallSpeed,
                    inAirGravity * Time.fixedDeltaTime
                );
            }
        }

        #endregion

        private void ApplyMovement()
        {
            Vector2 finalVelocity = _frameVelocity;

            if (_groundedPlatform != null)
            {
                Vector2 platformVelocity =
                    _groundedPlatform.Delta / Time.fixedDeltaTime;

                finalVelocity += platformVelocity;
            }

            _rb.linearVelocity = finalVelocity;
        }


        public bool DashAvailable => _dashAvailable;

        /// <summary>
        /// Immediately restores the ability to dash.  External systems (eg. pickups) can
        /// call this to give the player another dash.
        /// </summary>
        public void RechargeDash()
        {
            _dashAvailable = true;
        }

        /// <summary>
        /// Called by moving platforms (or other external movers) to apply additional
        /// velocity to the controller for the duration of the frame.  This is used to
        /// "fix" the player to a platform even though the controller overwrites the
        /// rigidbody velocity each tick.
        /// </summary>
        /// <param name="externalVelocity">Velocity to add.</param>
        public void AddPlatformVelocity(Vector2 externalVelocity)
        {
            _frameVelocity += externalVelocity;
        }

        /// <summary>
        /// External systems (bounce pads, launchers, etc.) can call this to force a vertical
        /// velocity on the player.  The implementation uses the same velocity field that the
        /// controller's jump code does so that the gravity, coyote time, buffering, etc. all
        /// continue to operate normally.
        /// </summary>
        /// <param name="strength">The y‑velocity to apply to the player.</param>
        public void ApplyBounce(float strength)
        {
            // clear jump buffers / coyote so that the player isn't able to immediately
            // double‑jump or perform other ground‑based tricks right after being thrown.
            _endedJumpEarly = false;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            // make sure we treat the player as having just left the ground
            _grounded = false;
            _frameLeftGrounded = _time;

            _frameVelocity.y = strength;
            Jumped?.Invoke();
        }

        public void ActivatePogoWindow()
        {
            _pogoAvailable = true;
            _pogoWindowEndTime = _time + _pogoWindowDuration;

            // If jump was buffered BEFORE pogo became active
            if (HasBufferedJump)
            {
                ExecuteJump();
                _pogoAvailable = false;
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
                Debug.LogWarning(
                    "Please assign a ScriptableStats asset to the Player Controller's Stats slot",
                    this
                );
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public bool DashDown;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;

        public event Action Jumped;
        public Vector2 FrameInput { get; }

        /// <summary>
        /// Apply an immediate vertical velocity to the controller.  Implementations should
        /// use their internal movement logic so that gravity/coyote/jump buffer etc. remain
        /// consistent.
        /// </summary>
        /// <param name="strength">Y velocity to set (positive = up)</param>
        public void ApplyBounce(float strength);

        /// <summary>
        /// Restores dash availability; picked up by dash recharge pickups.
        /// </summary>
        public void RechargeDash();

        /// <summary>
        /// Add an external velocity such as from a moving platform.  This is applied on
        /// top of whatever the controller computes internally.
        /// </summary>
        /// <param name="externalVelocity">Horizontal/vertical velocity to add.</param>
        public void AddPlatformVelocity(Vector2 externalVelocity);
    }
}