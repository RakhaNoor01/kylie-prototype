using UnityEngine;

public class ParallaxLayer_Test : MonoBehaviour
{
    [SerializeField] private float parallaxFactor = 0.5f;

    private Transform _tileA;
    private Transform _tileB;
    private float _width;

    private Vector3 _startPos;
    private Vector3 _cameraStart;
    private Transform _cameraTransform;


    private void Start()
    {
        _tileA = transform.GetChild(0);
        _tileB = transform.GetChild(1);

        _width = _tileA.GetComponent<SpriteRenderer>().bounds.size.x;

        var cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("No Main Camera found!");
            enabled = false;
            return;
        }

        _cameraTransform = cam.transform;

        _startPos = transform.position;
        _cameraStart = _cameraTransform.position;
    }



    private void LateUpdate()
    {
        HandleParallax();
        HandleLoop();
    }

    private void HandleParallax()
    {
        Vector3 delta = _cameraTransform.position - _cameraStart;

        transform.position = new Vector3(
            _startPos.x + delta.x * parallaxFactor,
            _cameraTransform.position.y,
            _startPos.z
        );
    }

    private void HandleLoop()
    {
        float camX = _cameraTransform.position.x;

        
        if (camX - _tileA.position.x >= _width)
        {
            _tileA.position += Vector3.right * _width * 2;
        }
        else if (camX - _tileA.position.x <= -_width)
        {
            _tileA.position -= Vector3.right * _width * 2;
        }

        
        if (camX - _tileB.position.x >= _width)
        {
            _tileB.position += Vector3.right * _width * 2;
        }
        else if (camX - _tileB.position.x <= -_width)
        {
            _tileB.position -= Vector3.right * _width * 2;
        }
    }
}