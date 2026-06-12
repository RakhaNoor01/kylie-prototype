using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Slopburger : MonoBehaviour
{
    // this class is used for stuff thats kinda significant but not significant enough to warrant their own scripts

    public Boomerang rang;
    public GameObject torc;
    private bool hasTorj;

    public bool TheOrch => hasTorj;
    public static Slopburger instance;

    private void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        TsFunction(hasTorj);
        var plealth = GetComponent<PlayerHealth>();
        plealth.death += Whatever;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var gobj = collision.gameObject.GetComponent<ExtraTags>();
        if (gobj == null) return;

        if (gobj.extraTag == ExtraTags.ExtraTag.walterfall && hasTorj)
        {
            TsFunction(false);
        }
    }

    private void Whatever()
    {
        TsFunction(false);
    }

    public void TsFunction(bool state)
    {
        hasTorj = state;
        torc.SetActive(state);
    }

    private void Update()
    {
        if (!hasTorj)
        {
            Shader.SetGlobalVector("_playerPos", new Vector2(-99, -99));
            Shader.SetGlobalVector("_rangPos", new Vector2(-99, -99));
            return;
        }

        Shader.SetGlobalVector("_playerPos", gameObject.transform.position);

        if (rang.IsThrown)
        {
            Shader.SetGlobalVector("_rangPos", rang.gameObject.transform.position);
        }
    }

    [ContextMenu("Temp Data")]
    public void hi()
    {
        TempData.GetTempData();
    }
}
