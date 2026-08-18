using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Splines;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;

        public static PlayerController Instance;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;

        private FrameInput _frameInput;
        [SerializeField] private Vector2 _frameVelocity;

        private bool _cachedQueryStartInColliders;

        //dashstuff
        private Vector2 _dashDirection;
        private float _dashEndTime;
        private bool _dashAvailable = true;

        //dashflagger
        private bool _dashEnabled = true;
        public bool DashEnabled => _dashEnabled;

        //TESTING STUFFFSSSSSSSSS DELETE LATER
        [ContextMenu("Disable Dash")]
        private void DebugDisableDash()
        {
            SetDashEnabled(false);
        }

        [ContextMenu("Enable Dash")]
        private void DebugEnableDash()
        {
            SetDashEnabled(true);
        }


        private PlayerKnockback _knockback;

        private Rigidbody2D _groundedPlatformRb;
        private Rigidbody2D _clingPlatformRb;
        public bool onMovingThing => _groundedPlatformRb != null || _clingPlatformRb != null;

        private PlayerAnimator _anim;

        public float _glideStamina;

        private Vector2 _externalVelocity;
        private Vector2 _platformVelocity;

        private PlayerAudio _audio;

        public Vector2 Velocity => _rb.linearVelocity;
        public Vector2 FrameVelocity => _frameVelocity;
        public ScriptableStats Stats => _stats;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

        private float _time;

        private SplineAnimate _spliner;

        private void Awake()
        {
            Instance = this;
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _knockback = GetComponent<PlayerKnockback>();
            _anim = GetComponentInChildren<PlayerAnimator>();
            _spliner = GetComponent<SplineAnimate>();
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
            _glideStamina = _stats.GlideDuration;
            _audio = GetComponent<PlayerAudio>();
            colY = _col.size.y;
            tag = gameObject.tag;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private bool firstInput;
        public bool FirstInput => firstInput;
        public KeyCode dashkey = KeyCode.LeftShift;
        private void GatherInput()
        {
            if (_spliner.IsPlaying)
            {
                _frameInput = new FrameInput();
                return;
            }

            bool anyInput =
                Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
                Input.GetKey(KeyCode.Mouse0);

            if (!firstInput && anyInput)
            {
                gameObject.tag = tag;
                firstInput = true;
            }

            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                DashDown = Input.GetKeyDown(dashkey)
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
            if (_knockback != null && _knockback.IsKnockedBack())
            {
                return;
            }

            CheckCollisions();

            CheckWallContact();
            HandleWallCling();
            HandleWallJump();
            HandleWallSlide();

            HandleDash();
            HandleStepUp();
            HandleJump();
            HandleDirection();
            HandleGravity();

            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        public bool _grounded;
        public bool Grounded => _grounded;

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Horror Code
            if (_grounded)
            {
                if (_stats.DashRefreshOnGround) _dashAvailable = true;
            }
        }

        public bool airborne { get; set; }

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(~_stats.PlayerLayer);
            filter.useLayerMask = true;

            RaycastHit2D[] results = new RaycastHit2D[1];
            int hitCount = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0,
                Vector2.down,
                filter,
                results,
                _stats.GrounderDistance
            );

            RaycastHit2D groundHit = hitCount > 0 ? results[0] : default;
            bool collTouchGround = _col.IsTouching(filter);

            bool isGrounded = false;

            if (airborne)
            {
                isGrounded = groundHit && collTouchGround;
                if (isGrounded)
                {
                    airborne = false;
                }
            }
            else
            {
                isGrounded = groundHit;
            }

                ContactFilter2D ceilFilter = new ContactFilter2D();
            ceilFilter.useTriggers = false;
            ceilFilter.SetLayerMask(~_stats.PlayerLayer);
            ceilFilter.useLayerMask = true;

            RaycastHit2D[] ceilResults = new RaycastHit2D[2]; // increased buffer to catch One Way objects
            int ceilHitCount = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0,
                Vector2.up,
                ceilFilter,
                ceilResults,
                _stats.GrounderDistance
            );

            bool ceilingHit = false;
            for (int i = 0; i < ceilHitCount; i++)
            {
                if (!ceilResults[i].collider.CompareTag("One Way"))
                {
                    ceilingHit = true;
                    break;
                }
            }

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

                _glideStamina = _stats.GlideDuration;

                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }

            // Left the Ground
            else if (_grounded && !isGrounded)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            // Detect moving platform and inherit its velocity
            if (isGrounded)
            {
                _groundedPlatformRb = groundHit.collider.attachedRigidbody;

                if (_groundedPlatformRb != null)
                {
                    Vector2 platVel = _groundedPlatformRb.linearVelocity;
                    _platformVelocity.x = platVel.x;

                    if (platVel.y > 0 || (platVel.y < 0 && _frameVelocity.y <= 0))
                        _platformVelocity.y = platVel.y;
                    else
                        _platformVelocity.y = 0f;
                }
                else
                {
                    _platformVelocity = Vector2.zero;
                }
            }
            else
            {
                _groundedPlatformRb = null;
                _platformVelocity = Vector2.zero;
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        private float _clingTimer;

        public bool IsClinging => _isClinging;
        public bool IsWallSliding => _isWallSliding;
        public bool IsGliding => _isGliding;

        public bool TouchingLeftWall { get; private set; }
        public bool TouchingRightWall { get; private set; }

        public KeyCode wallClingKey;

        #region WallInteraction

        private bool _isTouchingWall;
        private bool _isClinging;
        private bool _isWallSliding;
        private bool _isGliding;
        public float _wallCoyoteTimer;
        private int _lastWallDirection;
        private bool _wasClinging;

        public bool ImWallCoyoting => (_wallCoyoteTimer >= 0);

        public void ForceGroundedRespawn()
        {
            _grounded = true;
            _isClinging = false;
            _isWallSliding = false;
            _isTouchingWall = false;
            _isDashing = false;
            _isGliding = false;
            _jumpToConsume = false;
            _frameVelocity = Vector2.zero;
        }

        public bool blockExVel;

        private void CheckWallContact()
        {
            float verticalOffset = _col.size.y * 0.4f;
            Vector2 top = new Vector2(transform.position.x, transform.position.y + verticalOffset);
            Vector2 bottom = new Vector2(transform.position.x, transform.position.y);

            Vector2 checkDirection = _facingDirection > 0 ? Vector2.right : Vector2.left;

            RaycastHit2D topHit = Physics2D.Raycast(top, checkDirection, _stats.wallCheckDistance, _stats.wallLayer);
            RaycastHit2D bottomHit = Physics2D.Raycast(bottom, checkDirection, _stats.wallCheckDistance, _stats.wallLayer);

            bool raysHitWall = topHit && bottomHit;

            ContactFilter2D wallFilter = new ContactFilter2D();
            wallFilter.SetLayerMask(_stats.wallLayer);
            wallFilter.useTriggers = false;
            wallFilter.useLayerMask = true;
            bool colliderTouchingWall = _col.IsTouching(wallFilter);

            bool touchingWall = raysHitWall && colliderTouchingWall;

            _isTouchingWall = touchingWall;

            TouchingLeftWall = _facingDirection < 0 && touchingWall;
            TouchingRightWall = _facingDirection > 0 && touchingWall;

#if UNITY_EDITOR
            Debug.DrawRay(top, checkDirection * _stats.wallCheckDistance, topHit ? (touchingWall ? Color.green : Color.blue) : Color.skyBlue);
            Debug.DrawRay(bottom, checkDirection * _stats.wallCheckDistance, bottomHit ? (touchingWall ? Color.green : Color.blue) : Color.skyBlue);
#endif

            _clingPlatformRb = touchingWall
                ? (topHit.rigidbody != null ? topHit.rigidbody : bottomHit.rigidbody)
                : null;

            if (colliderTouchingWall && !_grounded && !_isClinging && !_isWallSliding && Mathf.Abs(_frameVelocity.x) > 0.1f)
            {
                _frameVelocity.x = 0f;
                _externalVelocity.x = 0f;
            }

            if (colliderTouchingWall && !_grounded)
            {
                blockExVel = true;
            } 
            else
            {
                blockExVel = false;
            }
        }

        private void HandleWallCling()
        {
            if (_grounded)
            {
                _isClinging = false;
                _wasClinging = false;
                return;
            }

            if (_isTouchingWall && Input.GetKey(wallClingKey))
            {
                // If we were already clinging, only allow re-cling when falling
                bool canCling = !_wasClinging || _frameVelocity.y <= 0;

                if (canCling && _clingTimer < _stats.maxClingTime)
                {
                    _isClinging = true;
                    _wasClinging = true;
                    _isWallSliding = false;
                    _frameVelocity = Vector2.zero;
                    _clingTimer += Time.deltaTime;
                }
                else
                {
                    _isClinging = false;
                    _isWallSliding = true; // transition into slide
                }
            }
            else
            {
                // Released Fah → clear cling AND slide
                _isClinging = false;
                _isWallSliding = false;
                _clingTimer = 0;
            }

            // Audio Cling
            if (_isClinging)
                _audio?.StartClimb();
            else
                _audio?.StopClimb();

            if (_anim != null) _anim.SetCling(_isClinging);
        }

        private void HandleWallSlide()
        {
            if (_grounded)
            {
                _isWallSliding = false;
                _wasClinging = false;
                return;
            }

            // Only slide if still touching wall AND F is held
            if (!_isClinging && _wasClinging && _isTouchingWall && Input.GetKey(wallClingKey))
            {
                _isWallSliding = true;
                if (_frameVelocity.y < -_stats.wallSlideSpeed)
                    _frameVelocity.y = -_stats.wallSlideSpeed;
            }
            else
            {
                _isWallSliding = false;
            }

            // audio wall slide
            if (_isWallSliding)
                _audio?.StartClimb();
            else if (!_isClinging)
                _audio?.StopClimb();

            if (_anim != null) _anim.SetWallSlide(_isWallSliding);
        }

        private void HandleWallJump()
        {
            // Update wall coyote timer
            if (_isTouchingWall && (_isClinging || _isWallSliding))
            {
                _wallCoyoteTimer = _stats.wallCoyoteTime;
            }
            else
            {
                _wallCoyoteTimer -= Time.deltaTime;
            }

            // Check if player can wall jump
            bool canWallJump = (_isClinging || _isWallSliding || _wallCoyoteTimer > 0) && !_grounded;
            if (canWallJump && (_jumpToConsume || HasBufferedJump))
            {
                _facingDirection = -_lastWallDirection;
                _frameVelocity.x = -_lastWallDirection * _stats.MaxSpeed;

                ForceJump();

                _isClinging = false;
                _isWallSliding = false;
                _wallCoyoteTimer = 0;
                _jumpToConsume = false;
            }
            // Reset cling/slide when grounded
            if (_grounded)
            {
                _isClinging = false;
                _isWallSliding = false;
            }
        }

        public void ResetWallStates()
        {
            _isClinging = false;
            _isWallSliding = false;
            _isTouchingWall = false;
            _isGliding = false;
            _wallCoyoteTimer = 0;
        }


        #endregion

        #region Dash

        private bool _dashToConsume;
        private bool _isDashing;
        public bool doWeDeserveDestruction;

        private Tweener _dashTween;

        private float colY;
        private void HandleDash()
        {
            // Always check if an active dash has expired, regardless of other states
            if (_time >= _dashEndTime)
            {
                if (_isDashing)
                {
                    _frameVelocity *= _stats.DashMomentumRetention;
                }
                _col.size = new Vector2(_col.size.x, colY);
                _isDashing = false;
            }

            // Block new dashes while boomerang is charging or on a spline
            if (doWeDeserveDestruction || _spliner.IsPlaying)
            {
                _dashToConsume = false;
                return;
            }

            // If level even allow dash or not, a Forest gate
            if (!_dashEnabled)
            {
                _dashToConsume = false;
                return;
            }

            //If level allow dash AND dash is available
            if (!_dashToConsume || !_dashAvailable)
            {
                _dashToConsume = false;
                return;
            }

            ExecuteDash();
            _dashToConsume = false;
        }

        [SerializeField] private DashEffect _dashEffect;

        private void ExecuteDash()
        {
            // disable any variable jump grav tweaks
            _endedJumpEarly = true;
            _bufferedJumpUsable = false;
            _jumpToConsume = false;
            _timeJumpWasPressed = float.MinValue;
            airborne = true;

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

            _col.size = new Vector2 (_col.size.x, _col.size.x);

            _dashDirection = inputDirection.normalized;
            _frameVelocity = _dashDirection * _stats.DashSpeed;
            _isDashing = true;
            _dashEndTime = _time + _stats.DashDuration;
            _dashAvailable = false;

            _audio?.PlayDash();

            if (_dashEffect != null)
            {
                _dashEffect.OnDash(_dashDirection);
            }
        }

        public void SetDashEnabled(bool enabled)
        {
            _dashEnabled = enabled;

            if (!enabled)
            {
                _dashToConsume = false;

                if (_isDashing)
                    CancelDash();
            }
        }

        public bool IsDashing => _isDashing;

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;

        private float _timeJumpWasPressed;

        public bool HasBufferedJump =>
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

            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _frameVelocity.y > 0)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump)
                return;

            if (_grounded || CanUseCoyote)
            {
                ExecuteJump();
            }

            _jumpToConsume = false;
        }

        // doom code
        public void ForceJump()
        {
            ExecuteJump();
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;

            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity.y = _stats.JumpPower;

            airborne = true;

            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private int _facingDirection = 1;
        public int FacingDirection => _facingDirection;

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
                float targetSpeed = _frameInput.Move.x * _stats.MaxSpeed;

                bool exceedingInSameDirection =
                    Mathf.Sign(_frameVelocity.x) == Mathf.Sign(targetSpeed) &&
                    Mathf.Abs(_frameVelocity.x) > Mathf.Abs(targetSpeed) && !_grounded;

                if (_isGliding) targetSpeed *= _stats.GlideSpeedMultiplier;

                if (!exceedingInSameDirection)
                {
                    _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, targetSpeed, _stats.Acceleration * Time.fixedDeltaTime);
                }
            }
        }

        #endregion

        #region Gravity

        public bool Glider {get; set;}

        private void HandleGravity()
        {
            if (_isClinging && !_isDashing)
            {
                _rb.linearVelocity = Vector2.zero;
                _frameVelocity = Vector3.zero;
                Vector2 platformVel = (_clingPlatformRb != null)
                    ? _clingPlatformRb.linearVelocity
                    : Vector2.zero;
                _platformVelocity = platformVel;
                return;
            }

            if (HandleGlide()) return;

            if (_isDashing) return;

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

        private bool HandleGlide()
        {
            if (!Glider) return false;

            // Determine glide state
            _isGliding = _frameInput.JumpHeld && !_grounded && _frameVelocity.y < 0;

            if (_isGliding)
                _audio?.StartGlide();
            else
                _audio?.StopGlide();

            if (_anim != null) _anim.SetGlide(_isGliding);

            if (!_isGliding) return false;

            _glideStamina -= Time.fixedDeltaTime;
            if (_glideStamina <= 0)
            {
                _isGliding = false;
                return false;
            }

            _frameVelocity.y = Mathf.MoveTowards(
                _frameVelocity.y,
                -_stats.GlideSpeed,
                _stats.GlideEntrySpeed * Time.fixedDeltaTime
            );

            return true;
        }

        #endregion

        private Tween _stepTween;
        private bool _steppingUp;

        private void HandleStepUp()
        {
            if (Mathf.Abs(_frameVelocity.x) < 0.1f)
                return;

            Vector2 dir = new Vector2(_facingDirection, 0);

            Vector2 baseOrigin = (Vector2)_col.bounds.center + Vector2.down * _stats.stepUpBase;

            Vector2 upperOrigin = baseOrigin + Vector2.up * _stats.stepUpHeight;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.SetLayerMask(~_stats.wallLayer);
            filter.useTriggers = false;

            RaycastHit2D[] results = new RaycastHit2D[1];

            int baseCount = Physics2D.Raycast(
                baseOrigin,
                dir,
                filter,
                results,
                _stats.stepUpDistance
            );
            bool baseHit = baseCount > 0;

            int upperCount = Physics2D.Raycast(
                upperOrigin,
                dir,
                filter,
                results,
                _stats.stepUpDistance
            );
            bool upperHit = upperCount > 0;

#if UNITY_EDITOR
            Debug.DrawRay(baseOrigin, dir * _stats.stepUpDistance, baseHit ? Color.green : Color.red);
            Debug.DrawRay(upperOrigin, dir * _stats.stepUpDistance, upperHit ? Color.green : Color.red);
#endif

            if (baseHit && !upperHit && !_steppingUp)
            {
                _steppingUp = true;

                _stepTween?.Kill();
                _stepTween = transform
                    .DOMoveY(transform.position.y + _stats.stepUpHeight, _stats.stepUpSpeed)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => _steppingUp = false);
            }
        }

        private void ApplyMovement()
        {
            _rb.linearVelocity = _frameVelocity + _externalVelocity + _platformVelocity;
            _externalVelocity = Vector2.zero;
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
        /// kill 9 billion people
        /// </summary>
        public void CancelDash()
        {
            _isDashing = false;
            _dashEndTime = 0f;
        }

        public void ResetGlide()
        {
            _glideStamina = _stats.GlideDuration;
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
        /// Adds a persistent external velocity (e.g. air currents) that is
        /// applied on top of the controller each frame.
        /// Call every FixedUpdate from the external system while active.
        /// </summary>
        public void AddExternalVelocity(Vector2 velocity)
        {
            _externalVelocity += velocity;
            airborne = true;
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

            airborne = true;

            _frameVelocity.y = strength;
            Jumped?.Invoke();
        }

        /// <summary>
        /// yeah
        /// </summary>
        /// <param name="bro">yeah</param>
        public void SetFrameVelocity(Vector2 bro)
        {
            _frameVelocity = bro;
            airborne = true;
        }

        public void OhTheMisery()
        {
            _isGliding = true;
            _isGliding = false;
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