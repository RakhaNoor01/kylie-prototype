using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BlizzardHandler : MonoBehaviour
{
    public float heat = 10;
    public float heatLoss = 1;
    public float heatRegen = 5;
    [Header("Visuals")]
    public GameObject blizzardStuff;
    public float blizzardFadeTime = 3;
    public Image blizzardVig;
    public Image blizzardFG;
    public SpriteRenderer blizzardBG;

    public bool isBlizzard = false;
    public bool cozy = true;
    public float heatReal;

    private int heatSourceCount = 0;

    private void Start()
    {
        heatReal = heat;
        blizzardStuff.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (isBlizzard)
        {
            if (cozy && heatReal < 10)
            {
                heatReal += heatRegen * Time.deltaTime;
            }
            else if (!cozy && heatReal > 0)
            {
                heatReal -= heatLoss * Time.deltaTime;
            }
            heatReal = Mathf.Clamp(heatReal, 0, heat);
        }

        float t = heatReal / heat;
        blizzardVig.color = Color.Lerp(Color.white, Color.black, t);

        var bfg = blizzardFG.color;
        float tfg = t > 0.5f ? 1f : Mathf.InverseLerp(0f, 0.5f, t);
        blizzardFG.color = new Color(bfg.r, bfg.g, bfg.b, Mathf.Lerp(1f, 0f, tfg));

        if (heatReal <= 0)
        {
            GetComponent<PlayerHealth>()?.Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var gobj = collision.gameObject.GetComponent<ExtraTags>();
        if (gobj == null) return;

        if (gobj.extraTag == ExtraTags.ExtraTag.heatSource)
        {
            heatSourceCount++;
            cozy = true;
        }
        if (gobj.extraTag == ExtraTags.ExtraTag.blizzardStart)
        {
            if (isBlizzard) return;
            heatReal = heat;
            blizzardStuff.SetActive(true);

            foreach (var img in blizzardStuff.GetComponentsInChildren<Image>())
            {
                var ogAlpha = img.color.a;
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                if (img == blizzardFG) continue;
                img.DOFade(ogAlpha, blizzardFadeTime)
                    .OnComplete(() =>
                    {
                        isBlizzard = true;
                        cozy = heatSourceCount > 0;
                    });
            }

            blizzardBG.color = new Color(blizzardBG.color.r, blizzardBG.color.g, blizzardBG.color.b, 0f);
            blizzardBG.DOFade(1f, blizzardFadeTime);
        }
        if (gobj.extraTag == ExtraTags.ExtraTag.blizzardEnd)
        {
            if (!isBlizzard) return;
            isBlizzard = false;

            foreach (var img in blizzardStuff.GetComponentsInChildren<Image>())
            {
                img.DOFade(0f, blizzardFadeTime)
                    .OnComplete(() => blizzardStuff.SetActive(false));
            }

            blizzardBG.DOFade(0f, blizzardFadeTime);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var gobj = collision.gameObject.GetComponent<ExtraTags>();
        if (gobj == null) return;

        if (gobj.extraTag == ExtraTags.ExtraTag.heatSource)
        {
            heatSourceCount = Mathf.Max(0, heatSourceCount - 1);
            cozy = heatSourceCount > 0;
        }
    }
}