using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Pasang di tiap card level (Forest / Mountain / Ruins).
///
/// HIERARCHY per card:
/// [LevelCard_Forest]          ← pasang LevelCard.cs di sini
///   └── Image                 ← portrait/art card
///
/// Tidak ada dim overlay lagi — efek selected/unselected cukup dari scale.
/// </summary>
public class LevelCard : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    // ── Internal ──────────────────────────────────────────────────────────────

    private int _index;
    private LevelSelectManager _manager;
    private RectTransform _rect;
    private Vector3 _originalScale;

    private Coroutine _scaleCoroutine;

    // ── Setup ─────────────────────────────────────────────────────────────────

    /// <summary>Dipanggil LevelSelectManager.Awake()</summary>
    public void Setup(int index, LevelSelectManager manager)
    {
        _index   = index;
        _manager = manager;
        _rect    = GetComponent<RectTransform>();
        _originalScale = _rect.localScale;
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
    /// Update tampilan card sesuai state selected/unselected. Cuma scale, tidak ada dim.
    /// </summary>
    public void SetSelected(bool selected, float selScale, float unselScale, float duration, bool animate)
    {
        float targetScale = selected ? selScale : unselScale;

        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);

        if (animate)
            _scaleCoroutine = StartCoroutine(Co_Scale(targetScale, duration));
        else
            _rect.localScale = _originalScale * targetScale;
    }

    /// <summary>
    /// Sembunyikan card total (scale 0) sebelum reveal sequence mulai.
    /// Dipanggil LevelSelectManager.PrepareTransition() sebelum panel Level Select muncul.
    /// </summary>
    public void PrepareHidden()
    {
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _rect.localScale = Vector3.zero;
    }

    /// <summary>
    /// Animasi "pop-in": card membesar dari scale 0 ke target (selected/unselected).
    /// Dipanggil LevelSelectManager saat reveal sequence (staggered per card).
    /// </summary>
    public void PopIn(bool selected, float selScale, float unselScale, float duration)
    {
        float targetScale = selected ? selScale : unselScale;

        _rect.localScale = Vector3.zero;

        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(Co_Scale(targetScale, duration));
    }

    /// <summary>Reset ke tampilan normal (dipanggil saat keluar Level Select)</summary>
    public void ResetVisual()
    {
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _rect.localScale = _originalScale;
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

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}