using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class LineRendererSplineSlop : MonoBehaviour
{
#if UNITY_EDITOR
    [Range(10, 200)]
    public int totalLinePoints = 50;

    private LineRenderer lineRenderer;
    private SplineContainer splineContainer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        splineContainer = GetComponent<SplineContainer>();
    }

    void LateUpdate()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;

        lineRenderer.positionCount = totalLinePoints;

        for (int i = 0; i < totalLinePoints; i++)
        {
            // Normalize current segment index to a 0.0 - 1.0 range
            float t = i / (float)(totalLinePoints - 1);

            // Sample position data directly from Unity's spline logic
            Vector3 worldPos = splineContainer.EvaluatePosition(t);
            lineRenderer.SetPosition(i, worldPos);
        }
    }
#endif
}
