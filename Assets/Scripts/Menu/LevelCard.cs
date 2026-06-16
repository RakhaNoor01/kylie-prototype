using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pasang di tiap card level (Forest / Mountain).
///
/// HIERARCHY per card:
/// [LevelCard_Forest]          ← pasang LevelCard.cs di sini
///   ├── Image                 ← portrait/art card (bisa langsung di root)
///   └── DimOverlay            ← Image, color hitam, alpha 0, raycastTarget OFF
/// </summary>
public class LevelCard : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("Image overlay hitam untuk efek menggelap saat tidak dipilih")]
    public Image dimOverlay;

    // ── Internal ──────────────────────────────────────────────────────────────

    private int _index;
    private LevelSelectManager _manager;
    private RectTransform _rect;
    private Vector3 _originalScale;

    private Coroutine _scaleCoroutine;
    private Coroutine _dimCoroutine;

    // ── Setup ─────────────────────────────────────────────────────────────────

    /// <summary>Dipanggil LevelSelectManager.Awake()</summary>
    public void Setup(int index, LevelSelectManager manager)
    {
        _index   = index;
        _manager = manager;
        _rect    = GetComponent<RectTransform>();
        _originalScale = _rect.localScale;

        if (dimOverlay != null)
        {
            Color c = dimOverlay.color;
            c.a = 0f;
            dimOverlay.color = c;
            dimOverlay.raycastTarget = false; // jangan block input ke card
        }
    }

    // ── Pointer Events ────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        _manager.OnCardHover(_index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _manager.OnCardClick(_index);
    }

    // ── Visual ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update tampilan card sesuai state selected/unselected.
    /// Parameter nama cocok dengan panggilan di LevelSelectManager.
    /// </summary>
    public void SetSelected(bool selected, float selScale, float unselScale,
                            float dimAlpha, float duration, bool animate)
    {
        float targetScale = selected ? selScale : unselScale;
        float targetDim   = selected ? 0f : dimAlpha;

        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        if (_dimCoroutine   != null) StopCoroutine(_dimCoroutine);

        if (animate)
        {
            _scaleCoroutine = StartCoroutine(Co_Scale(targetScale, duration));
            if (dimOverlay != null)
                _dimCoroutine = StartCoroutine(Co_Dim(targetDim, duration));
        }
        else
        {
            _rect.localScale = _originalScale * targetScale;
            ApplyDim(targetDim);
        }
    }

    /// <summary>Reset ke tampilan normal (dipanggil saat keluar Level Select)</summary>
    public void ResetVisual()
    {
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        if (_dimCoroutine   != null) StopCoroutine(_dimCoroutine);

        _rect.localScale = _originalScale;
        ApplyDim(0f);
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator Co_Scale(float targetScale, float duration)
    {
        Vector3 startScale = _rect.localScale;
        Vector3 endScale   = _originalScale * targetScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            _rect.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        _rect.localScale = endScale;
    }

    private IEnumerator Co_Dim(float targetAlpha, float duration)
    {
        float startAlpha = dimOverlay.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyDim(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        ApplyDim(targetAlpha);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyDim(float alpha)
    {
        if (dimOverlay == null) return;
        Color c = dimOverlay.color;
        c.a = alpha;
        dimOverlay.color = c;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}