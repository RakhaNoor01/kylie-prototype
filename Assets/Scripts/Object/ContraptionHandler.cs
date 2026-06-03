using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ContraptionHandler : MonoBehaviour
{
    public float lightRegen = 10;
    public float lightDecay = 1;
    public float maxLightCount = 20;
    public float maxLight = 1;
    public float minLight = 0.2f;
    public Light2D sunGodRa;

    private float realLightCount;
    private bool inRadius;

    public float LightCount => realLightCount;
    public bool InRadius => inRadius;

    public static ContraptionHandler Instance;
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        realLightCount = 0;
    }

    private void Update()
    {
        if (inRadius)
        {
            realLightCount = Mathf.Min(realLightCount + lightRegen * Time.deltaTime, maxLightCount);
        }
        else
        {
            realLightCount = Mathf.Max(realLightCount - lightDecay * Time.deltaTime, 0);
        }

        sunGodRa.intensity = Mathf.Lerp(minLight, maxLight, realLightCount / maxLightCount);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var gobj = collision.gameObject.GetComponent<ExtraTags>();
        if (gobj == null) return;
        if (gobj.extraTag == ExtraTags.ExtraTag.contraption)
        {
            inRadius = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var gobj = collision.gameObject.GetComponent<ExtraTags>();
        if (gobj == null) return;
        if (gobj.extraTag == ExtraTags.ExtraTag.contraption)
        {
            inRadius = false;
        }
    }
}
