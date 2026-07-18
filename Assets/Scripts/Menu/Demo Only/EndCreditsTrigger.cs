using UnityEngine;

/// <summary>
/// Pasang di GameObject trigger custom di room terakhir.
/// Butuh Collider2D dengan "Is Trigger" = ON.
///
/// SETUP:
/// [EndTrigger]              ← GameObject di room terakhir
///   ├── Collider2D (Is Trigger: ON)
///   └── EndCreditsTrigger.cs (script ini)
///
/// Drag EndCreditsController (yang ada di scene/persistent) ke field di bawah.
/// </summary>
public class EndCreditsTrigger : MonoBehaviour
{
    [Tooltip("Drag GameObject yang punya EndCreditsController.cs")]
    public EndCreditsController creditsController;

    [Tooltip("Tag GameObject Player, dipakai untuk filter trigger")]
    public string playerTag = "Player";

    private bool _triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag(playerTag)) return;

        _triggered = true;

        if (creditsController != null)
            creditsController.StartCredits();
        else
            Debug.LogError("[EndCreditsTrigger] creditsController belum di-assign!");
    }
}