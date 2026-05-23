using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightingManager : MonoBehaviour
{
    public static LightingManager Instance { get; private set; }

    [SerializeField] private Light2D globalLight;

    private float _targetIntensity;
    private Color _targetColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        globalLight.intensity = _targetIntensity;
        globalLight.color = _targetColor;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetRoom(Room room)
    {
        _targetIntensity = room.globalLightIntensity;
        _targetColor = room.globalLightColor;
    }

    private void Update()
    {
        globalLight.intensity = Mathf.Lerp(
            globalLight.intensity,
            _targetIntensity,
            Time.deltaTime * 3f
        );

        globalLight.color = Color.Lerp(
            globalLight.color,
            _targetColor,
            Time.deltaTime * 3f
        );
    }
}