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

    private bool wasGrounded;
    private float landingSquash;
    private float lastYVelocity;

    private Vector3 defaultScale;

    private int facing = 1;


    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    private void LateUpdate()
    {

        if (controller.FrameVelocity.x > 0.1f)
            facing = 1;
        else if (controller.FrameVelocity.x < -0.1f)
            facing = -1;


        float yVelocity = controller.FrameVelocity.y;


        float t = Mathf.Clamp(yVelocity / maxVerticalSpeed, -1f, 1f);

        // Positive = going up
        // Negative = falling
        float stretch = Mathf.Abs(t) * maxStretch;
        landingSquash = Mathf.Lerp(landingSquash,0f,landingRecoverSpeed * Time.deltaTime);

        Vector3 targetScale = defaultScale;

        float totalX = -stretch + landingSquash;
        float totalY = stretch - landingSquash;

        targetScale.x = facing * defaultScale.x * (1f + totalX);
        targetScale.y = defaultScale.y * (1f + totalY);

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            smoothSpeed * Time.deltaTime);

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
            Mathf.Abs(lastYVelocity));

        landingSquash = impact * maxLandingSquash;
    }

}
