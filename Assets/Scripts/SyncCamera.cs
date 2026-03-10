using UnityEngine;

public class SyncCamera : MonoBehaviour
{
    private Camera mainCam;
    private Camera cam;
    public float size = 0.2f;
    public RenderTexture texture;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        //adjust texture size based on size float and camera ortho size
        cam.orthographicSize = mainCam.orthographicSize;
    }
}
