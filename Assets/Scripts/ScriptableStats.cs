using UnityEngine;

namespace TarodevController
{
    [CreateAssetMenu]
    public class ScriptableStats : ScriptableObject
    {
        [Header("LAYERS")]
        [Tooltip("Set this to the layer your player is on")]
        public LayerMask PlayerLayer;

        [Header("INPUT")]
        [Tooltip("Makes all Input snap to an integer. Prevents gamepads from walking slowly. Recommended value is true to ensure gamepad/keybaord parity.")]
        public bool SnapInput = true;

        [Tooltip("Minimum input required before you mount a ladder or climb a ledge. Avoids unwanted climbing using controllers"), Range(0.01f, 0.99f)]
        public float VerticalDeadZoneThreshold = 0.3f;

        [Tooltip("Minimum input required before a left or right is recognized. Avoids drifting with sticky controllers"), Range(0.01f, 0.99f)]
        public float HorizontalDeadZoneThreshold = 0.1f;

        [Header("MOVEMENT")]
        [Tooltip("The top horizontal movement speed")]
        public float MaxSpeed = 14;

        [Tooltip("The player's capacity to gain horizontal speed")]
        public float Acceleration = 120;

        [Tooltip("The pace at which the player comes to a stop")]
        public float GroundDeceleration = 60;

        [Tooltip("Deceleration in air only after stopping input mid-air")]
        public float AirDeceleration = 30;

        [Tooltip("A constant downward force applied while grounded. Helps on slopes"), Range(0f, -10f)]
        public float GroundingForce = -1.5f;

        [Tooltip("The detection distance for grounding and roof detection"), Range(0f, 0.5f)]
        public float GrounderDistance = 0.05f;

        [Header("JUMP")]
        [Tooltip("The immediate velocity applied when jumping")]
        public float JumpPower = 36;

        [Tooltip("The maximum vertical movement speed")]
        public float MaxFallSpeed = 40;

        [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
        public float FallAcceleration = 110;

        [Tooltip("The gravity multiplier added when jump is released early")]
        public float JumpEndEarlyGravityModifier = 3;

        [Tooltip("The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
        public float CoyoteTime = .15f;

        [Tooltip("The amount of time we buffer a jump. This allows jump input before actually hitting the ground")]
        public float JumpBuffer = .2f;

        [Header("DASH")]
        [Tooltip("The speed of the dash")]
        public float DashSpeed = 40;

        [Tooltip("How long the dash lasts in seconds")]
        public float DashDuration = 0.15f;

        [Tooltip("Cooldown between dashes in seconds (0 for Celeste-like instant refresh)")]
        public float DashCooldown = 0f;

        [Tooltip("Percentage of dash speed preserved after dash ends")]
        public float DashMomentumRetention = 0.3f;

        [Tooltip("Whether the player gets a dash reset when touching the ground")]
        public bool DashRefreshOnGround = true;

        [Header("WALL CLIMBING")]

        [Tooltip("How long the player can cling onto walls before sliding")]
        public float maxClingTime = 3f;

        [Tooltip("Raycast distance for wall checks")]
        public float wallCheckDistance = 0.4f;

        [Tooltip("Walla")]
        public LayerMask wallLayer;

        [Tooltip("How fast the player slides down walls")]
        public float wallSlideSpeed = 2f;

        [Tooltip("idk what this one does tbh")]
        public float wallCoyoteTime = 0.2f;
    }
}