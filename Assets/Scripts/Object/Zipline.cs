using TarodevController;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(Collider2D), typeof(SplineContainer))]
public class Zipline : MonoBehaviour
{
    public int resolution = 20;
    public float speed = 5f;
    public ParticleSystem ziplineFx;
    public bool canDismount = false;

    [Header("Velocity Inheritance")]
    public bool inheritVelocity = false;
    public float velocityInheritanceScale = 0.8f;

    private SplineContainer spline;
    private LineRenderer line;
    private ScriptableStats stats;
    private float ogFallAccel;
    private SplineAnimate playerSplineAnim;
    private PlayerController playerController;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        spline = GetComponent<SplineContainer>();
        CleanupFx();
        var goog = FindFirstObjectByType<PlayerController>();
        if (goog != null)
        {
            playerController = goog;
            playerSplineAnim = goog.GetComponent<SplineAnimate>();
            stats = goog.Stats;
            ogFallAccel = stats.FallAcceleration;
            var playerHealth = playerController.gameObject.GetComponent<PlayerHealth>();
            playerHealth.death += StopZipline;
        }
        else
        {
            Debug.Log("where goo");
        }
        ziplineFx.Stop();
    }

    private void Update()
    {
        if (line != null && spline != null) LineToSpline();
        if (canDismount && playerSplineAnim != null && playerSplineAnim.IsPlaying)
        {
            if (Input.GetButtonDown("Jump"))
            {
                DismountZipline();
            }
        }
    }

    private void LineToSpline()
    {
        line.positionCount = resolution;
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            Vector3 worldPos = transform.TransformPoint(spline.EvaluatePosition(t));
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            line.SetPosition(i, localPos);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Unsubscribe first to avoid double-registering if re-ridden
        playerSplineAnim.Completed -= StopZipline;
        playerSplineAnim.Completed += StopZipline;

        playerSplineAnim.NormalizedTime = 0f;
        playerSplineAnim.Container = spline;
        playerSplineAnim.MaxSpeed = speed;
        playerSplineAnim.Play();
        stats.FallAcceleration = 0f;
        ziplineFx.transform.parent = collision.gameObject.transform;
        ziplineFx.transform.position = collision.gameObject.transform.position;
        ziplineFx.Play();
    }

    private Vector2 GetSplineTravelDirection()
    {
        float t = playerSplineAnim.NormalizedTime;
        // Sample tangent in local spline space, then convert to world
        Vector3 localTangent = (Vector3)spline.EvaluateTangent(t);
        Vector3 worldTangent = transform.TransformDirection(localTangent);
        return new Vector2(worldTangent.x, worldTangent.y).normalized;
    }

    private void ApplyInheritedVelocity()
    {
        if (!inheritVelocity || playerController == null) return;

        Vector2 travelDir = GetSplineTravelDirection();
        Vector2 inheritedVel = travelDir * speed * velocityInheritanceScale;

        // Use ApplyBounce for the vertical component, then nudge horizontal
        // via AddPlatformVelocity for one frame so the controller's own
        // deceleration takes over naturally from there.
        playerController.SetFrameVelocity(inheritedVel);

        Debug.Log($"{travelDir}, {inheritedVel}");
    }

    private void CleanupFx()
    {
        if (ziplineFx == null) return;
        ziplineFx.Stop();
        ziplineFx.transform.parent = transform;
        ziplineFx.transform.position = transform.position;
    }

    private void StopZipline()
    {
        playerSplineAnim.Completed -= StopZipline;

        ApplyInheritedVelocity();
        playerSplineAnim.Container = null;
        stats.FallAcceleration = ogFallAccel;
        CleanupFx();
    }

    private void DismountZipline()
    {
        if (!canDismount) return;

        playerSplineAnim.Completed -= StopZipline;

        ApplyInheritedVelocity();
        playerSplineAnim.Pause();

        playerSplineAnim.Container = null;
        stats.FallAcceleration = ogFallAccel;
        CleanupFx();

        playerController.ForceJump();
    }
}