using UnityEngine;

public class Parallax : MonoBehaviour
{
    public float parallaxFactor; // smaller = slower movement
    private Transform cam;
    private Vector3 startPos;

    // Flag to lock X movement (set by RoomManager depending on level type)
    public bool lockX = false;

    void Start()
    {
        cam = Camera.main.transform;
        startPos = transform.position;
    }

    void Update()
    {
        float xOffset = lockX ? 0 : cam.position.x * parallaxFactor;
        float yOffset = cam.position.y * parallaxFactor;

        transform.position = new Vector3(
            startPos.x + xOffset,
            startPos.y + yOffset,
            transform.position.z
        );
    }
}
