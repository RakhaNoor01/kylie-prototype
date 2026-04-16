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
        if (player != null && firsCpoint)
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
    }

    private IEnumerator RespawnSequence()
    {
        isRespawning = true;

        UIFadeManager fade = FindFirstObjectByType<UIFadeManager>();
        if (fade != null) fade.PlayFadeOut(); // fade out before anything resets

        yield return new WaitForSeconds(respawnDelay);

        FindPlayer();

        if (player != null)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.enabled = false; // disable during reload
            }
        }

        // Reload the room scenes
        Room targetRoom = RoomManager.Instance?.GetRoomByName(checkpointScene);
        if (targetRoom != null)
        {
            yield return StartCoroutine(RoomManager.Instance.ReloadRoomCoroutine(targetRoom));
        }
        else
        {
            // Fallback for non-RoomManager setups
            pendingRespawn = true;
            SceneManager.LoadScene(SoloLeveling.playerStatic);
            SceneManager.LoadSceneAsync(checkpointScene, LoadSceneMode.Additive);
            isRespawning = false;
            yield break;
        }

        // Scenes are fresh — now reset player
        if (player != null)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ResetWallStates();
                controller.ForceGroundedRespawn();
                controller.enabled = true;

                Boomerang boomerang = player.GetComponentInChildren<Boomerang>();
                if (boomerang != null)
                    boomerang.ResetBoomerang();
            }

            PlayerAnimator anim = player.GetComponentInChildren<PlayerAnimator>();
            if (anim != null)
            {
                anim.SetController(controller);
                anim.ResetDeath();
            }

            TeleportPlayerToCheckpoint();
            invincibilityTimer = invincibilityTime;

            if (fade != null) fade.PlayFadeIn();
        }

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