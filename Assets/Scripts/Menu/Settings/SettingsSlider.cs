// SettingsSlider.cs - Tanpa efek hover sama sekali
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Slider volume (Master/Music/SFX)
/// TANPA EFEK HOVER - background tetap transparan, handle tetap putih
/// </summary>
public class SettingsSlider : SettingsRow
{
    [Header("Slider")]
    public Slider slider;
    public TMP_Text valueLabel;
    [Range(0.01f, 0.5f)]
    public float keyboardStep = 0.01f;

    [Header("Handle")]
    public Image handle;
    public Color handleNormalColor = Color.white;

    private float _appliedValue;
    private float _defaultValue;

    protected override void Awake()
    {
        base.Awake();

        if (slider != null)
        {
            Navigation nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;
            slider.onValueChanged.AddListener(OnSliderChanged);
        }

        _defaultValue = slider != null ? slider.value : 0f;
        _appliedValue = _defaultValue;
        RefreshValueLabel();
        
        // Pastikan background tetap transparan
        if (background != null)
        {
            background.color = normalBackground;
        }
    }

    private void OnDestroy()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    public override void Increment()
    {
        if (slider != null) slider.value = Mathf.Clamp01(slider.value + keyboardStep);
    }

    public override void Decrement()
    {
        if (slider != null) slider.value = Mathf.Clamp01(slider.value - keyboardStep);
    }

    // HIGHLIGHT - TIDAK ADA EFEK SAMA SEKALI
    public override void SetHighlighted(bool highlighted)
    {
        // TIDAK ADA EFEK - background tetap transparan, label tetap putih
        // Handle tetap putih
        if (background != null) background.color = normalBackground;
        if (label != null) label.color = normalText;
        if (handle != null) handle.color = handleNormalColor;
        if (valueLabel != null) valueLabel.color = normalText;
    }

    // HOVER - TIDAK ADA EFEK SAMA SEKALI
    public override void SetHovered(bool hovered)
    {
        // TIDAK ADA EFEK - kosong total
    }

    public override bool IsDirty() => slider != null && !Mathf.Approximately(slider.value, _appliedValue);

    public override void ApplyChanges()
    {
        if (slider != null) _appliedValue = slider.value;
    }

    public override void DiscardChanges()
    {
        if (slider != null) slider.value = _appliedValue;
    }

    public override void ResetToDefault()
    {
        if (slider != null) slider.value = _defaultValue;
    }

    private void OnSliderChanged(float _) => RefreshValueLabel();

    private void RefreshValueLabel()
    {
        if (valueLabel != null && slider != null)
            valueLabel.text = Mathf.RoundToInt(slider.value * 100f) + "%";
    }
}