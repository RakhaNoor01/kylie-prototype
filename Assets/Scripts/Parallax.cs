using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public float parallaxMultiplier = 0.2f;

    private Transform cam;
    private Vector3 initialCamPosition;
    private Vector3 initialPosition;

    void Start()
    {
        cam = Camera.main.transform;
        initialCamPosition = cam.position;
        initialPosition = transform.position;
    }

    void LateUpdate()
    {
        float distanceX = cam.position.x - initialCamPosition.x;

        transform.position = new Vector3(
            initialPosition.x + distanceX * parallaxMultiplier,
            initialPosition.y,
            transform.position.z
        );
    }
}
