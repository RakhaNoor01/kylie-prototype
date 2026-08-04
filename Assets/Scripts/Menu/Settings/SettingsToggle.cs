// SettingsToggle.cs - Tanpa efek hover
using UnityEngine;
using UnityEngine.UI;

public class SettingsToggle : SettingsRow
{
    [Header("Toggle")]
    public bool value = false;
    public Image checkmark;

    private bool _appliedValue;
    private bool _defaultValue;

    protected override void Awake()
    {
        base.Awake();
        _defaultValue = value;
        _appliedValue = value;
        RefreshVisual();
        
        // Pastikan background tetap transparan
        if (background != null)
        {
            background.color = normalBackground;
        }
    }

    public override void Confirm()
    {
        value = !value;
        RefreshVisual();
    }

    // HIGHLIGHT - TIDAK ADA EFEK
    public override void SetHighlighted(bool highlighted)
    {
        if (background != null) background.color = normalBackground;
        if (label != null) label.color = normalText;
    }

    // HOVER - TIDAK ADA EFEK
    public override void SetHovered(bool hovered)
    {
        // Kosong - tidak ada efek hover
    }

    public override bool IsDirty() => value != _appliedValue;

    public override void ApplyChanges() => _appliedValue = value;

    public override void DiscardChanges()
    {
        value = _appliedValue;
        RefreshVisual();
    }

    public override void ResetToDefault()
    {
        value = _defaultValue;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (checkmark != null) checkmark.enabled = value;
    }
}