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

    private bool firstCpSet = false;

    public bool IsRespawning => isRespawning;

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
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (player == null && Slopburger.instance != null)
            player = Slopburger.instance.gameObject;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetCheckpoint(Vector3 newCheckpoint, string sceneName, bool firsCpoint)
    {
        currentCheckpoint = newCheckpoint;
        checkpointScene = sceneName;
        hasCheckpoint = true;

        // If player isn't found yet, try now (starting checkpoint fires before Start sometimes)
        FindPlayer();

        // Teleport the player immediately on first checkpoint set
        if (player != null && firsCpoint && !firstCpSet)
            player.transform.position = currentCheckpoint; firstCpSet = true;
    }

    public void PlayerDied()
    {
        if (isRespawning) return;
        isRespawning = true;
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
    }

    private IEnumerator RespawnSequence()
    {
        UIFadeManager fade = FindFirstObjectByType<UIFadeManager>();
        if (fade != null) fade.PlayFadeOut();

        // Disable player entirely before any scene operations
        FindPlayer();
        PlayerController controller = null;
        if (player != null)
        {
            controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;

            // Disable health so death can't re-trigger mid-reload
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null) health.enabled = false;

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false; // stop physics interactions entirely
            }
        }

        yield return new WaitForSeconds(respawnDelay);

        Room targetRoom = RoomManager.Instance?.GetRoomByName(checkpointScene);
        if (targetRoom != null)
        {
            yield return StartCoroutine(RoomManager.Instance.ReloadRoomCoroutine(targetRoom));
        }
        else
        {
            pendingRespawn = true;
            SceneManager.LoadScene(SoloLeveling.playerStatic);
            SceneManager.LoadSceneAsync(checkpointScene, LoadSceneMode.Additive);
            isRespawning = false;
            yield break;
        }

        // Re-enable physics before teleport
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.gravityScale = 1f;
            }

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

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null) health.enabled = true;

            TeleportPlayerToCheckpoint();
            invincibilityTimer = invincibilityTime;

            if (fade != null) fade.PlayFadeIn();
        }

        isRespawning = false;
    }

    public void SpawnAtRoom(Room room)
    {
        // Find the starting checkpoint in the room scene, or fall back to room origin
        Vector3 spawnPos = GetRoomSpawnPosition(room);
        SetCheckpoint(spawnPos, room.sceneName, false); // false = don't use isStartingPoint logic
        FindPlayer();
        if (player != null)
            player.transform.position = spawnPos;
    }

    private Vector3 GetRoomSpawnPosition(Room room)
    {
        // Look for a Checkpoint with isStartingPoint in the loaded scene
        Scene scene = SceneManager.GetSceneByName(room.sceneName);
        if (scene.isLoaded)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var cp = root.GetComponentInChildren<Checkpoint>();
                if (cp != null)
                {
                    return cp.setLocation != null
                        ? cp.setLocation.position
                        : cp.transform.position;
                }
            }
        }
        // No checkpoint found — you could fall back to a Room-defined spawn point here
        Debug.LogWarning($"[CheckpointManager] No checkpoint found in {room.sceneName}, using zero");
        return Vector3.zero;
    }

    public bool IsPlayerInvincible() => invincibilityTimer > 0;
}