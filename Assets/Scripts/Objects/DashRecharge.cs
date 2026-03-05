using System;
using UnityEngine;
using TarodevController;

[RequireComponent(typeof(Collider2D))]
public class DashRecharge : MonoBehaviour
{
    // optional event that other systems can listen for (particles, sound, etc.)
    public event Action<DashRecharge> Collected;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // try interface first for flexibility
        var player = other.GetComponent<IPlayerController>();
        if (player == null)
        {
            // fallback to concrete type
            player = other.GetComponent<PlayerController>();
        }

        if (player != null)
        {
            player.RechargeDash();
            Collected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
