using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

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

    private bool isBlizzard = false;
    private bool started = false;
    private bool cozy = true;
    public float heatReal;
    private int heatSourceCount = 0;
    private static bool checkpointAfterBlizzard = false;
    private PlayerHealth health;

    private void Start()
    {
        heatReal = heat;
        blizzardStuff.SetActive(false);
        health = GetComponent<PlayerHealth>();
        if (checkpointAfterBlizzard) InstantBlizzard();
    }

    private void FixedUpdate()
    {
        if (!started) return;
        if (isBlizzard)
        {
            if (cozy && heatReal < heat)
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
        float tfg = t > 0.75f ? 1f : Mathf.InverseLerp(0f, 0.25f, t);
        blizzardFG.color = new Color(bfg.r, bfg.g, bfg.b, Mathf.Lerp(1f, 0f, tfg));

        if (heatReal <= 0)
        {
            health.Die();
        }
    }

    private void InstantBlizzard()
    {
        if (!checkpointAfterBlizzard) return;

        started = true;
        isBlizzard = true;
        cozy = heatSourceCount > 0;
        blizzardStuff.SetActive(true);

        blizzardFG.color = new Color(blizzardFG.color.r, blizzardFG.color.g, blizzardFG.color.b, 0f);
        blizzardBG.color = new Color(blizzardBG.color.r, blizzardBG.color.g, blizzardBG.color.b, 1f);
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

        if (gobj.extraTag == ExtraTags.ExtraTag.blizzardContinue)
        {
            checkpointAfterBlizzard = true;
            if (!isBlizzard && !started) InstantBlizzard();
        }

        if (gobj.extraTag == ExtraTags.ExtraTag.blizzardStart)
        {
            if (isBlizzard || started) return;
            heatReal = heat;
            blizzardStuff.SetActive(true);
            started = true;

            foreach (var img in blizzardStuff.GetComponentsInChildren<Image>())
            {
                var ogAlpha = img.color.a;
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                if (img == blizzardFG) continue;
                img.DOFade(ogAlpha, blizzardFadeTime)
                    .SetEase(Ease.InQuint)
                    .OnComplete(() =>
                    {
                        isBlizzard = true;
                        cozy = heatSourceCount > 0;
                    });
            }

            blizzardBG.color = new Color(blizzardBG.color.r, blizzardBG.color.g, blizzardBG.color.b, 0f);
            blizzardBG.DOFade(1f, blizzardFadeTime);

            shrineSeq();
        }

        if (gobj.extraTag == ExtraTags.ExtraTag.blizzardEnd)
        {
            if (!isBlizzard) return;
            isBlizzard = false;
            checkpointAfterBlizzard = false;

            foreach (var img in blizzardStuff.GetComponentsInChildren<Image>())
            {
                img.DOFade(0f, blizzardFadeTime)
                    .OnComplete(() => blizzardStuff.SetActive(false));
            }

            blizzardBG.DOFade(0f, blizzardFadeTime);
        }
    }

    private void shrineSeq()
    {
        var shrine = FindObjectsByType<ExtraTags>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.extraTag == ExtraTags.ExtraTag.shrine);
        if (shrine == null) return;

        var anim = shrine.GetComponent<Animator>();
        if (anim == null) return;

        anim.CrossFadeInFixedTime("evil_on", 0.1f);

        DOVirtual.DelayedCall(1f, () => anim.CrossFadeInFixedTime("evil_idle", 0.25f));
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