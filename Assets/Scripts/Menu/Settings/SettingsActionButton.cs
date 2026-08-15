// SettingsActionButton.cs - FULL CODE
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Row generik buat AKSI (bukan setting bernilai)
/// Contoh: "Reset all changes" di kategori Miscellaneous
/// </summary>
public class SettingsActionButton : SettingsRow
{
    public UnityEvent onConfirm;

    public override void Confirm() 
    {
        Debug.Log("[SettingsActionButton] Confirm dipanggil!");
        onConfirm?.Invoke();
    }

    // OnPointerClick dari SettingsRow - override
    public override void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[SettingsActionButton] OnPointerClick dipanggil! Nama: {gameObject.name}");
        // Panggil onSubmit yang sudah di-set oleh SettingsPanelNavigator
        onSubmit?.Invoke();
        // Juga panggil Confirm langsung
        Confirm();
    }
}