using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Slopburger : MonoBehaviour
{
    public GameObject torc;
    private bool hasTorj;

    public bool TheOrch => hasTorj;

    public void Start()
    {
        TsFunction(false);
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
        Shader.SetGlobalVector("_playerPos", gameObject.transform.position);
    }
}
