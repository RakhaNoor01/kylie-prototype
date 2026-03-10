using UnityEngine;

public class SyncCamera : MonoBehaviour
{
    private Camera mainCam;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        mainCam = Camera.main;
        cam.clearFlags = CameraClearFlags.Depth;
    }

    void LateUpdate()
    {
        //adjust texture size based on size float and camera ortho size
        cam.orthographicSize = mainCam.orthographicSize;
    }
}
