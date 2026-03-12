using DG.Tweening;
using System.Collections;
using TarodevController;
using Unity.VisualScripting;
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
    private bool zippin;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        spline = GetComponent<SplineContainer>();
        var goog = FindFirstObjectByType<PlayerController>();
        if (goog != null)
        {
            stats = goog.Stats;
            ogFallAccel = stats.FallAcceleration;
        }
    }

    private void Update()
    {
        if (Application.isPlaying || line != null) LineToSpline();
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

        zippin = true;
    }

    private float GetSplineLength()
    {
        float length = 0f;
        Vector3 prev = transform.TransformPoint(spline.EvaluatePosition(0f));

        for (int i = 1; i <= resolution; i++)
        {
            Vector3 curr = transform.TransformPoint(spline.EvaluatePosition(i / resolution));
            length += Vector3.Distance(prev, curr);
            prev = curr;
        }

        return Mathf.Max(length, 0.001f);
    }
}
