// SettingsCycleOption.cs - Tanpa efek hover
using UnityEngine;
using TMPro;

public class SettingsCycleOption : SettingsRow
{
    [Header("Cycle Options")]
    public string[] options = { "Off", "30", "60", "120", "Unlimited" };
    public int currentIndex = 0;
    public TMP_Text valueLabel;

    private int _appliedIndex;
    private int _defaultIndex;

    protected override void Awake()
    {
        base.Awake();
        _defaultIndex = currentIndex;
        _appliedIndex = currentIndex;
        RefreshVisual();
        
        // Pastikan background tetap transparan
        if (background != null)
        {
            background.color = normalBackground;
        }
    }

    public override void Increment()
    {
        if (options.Length == 0) return;
        currentIndex = (currentIndex + 1) % options.Length;
        RefreshVisual();
    }

    public override void Decrement()
    {
        if (options.Length == 0) return;
        currentIndex = (currentIndex - 1 + options.Length) % options.Length;
        RefreshVisual();
    }

    public override void Confirm() => Increment();

    // HIGHLIGHT - TIDAK ADA EFEK
    public override void SetHighlighted(bool highlighted)
    {
        if (background != null) background.color = normalBackground;
        if (label != null) label.color = normalText;
        if (valueLabel != null) valueLabel.color = normalText;
    }

    // HOVER - TIDAK ADA EFEK
    public override void SetHovered(bool hovered)
    {
        // Kosong - tidak ada efek hover
    }

    public override bool IsDirty() => currentIndex != _appliedIndex;

    public override void ApplyChanges() => _appliedIndex = currentIndex;

    public override void DiscardChanges()
    {
        currentIndex = _appliedIndex;
        RefreshVisual();
    }

    public override void ResetToDefault()
    {
        currentIndex = _defaultIndex;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (valueLabel != null && options.Length > 0)
            valueLabel.text = options[Mathf.Clamp(currentIndex, 0, options.Length - 1)];
    }
}