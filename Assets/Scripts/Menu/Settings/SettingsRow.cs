// SettingsRow.cs - FULL CODE
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public abstract class SettingsRow : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("References")]
    public Image background;
    public TMP_Text label;

    [Header("Warna Normal / Highlighted")]
    public Color normalBackground = new Color(0f, 0f, 0f, 0f);
    public Color normalText = Color.white;
    public Color highlightBackground = Color.white;
    public Color highlightText = Color.black;

    [HideInInspector] public System.Action onHover;
    [HideInInspector] public System.Action onSubmit;

    protected virtual void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        SetHighlighted(false);
    }

    public virtual void SetHighlighted(bool highlighted)
    {
        if (background != null) background.color = normalBackground;
        if (label != null) label.color = normalText;
    }

    public virtual void SetHovered(bool hovered) { }
    public virtual void Confirm() { }
    public virtual void Increment() { }
    public virtual void Decrement() { }
    public virtual bool IsDirty() => false;
    public virtual void ApplyChanges() { }
    public virtual void DiscardChanges() { }
    public virtual void ResetToDefault() { }

    public virtual void OnPointerEnter(PointerEventData eventData) 
    {
        Debug.Log($"[SettingsRow] OnPointerEnter: {gameObject.name}");
        onHover?.Invoke();
    }

    public virtual void OnPointerClick(PointerEventData eventData) 
    {
        Debug.Log($"[SettingsRow] OnPointerClick: {gameObject.name}");
        onSubmit?.Invoke();
    }
}