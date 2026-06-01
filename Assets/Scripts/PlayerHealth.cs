using UnityEngine;
using TarodevController;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Effects")]
    public ParticleSystem deathEffect;
    public AudioClip deathSound;

    [Header("Camera Shake")]
    public bool enableCameraShake = true;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.15f;

    [Header("Knockback")]
    public bool enableKnockback = true;

    public float voidThreshold = -40f;

    private bool _isDead = false;
    public bool IsDead => _isDead;

    public event Action death;
    public event Action respawn;

    public bool tping;

    public bool hasBomb = false;

    public float iframeTimer = 0f;

    private void Update()
    {
        if (iframeTimer > 0f) iframeTimer -= Time.deltaTime;

        if (transform.position.y < voidThreshold)
        {
            Die();
        }
    }

    private bool IsInvincible => iframeTimer > 0f;

    public void AddIframes(float seconds)
    {
        iframeTimer = Mathf.Max(iframeTimer, seconds);
    }

    public void Die(Vector2? hitDirection = null)
    {
        if (_isDead || tping) return;
        if (IsInvincible) return;
        if (CheckpointManager.Instance != null && CheckpointManager.Instance.IsPlayerInvincible()) return;

        _isDead = true;

        // Stop movement
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        // Disable controller so player can't move
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        // Camera shake
        // Camera shake
        if (enableCameraShake && CameraShake.Instance != null)
        {
            Debug.Log("Camera Shaked Boi");
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
        }


        // Knockback
        if (enableKnockback)
        {
            PlayerKnockback knockback = GetComponent<PlayerKnockback>();
            if (knockback != null)
                knockback.ApplyKnockback(hitDirection ?? Vector2.zero);
        }

        // Death particle
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // Death sound
        PlayerAudio audio = GetComponent<PlayerAudio>();

        if (audio != null)
            audio.PlayDeath();

        // Death animation
        PlayerAnimator anim = GetComponentInChildren<PlayerAnimator>();
        if (anim != null)
            anim.PlayDeath();

        death?.Invoke();

        // Fade out UI+
        UIFadeManager fade = FindFirstObjectByType<UIFadeManager>();
        if (fade != null)
            fade.PlayFadeOut();

        if (CheckpointManager.Instance != null)
            Invoke(nameof(Respawn), 0.8f);
    }

    private void Respawn()
    {
        _isDead = false;
        respawn.Invoke();
        CheckpointManager.Instance.PlayerDied();
    }
}