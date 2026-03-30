using UnityEngine;
using System.Collections;
using TarodevController;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Respawn Settings")]
    public float respawnDelay = 0.5f;
    public float invincibilityTime = 1f;

    private Vector3 currentCheckpoint;
    private GameObject player;
    private float invincibilityTimer = 0f;
    private bool isRespawning = false;
    private bool hasCheckpoint = false;
    public bool HasCheckpoint => hasCheckpoint;
    public bool IsCurrentCheckpoint(Vector3 pos) =>
        hasCheckpoint && Vector3.Distance(currentCheckpoint, pos) < 0.1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // NEW: subscribe to scene load event
        }
        else Destroy(gameObject);
    }

    // NEW: called after scene fully finishes loading
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (hasCheckpoint)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
            }

            player.transform.position = currentCheckpoint; // currentCheckpoint is already set correctly by SetCheckpoint()

            StartCoroutine(RestoreGravity(rb));
        }
    }

    private IEnumerator RestoreGravity(Rigidbody2D rb)
    {
        yield return new WaitForEndOfFrame(); // wait one frame for position to stick
        if (rb != null)
            rb.gravityScale = 3f; // set this to whatever your normal gravity scale is
    }

    // Also add OnDestroy to unsubscribe cleanly
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
            FlashInvincible();
        }
        else ResetSpriteColor();
    }

    public void SetCheckpoint(Vector3 newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
        hasCheckpoint = true; // NEW: mark that a real checkpoint exists
    }

    public void PlayerDied()
    {
        if (isRespawning) return;
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay); // wait before respawn

        if (player != null)
        {
            player.transform.position = currentCheckpoint;

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
            }

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ResetWallStates();
                controller.ForceGroundedRespawn();
                controller.enabled = true;
            }


            PlayerAnimator anim = player.GetComponentInChildren<PlayerAnimator>();
            if (anim != null)
            {
                anim.SetController(controller);
                anim.ResetDeath(); // clear death and force Idle
            }
            UIFadeManager fade = FindObjectOfType<UIFadeManager>();
            if (fade != null) fade.PlayFadeIn();

            invincibilityTimer = invincibilityTime;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        isRespawning = false;
    }

    private void FlashInvincible()
    {
        if (player == null) return;
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float alpha = Mathf.PingPong(Time.time * 10f, 1f);
            sr.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private void ResetSpriteColor()
    {
        if (player == null) return;
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }

    public bool IsPlayerInvincible() => invincibilityTimer > 0;
}
