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

    private bool pendingRespawn = false;
    private string checkpointScene = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }
    }

    private void Start()
    {
        // Find the player on initial load — it lives in the Persistent scene with us
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }

    private IEnumerator RestoreGravity(Rigidbody2D rb)
    {
        yield return new WaitForEndOfFrame();
        if (rb != null)
            rb.gravityScale = 3f;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetCheckpoint(Vector3 newCheckpoint, string sceneName)
    {
        currentCheckpoint = newCheckpoint;
        checkpointScene = sceneName;
        hasCheckpoint = true;

        // If player isn't found yet, try now (starting checkpoint fires before Start sometimes)
        FindPlayer();

        // Teleport the player immediately on first checkpoint set
        if (player != null)
            player.transform.position = currentCheckpoint;
    }

    public void PlayerDied()
    {
        if (isRespawning) return;
        StartCoroutine(RespawnSequence());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only used as a fallback for non-RoomManager setups
        FindPlayer();

        if (!pendingRespawn) return;
        if (scene.name != checkpointScene) return;

        pendingRespawn = false;
        TeleportPlayerToCheckpoint();
    }

    private void TeleportPlayerToCheckpoint()
    {
        FindPlayer();
        if (player == null || !hasCheckpoint) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        player.transform.position = currentCheckpoint;
        StartCoroutine(RestoreGravity(rb));
    }

    private IEnumerator RespawnSequence()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        FindPlayer();

        if (player != null)
        {
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
                anim.ResetDeath();
            }

            UIFadeManager fade = FindFirstObjectByType<UIFadeManager>();
            if (fade != null) fade.PlayFadeIn();

            invincibilityTimer = invincibilityTime;
        }

        // Make sure the right room is loaded
        Room targetRoom = RoomManager.Instance?.GetRoomByName(checkpointScene);
        if (targetRoom != null)
        {
            RoomManager.Instance.LoadRoom(targetRoom);
            // Wait for RoomManager's coroutine to finish loading scenes
            yield return StartCoroutine(WaitForSceneLoaded(checkpointScene));
        }
        else
        {
            pendingRespawn = true; // let OnSceneLoaded handle teleport
            SceneManager.LoadScene(checkpointScene);
            isRespawning = false;
            yield break;
        }

        // Scene is loaded — teleport directly
        TeleportPlayerToCheckpoint();
        isRespawning = false;
    }

    private IEnumerator WaitForSceneLoaded(string sceneName)
    {
        // If already loaded, continue immediately
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
            yield break;

        bool loaded = false;
        void OnLoaded(Scene s, LoadSceneMode m) { if (s.name == sceneName) loaded = true; }
        SceneManager.sceneLoaded += OnLoaded;
        yield return new WaitUntil(() => loaded);
        SceneManager.sceneLoaded -= OnLoaded;
    }

    public bool IsPlayerInvincible() => invincibilityTimer > 0;
}