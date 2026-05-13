using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class BlizzardHandler : MonoBehaviour
{
    public float heat = 10;
    public float heatLoss = 1;
    public float heatRegen = 5;

    [Header("Visuals")]
    public GameObject blizzardStuff; //collection of blizzard sprite overlays
    public float blizzardFadeTime = 3;
    public SpriteRenderer blizzardVig;
    public SpriteRenderer blizzardFG;

    public bool isBlizzard = false;
    public bool cozy = true;
    public float heatReal;

    private void Start()
    {
        heatReal = heat;
        blizzardStuff.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!isBlizzard) return;

        if (cozy && heatReal < 10)
        {
            heatReal += heatRegen * Time.deltaTime;
        }
        else if (!cozy && heatReal > 0)
        {
            heatReal -= heatLoss * Time.deltaTime;
        }

        heatReal = Mathf.Clamp(heatReal, 0, heat);

        float t = heatReal / heat;
        blizzardVig.color = Color.Lerp(Color.white, Color.black, t);
        var bfg = blizzardFG.color;
        blizzardFG.color = Color.Lerp(new Color(bfg.r, bfg.g, bfg.b, 1), new Color(bfg.r, bfg.g, bfg.b, 0f), t);

        if (heatReal <= 0)
        {
            var gwa = GetComponent<PlayerHealth>();
            gwa.Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var gobj = collision.gameObject.GetComponent<ExtraTags>();
        if (gobj.extraTag == ExtraTags.ExtraTag.heatSource)
        {
            cozy = true;
        }

        if (gobj.extraTag == ExtraTags.ExtraTag.blizzardStart)
        {
            if (isBlizzard) return;
            blizzardStuff.SetActive(true);
            foreach (var sr in blizzardStuff.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                if (sr == blizzardFG) continue;
                sr.DOFade(1f, blizzardFadeTime)
                    .OnComplete(() => { isBlizzard = true; cozy = false; });
            }
        }
        if (gobj.extraTag == ExtraTags.ExtraTag.blizzardEnd)
        {
            if (!isBlizzard) return;
            isBlizzard = false;
            foreach (var sr in blizzardStuff.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.DOFade(0f, blizzardFadeTime)
                  .OnComplete(() => blizzardStuff.SetActive(false));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var gobj = collision.gameObject.GetComponent<ExtraTags>();
        if (gobj.extraTag == ExtraTags.ExtraTag.heatSource)
        {
            cozy = false;
        }
    }
}
