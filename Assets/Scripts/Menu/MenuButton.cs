// MenuButton.cs - FULL CODE (dengan OnPointerExit)
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Pasang di tiap tombol menu (termasuk popup Yes/No)
/// </summary>
[RequireComponent(typeof(Image))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References (auto-find kalau kosong)")]
    public Image background;
    public TMP_Text label;

    [Header("Warna Normal / Selected (invert)")]
    public Color normalBackground = Color.black;
    public Color normalText = Color.white;
    public Color selectedBackground = Color.white;
    public Color selectedText = Color.black;

    [Header("Outline saat selected")]
    public Color outlineColor = Color.black;
    public Vector2 outlineDistance = new Vector2(2f, -2f);
    public bool showOutlineWhenSelected = true;

    [Header("Tilt saat selected")]
    public float tiltAngle = -3f;
    public float tiltDuration = 0.18f;

    [HideInInspector] public System.Action onHover;
    [HideInInspector] public System.Action onExit; // Tambahan untuk exit hover
    [HideInInspector] public System.Action onSubmit;

    private Outline _outline;
    private RectTransform _rect;
    private Tween _tiltTween;

    private void Awake()
    {
        _rect = (RectTransform)transform;

        if (background == null) background = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TMP_Text>();

        // Setup outline
        _outline = background.GetComponent<Outline>();
        if (_outline == null) _outline = background.gameObject.AddComponent<Outline>();
        _outline.effectColor = outlineColor;
        _outline.effectDistance = outlineDistance;
        _outline.useGraphicAlpha = false;
        _outline.enabled = false;

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (background != null) 
            background.color = selected ? selectedBackground : normalBackground;
        
        if (label != null) 
            label.color = selected ? selectedText : normalText;

        if (_outline != null && showOutlineWhenSelected)
            _outline.enabled = selected;

        float targetZ = selected ? 0f : tiltAngle;
        _tiltTween?.Kill();
        _tiltTween = _rect.DORotate(new Vector3(0f, 0f, targetZ), tiltDuration)
            .SetEase(Ease.OutBack);
    }

    public void OnPointerEnter(PointerEventData eventData) 
    {
        onHover?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData) 
    {
        onExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData) 
    {
        onSubmit?.Invoke();
    }
}