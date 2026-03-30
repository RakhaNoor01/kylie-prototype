using UnityEngine;
using TarodevController;
using UnityEngine.Events;

public class PlayerDeath : MonoBehaviour
{
    [Header("Knockback")]
    public bool enableKnockback = true;
    public bool useHitDirection = false;

    public UnityEvent OnDeath;

    private bool _isDead = false;

    public bool IsDead => _isDead;

    public void Die(Vector2 hitSourcePosition = default)
    {
        if (_isDead) return;
        _isDead = true;

        // Stop physics
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        // Knockback
        if (enableKnockback)
        {
            PlayerKnockback knockback = GetComponent<PlayerKnockback>();
            if (knockback != null)
            {
                Vector2 hitDir = useHitDirection
                    ? (transform.position - (Vector3)hitSourcePosition).normalized
                    : Vector2.zero;
                knockback.ApplyKnockback(hitDir);
            }
        }

        // Animation
        PlayerAnimator anim = GetComponentInChildren<PlayerAnimator>();
        if (anim != null) anim.PlayDeath();

        // Disable controller
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        // UI fade
        UIFadeManager fade = FindObjectOfType<UIFadeManager>();
        if (fade != null) fade.PlayFadeOut();

        // Notify anyone listening (DeathZone, enemies, etc.)
        OnDeath?.Invoke();
    }

    public void ResetDeath()
    {
        _isDead = false;
    }
}