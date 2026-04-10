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

    public bool isActive = false;
    public SplineContainer spline;
    public LineRenderer line;
    public ScriptableStats stats;
    public float ogFallAccel;
    public SplineAnimate playerSplineAnim;
    public PlayerController playerController;
    public Vector2 cachedTravelDirection;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        spline = GetComponent<SplineContainer>();
        CleanupFx();
        var goog = FindAnyObjectByType<PlayerController>();
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
        LineToSpline();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (line != null && spline != null) LineToSpline();
#endif
        if (playerSplineAnim != null && playerSplineAnim.IsPlaying)
        {
            cachedTravelDirection = GetSplineTravelDirection();

            if (!canDismount || !isActive) return;
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
        isActive = true;

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
        Vector3 localTangent = (Vector3)spline.EvaluateTangent(t);
        Vector3 worldTangent = transform.TransformDirection(localTangent);
        return new Vector2(worldTangent.x, worldTangent.y).normalized;
    }

    private void ApplyInheritedVelocity()
    {
        if (!inheritVelocity || playerController == null) return;

        // Use cached direction instead of sampling at teardown time
        Vector2 inheritedVel = cachedTravelDirection * speed * velocityInheritanceScale;
        playerController.SetFrameVelocity(inheritedVel);

        Debug.Log($"{cachedTravelDirection}, {inheritedVel}");
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
        if (!isActive) return;
        isActive = false;
        if (playerSplineAnim.IsPlaying) playerSplineAnim.Pause();
        playerSplineAnim.Completed -= StopZipline;
        ApplyInheritedVelocity();
        playerSplineAnim.Container = null;
        stats.FallAcceleration = ogFallAccel;
        CleanupFx();
    }

    private void DismountZipline()
    {
        StopZipline();
        playerController.ForceJump();
    }
}