using UnityEngine;

[DefaultExecutionOrder(100)] // Runs after normal movement scripts
public class RenderTexCameraSnapping : MonoBehaviour
{
    public float pixelsPerUnit = 16f;
    private Vector3 truePosition;
    private Camera maincam;

    private void Awake()
    {
        maincam = Camera.main;
    }

    void LateUpdate()
    {
        // Unsnapped position
        truePosition = maincam.transform.position;

        // Calculate pixel size
        float pixelSize = 1f / pixelsPerUnit;

        // Snap position to pixel grid
        Vector3 snappedPosition = new Vector3(
            Mathf.Round(truePosition.x / pixelSize) * pixelSize,
            Mathf.Round(truePosition.y / pixelSize) * pixelSize,
            truePosition.z
        );

        // Set to snapped position
        transform.position = snappedPosition;
    }
}
