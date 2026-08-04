// LevelCard.cs
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Pasang di tiap card level (Forest / Mountain / Ruins).
/// </summary>
public class LevelCard : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private int _index;
    private LevelSelectManager _manager;
    private RectTransform _rect;
    private Vector3 _originalScale;
    private Tween _tween;

    public void Setup(int index, LevelSelectManager manager)
    {
        _index = index;
        _manager = manager;
        _rect = GetComponent<RectTransform>();
        _originalScale = _rect.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _manager.OnCardHover(_index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _manager.OnCardClick(_index);
    }

    public void SetSelected(bool selected, float selScale, float unselScale, float duration, bool animate)
    {
        float targetScale = selected ? selScale : unselScale;
        _tween?.Kill();

        if (animate)
        {
            _tween = _rect.DOScale(_originalScale * targetScale, duration)
                .SetEase(Ease.OutCubic);
        }
        else
        {
            _rect.localScale = _originalScale * targetScale;
        }
    }

    public void PrepareHidden()
    {
        _tween?.Kill();
        _rect.localScale = Vector3.zero;
    }

    public void PopIn(bool selected, float selScale, float unselScale, float duration)
    {
        float targetScale = selected ? selScale : unselScale;
        _tween?.Kill();
        _rect.localScale = Vector3.zero;
        _tween = _rect.DOScale(_originalScale * targetScale, duration)
            .SetEase(Ease.OutCubic);
    }

    public void ResetVisual()
    {
        _tween?.Kill();
        _rect.localScale = _originalScale;
    }
}