using UnityEngine;
using TMPro;

public class DebugResetRuins : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        UpdateStatusText();
    }

    // 🔥 DIPANGGIL SETIAP KALI BUTTON DI KLIK — BISA BOLAK-BALIK
    public void OnClickToggleRuins()
    {
        // Cek status dari Playerpref
        bool isUnlocked = Playerpref.RuinsUnlocked;

        if (isUnlocked)
        {
            // Kalau unlocked → lock
            Playerpref.LockRuins();
            Debug.Log("[DebugResetRuins] Ruins → LOCKED");
        }
        else
        {
            // Kalau locked → unlock
            Playerpref.UnlockRuins();
            Debug.Log("[DebugResetRuins] Ruins → UNLOCKED");
        }

        // Update teks
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        bool isUnlocked = Playerpref.RuinsUnlocked;
        statusText.text = isUnlocked ? "Ruins: UNLOCKED" : "Ruins: LOCKED";
    }

    // Refresh status dari luar (opsional)
    public void RefreshStatus()
    {
        UpdateStatusText();
    }
}