using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuAnimator : MonoBehaviour
{
    private float EaseOut(float t)   => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInOut(float t) => t < 0.5f ? 2*t*t : 1f - Mathf.Pow(-2*t+2, 2f)/2f;

    private Dictionary<RectTransform, Vector2> _homePos = new();

    // ── Fade ──────────────────────────────────────────────────────────────────

    public IEnumerator FadeGroup(CanvasGroup g, float from, float to, float duration)
    {
        g.alpha = from;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, EaseInOut(Mathf.Clamp01(e / duration)));
            yield return null;
        }
        g.alpha = to;
    }

    // ── Individual Slide (logo & tombol) ──────────────────────────────────────

    public void PrepareSlide(RectTransform rect, float offsetX = -350f)
    {
        _homePos[rect] = rect.anchoredPosition;
        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
        rect.anchoredPosition = _homePos[rect] + new Vector2(offsetX, 0f);
    }

    public IEnumerator SlideToHome(RectTransform rect, float duration)
    {
        if (!_homePos.TryGetValue(rect, out Vector2 targetPos))
        {
            Debug.LogWarning($"[MenuAnimator] {rect.name} belum PrepareSlide.");
            yield break;
        }

        Vector2 startPos = rect.anchoredPosition;
        CanvasGroup cg   = rect.GetComponent<CanvasGroup>();

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = EaseOut(Mathf.Clamp01(e / duration));
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            if (cg != null) cg.alpha = Mathf.Clamp01(t * 1.5f);
            yield return null;
        }

        rect.anchoredPosition = targetPos;
        if (cg != null) cg.alpha = 1f;
    }

    // ── Panel Slide ───────────────────────────────────────────────────────────

    public IEnumerator SlideInRight(CanvasGroup group, float duration, float canvasW)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 target = rect.anchoredPosition;
        Vector2 start  = target + new Vector2(canvasW, 0f);
        rect.anchoredPosition = start;

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(start, target, EaseOut(Mathf.Clamp01(e / duration)));
            yield return null;
        }
        rect.anchoredPosition = target;
    }

    public IEnumerator SlideInLeft(CanvasGroup group, float duration, float canvasW)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 target = rect.anchoredPosition;
        Vector2 start  = target - new Vector2(canvasW, 0f);
        rect.anchoredPosition = start;

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(start, target, EaseOut(Mathf.Clamp01(e / duration)));
            yield return null;
        }
        rect.anchoredPosition = target;
    }

    public IEnumerator SlideOutLeft(CanvasGroup group, float duration, float canvasW)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 origin = rect.anchoredPosition;
        Vector2 end    = origin - new Vector2(canvasW, 0f);

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(origin, end, EaseInOut(Mathf.Clamp01(e / duration)));
            yield return null;
        }
        rect.anchoredPosition = origin;
    }

    public IEnumerator SlideOutRight(CanvasGroup group, float duration, float canvasW)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 origin = rect.anchoredPosition;
        Vector2 end    = origin + new Vector2(canvasW, 0f);

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(origin, end, EaseInOut(Mathf.Clamp01(e / duration)));
            yield return null;
        }
        rect.anchoredPosition = origin;
    }

    // ── Circle Wipe ───────────────────────────────────────────────────────────

    /// <summary>
    /// Circle wipe in: lingkaran scale 0 → menutupi seluruh layar.
    /// circleRect harus berupa Image lingkaran putih di tengah layar.
    /// Multiplier *3 supaya ujung layar tertutup di semua resolusi.
    /// </summary>
    public IEnumerator CircleWipeIn(RectTransform circleRect, float duration)
    {
        circleRect.gameObject.SetActive(true);
        circleRect.localScale = Vector3.zero;

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = EaseInOut(Mathf.Clamp01(e / duration));
            circleRect.localScale = Vector3.one * t * 3f;
            yield return null;
        }

        circleRect.localScale = Vector3.one * 3f;
    }

    /// <summary>Circle wipe out: setelah scene baru load, circle mengecil.</summary>
    public IEnumerator CircleWipeOut(RectTransform circleRect, float duration)
    {
        circleRect.localScale = Vector3.one * 3f;

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = EaseInOut(Mathf.Clamp01(e / duration));
            circleRect.localScale = Vector3.one * (1f - t) * 3f;
            yield return null;
        }

        circleRect.localScale = Vector3.zero;
        circleRect.gameObject.SetActive(false);
    }
}