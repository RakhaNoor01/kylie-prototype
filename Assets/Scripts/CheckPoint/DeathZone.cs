using UnityEngine;
using TarodevController;
using UnityEngine.SceneManagement;
public class DeathZone : MonoBehaviour
{
    [Header("Effects")]
    public ParticleSystem deathEffect;
    public AudioClip deathSound;

    [Header("Camera Shake")]
    public bool enableCameraShake = true;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.15f;

    [Header("Player Knockback")]
    public bool enableKnockback = true;
    public bool useHitDirection = false;

    private bool _playerDead = false;
    private GameObject _player;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_playerDead) return;

        if (other.CompareTag("Player"))
        {
            if (CheckpointManager.Instance != null && CheckpointManager.Instance.IsPlayerInvincible())
                return;

            _playerDead = true;
            HandleDeath(other.gameObject);
        }
    }

    private void HandleDeath(GameObject player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        if (enableCameraShake && CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);

        if (enableKnockback)
        {
            PlayerKnockback knockback = player.GetComponent<PlayerKnockback>();
            if (knockback != null)
            {
                Vector2 hitDir = useHitDirection ? (player.transform.position - transform.position).normalized : Vector2.zero;
                knockback.ApplyKnockback(hitDir);
            }
        }

        if (deathEffect != null)
            Instantiate(deathEffect, player.transform.position, Quaternion.identity);

        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, Camera.main.transform.position);

        PlayerAnimator anim = player.GetComponentInChildren<PlayerAnimator>();
        if (anim != null)
            anim.PlayDeath();
        UIFadeManager fade = FindObjectOfType<UIFadeManager>();
        if (fade != null) fade.PlayFadeOut();

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        if (CheckpointManager.Instance != null)
            Invoke(nameof(RespawnPlayer), 0.8f);
    }

    private void RespawnPlayer()
    {
        _playerDead = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); //relaods the whole scene
    }

    private void Update()
    {
        if (!_playerDead && _player != null && _player.transform.position.y < -20f)
        {
            if (CheckpointManager.Instance != null && !CheckpointManager.Instance.IsPlayerInvincible())
            {
                _playerDead = true;
                HandleDeath(_player);
            }
        }
    }
}