using TarodevController;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(Collider2D), typeof(SplineContainer))]
public class Zipline : MonoBehaviour
{
    public int resolution = 20;
    public float speed = 5f;

    private SplineContainer spline;
    private LineRenderer line;

    private ScriptableStats stats;
    private float ogFallAccel;

    private SplineAnimate playerSplineAnim;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        spline = GetComponent<SplineContainer>();
        var goog = FindFirstObjectByType<PlayerController>();
        if (goog != null)
        {
            playerSplineAnim = goog.GetComponent<SplineAnimate>();
            playerSplineAnim.Completed += StopZipline;

            stats = goog.Stats;
            ogFallAccel = stats.FallAcceleration;
        }
    }

    private void Update()
    {
        if (line != null && spline != null) LineToSpline();
    }

    private void LineToSpline()
    {
        line.positionCount = resolution;

        for (int i = 0; i < resolution; i++)
        {
            // Calculate progress along spline (0 to 1)
            float t = i / (float)(resolution - 1);

            // Sample position in world space
            Vector3 worldPos = transform.TransformPoint(spline.EvaluatePosition(t));
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            line.SetPosition(i, localPos);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        playerSplineAnim.NormalizedTime = 0f;
        playerSplineAnim.Container = spline;
        playerSplineAnim.MaxSpeed = speed;
        playerSplineAnim.Play();
        stats.FallAcceleration = 0f;
    }

    private void StopZipline()
    {
        stats.FallAcceleration = ogFallAccel;
        playerSplineAnim.Container = null;
    }
}
