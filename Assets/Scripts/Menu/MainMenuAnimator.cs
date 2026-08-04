// MenuAnimator.cs
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MenuAnimator : MonoBehaviour
{
    public IEnumerator FadeGroup(CanvasGroup g, float from, float to, float duration)
    {
        g.alpha = from;
        Tween tween = g.DOFade(to, duration).SetEase(Ease.InOutQuad);
        yield return tween.WaitForCompletion();
    }

    public void PrepareSlide(RectTransform rect, Vector2 offset)
    {
        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
        rect.anchoredPosition = rect.anchoredPosition + offset;
    }

    public IEnumerator SlideToHome(RectTransform rect, float duration)
    {
        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        Tween tween = rect.DOAnchorPos(Vector2.zero, duration)
            .SetEase(Ease.OutCubic);
        if (cg != null)
        {
            Tween fadeTween = cg.DOFade(1f, duration * 0.7f)
                .SetEase(Ease.OutQuad)
                .SetDelay(duration * 0.3f);
            yield return tween.WaitForCompletion();
            fadeTween?.Kill();
        }
        else
        {
            yield return tween.WaitForCompletion();
        }
    }

    public IEnumerator SlideInRight(CanvasGroup group, float duration, float canvasW)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 target = rect.anchoredPosition;
        Vector2 start = target + new Vector2(canvasW, 0f);
        rect.anchoredPosition = start;

        Tween tween = rect.DOAnchorPos(target, duration).SetEase(Ease.OutCubic);
        yield return tween.WaitForCompletion();
    }

    public IEnumerator SlideInLeft(CanvasGroup group, float duration, float canvasW)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 target = rect.anchoredPosition;
        Vector2 start = target - new Vector2(canvasW, 0f);
        rect.anchoredPosition = start;

        Tween tween = rect.DOAnchorPos(target, duration).SetEase(Ease.OutCubic);
        yield return tween.WaitForCompletion();
    }

    public IEnumerator SlideOutLeft(CanvasGroup group, float duration, float canvasW)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 origin = rect.anchoredPosition;
        Vector2 end = origin - new Vector2(canvasW, 0f);

        Tween tween = rect.DOAnchorPos(end, duration).SetEase(Ease.InOutQuad);
        yield return tween.WaitForCompletion();
        rect.anchoredPosition = origin;
    }

    public IEnumerator SlideOutRight(CanvasGroup group, float duration, float canvasW)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 origin = rect.anchoredPosition;
        Vector2 end = origin + new Vector2(canvasW, 0f);

        Tween tween = rect.DOAnchorPos(end, duration).SetEase(Ease.InOutQuad);
        yield return tween.WaitForCompletion();
        rect.anchoredPosition = origin;
    }

    public IEnumerator SlideOutUp(CanvasGroup group, float duration, float canvasH)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 origin = rect.anchoredPosition;
        Vector2 end = origin + new Vector2(0f, canvasH);

        Tween tween = rect.DOAnchorPos(end, duration).SetEase(Ease.InOutQuad);
        yield return tween.WaitForCompletion();
        rect.anchoredPosition = origin;
    }

    public IEnumerator SlideInUp(CanvasGroup group, float duration, float canvasH)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 target = rect.anchoredPosition;
        Vector2 start = target - new Vector2(0f, canvasH);
        rect.anchoredPosition = start;

        Tween tween = rect.DOAnchorPos(target, duration).SetEase(Ease.OutCubic);
        yield return tween.WaitForCompletion();
    }

    public IEnumerator SlideOutDown(CanvasGroup group, float duration, float canvasH)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 origin = rect.anchoredPosition;
        Vector2 end = origin - new Vector2(0f, canvasH);

        Tween tween = rect.DOAnchorPos(end, duration).SetEase(Ease.InOutQuad);
        yield return tween.WaitForCompletion();
        rect.anchoredPosition = origin;
    }

    public IEnumerator SlideInDown(CanvasGroup group, float duration, float canvasH)
    {
        RectTransform rect = group.GetComponent<RectTransform>();
        Vector2 target = rect.anchoredPosition;
        Vector2 start = target + new Vector2(0f, canvasH);
        rect.anchoredPosition = start;

        Tween tween = rect.DOAnchorPos(target, duration).SetEase(Ease.OutCubic);
        yield return tween.WaitForCompletion();
    }
}