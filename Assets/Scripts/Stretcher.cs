using TarodevController;
using UnityEngine;

public class Stretcher : MonoBehaviour
{
    [Header("Controller")]
    [SerializeField] private PlayerController controller;

    [Header("Main Stretcher")]
    [SerializeField] private float maxStretch = 0.08f;
    [SerializeField] private float maxVerticalSpeed = 20f;
    [SerializeField] private float smoothSpeed = 15f;

    [Header("Landing Squash")]
    [SerializeField] private float maxLandingSquash = 0.18f;
    [SerializeField] private float landingRecoverSpeed = 12f;
    [SerializeField] private float minFallSpeed = 5f;

    [Header("Dash Stretch")]
    [SerializeField] private float dashStretch = 0.10f;
    [SerializeField] private float dashSmoothSpeed = 20f;

    private bool wasGrounded;
    private float landingSquash;
    private float lastYVelocity;


    private float currentDashStretchX;
    private float currentDashStretchY;

    private Vector3 defaultScale;

    private int facing = 1;


    private void Awake()
    {
        defaultScale = transform.localScale;
    }


    private void LateUpdate()
    {
        // Update facing direction
        if (controller.FrameVelocity.x > 0.1f)
            facing = 1;
        else if (controller.FrameVelocity.x < -0.1f)
            facing = -1;





        // =========================
        // Landing Squash
        // =========================

        landingSquash = Mathf.Lerp(
            landingSquash,
            0f,
            landingRecoverSpeed * Time.deltaTime
        );

        // =========================
        // Movement Stretch
        // =========================

        float targetStretchX = 0f;
        float targetStretchY = 0f;
        if (controller.IsDashing)
        {
            Vector2 dashVelocity = controller.FrameVelocity;

            Debug.Log(
                $"DASH | FrameVelocity: {dashVelocity} | X: {Mathf.Abs(dashVelocity.x)} | Y: {Mathf.Abs(dashVelocity.y)}"
            );

            if (Mathf.Abs(dashVelocity.x) > Mathf.Abs(dashVelocity.y))
            {
                targetStretchX = dashStretch;
                targetStretchY = -dashStretch * 0.5f;
            }
            else
            {
                targetStretchX = -dashStretch * 0.5f;
                targetStretchY = dashStretch;
            }
        }
        else
        {
            // Normal vertical movement stretch
            float yVelocity = controller.FrameVelocity.y;

            float t = Mathf.Clamp(
                yVelocity / maxVerticalSpeed,
                -1f,
                1f
            );

            float stretch = Mathf.Abs(t) * maxStretch;

            targetStretchX = -stretch;
            targetStretchY = stretch;
        }

        // =========================
        // Final Scale
        // =========================

        Vector3 targetScale = defaultScale;

        float totalX =
            targetStretchX
            + landingSquash;

        float totalY =
            targetStretchY
            - landingSquash;

        targetScale.x =
            facing *
            defaultScale.x *
            (1f + totalX);

        targetScale.y =
            defaultScale.y *
            (1f + totalY);

        transform.localScale = targetScale;

        // =========================
        // Landing Detection
        // =========================

        lastYVelocity = controller.FrameVelocity.y;

        if (!wasGrounded && controller.Grounded)
        {
            OnLand();
        }

        wasGrounded = controller.Grounded;
    }


    private void OnLand()
    {
        float impact = Mathf.InverseLerp(
            minFallSpeed,
            maxVerticalSpeed,
            Mathf.Abs(lastYVelocity)
        );

        landingSquash = impact * maxLandingSquash;
    }
}