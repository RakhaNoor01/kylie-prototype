using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class Playerpref : MonoBehaviour
{
    public TMP_Text statusText;
    private const string SavedRoomPrefsKey = "SavedRoomSceneName";
    private const string SavedSoloLevelingSceneKey = "SavedSoloLevelingSceneName";

    public static bool HasSavedCheckpoint => PlayerPrefs.HasKey(SavedRoomPrefsKey);
    public static string GetSavedRoomName => PlayerPrefs.GetString(SavedRoomPrefsKey, "");
    public static string GetSavedSoloLevelingSceneName => PlayerPrefs.GetString(SavedSoloLevelingSceneKey, "");
    public static GameObject playersPrefd = null;

    private const string RuinsUnlockedKey = "RuinsUnlocked";

    void Awake()
    {
        // its horrific
        Destroy(playersPrefd);
        playersPrefd = gameObject;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveCurrentRoom()
    {
        if (RoomManager.Instance == null)
        {
            Debug.LogWarning("[Playerpref] No RoomManager found.");
            UpdateStatus("Save failed: no RoomManager.");
            return;
        }

        var currentRoom = RoomManager.Instance.CurrentRoom;
        if (currentRoom == null || string.IsNullOrEmpty(currentRoom.sceneName))
        {
            Debug.LogWarning("[Playerpref] No current room to save.");
            UpdateStatus("Save failed: no current room.");
            return;
        }

        string savedRoomName = currentRoom.sceneName;
        string savedSoloLevelingName = SoloLeveling.lastLoadedLevelSceneName;

        PlayerPrefs.SetString(SavedRoomPrefsKey, savedRoomName);
        PlayerPrefs.SetString(SavedSoloLevelingSceneKey, savedSoloLevelingName);
        PlayerPrefs.Save();
        Debug.Log($"[Playerpref] Saved current room: {savedRoomName}");
        Debug.Log($"[Playerpref] Saved SoloLeveling button-passed scene name: {savedSoloLevelingName}");
        UpdateStatus($"Saved room: {savedRoomName} (SoloLeveling: {savedSoloLevelingName})");
    }

    public void LoadSavedRoom()
    {
        // ===== CRITICAL FIX: UNFREEZE DULU =====
        Time.timeScale = 1f;

        if (RoomManager.Instance == null)
        {
            Debug.LogWarning("[Playerpref] No RoomManager found.");
            UpdateStatus("Load failed: no RoomManager.");
            return;
        }

        if (!PlayerPrefs.HasKey(SavedRoomPrefsKey))
        {
            Debug.LogWarning("[Playerpref] No saved room found.");
            UpdateStatus("Load failed: no saved room.");
            return;
        }

        string savedSceneName = PlayerPrefs.GetString(SavedRoomPrefsKey);
        StartCoroutine(LoadSavedRoomCoroutine(savedSceneName)); 
    }

    private IEnumerator LoadSavedRoomCoroutine(string sceneName)
    {
        var room = RoomManager.Instance.GetRoomByName(sceneName);
        if (room == null)
        {
            Debug.Log($"[Playerpref] Saved scene '{sceneName}' not loaded. Loading it additively.");
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            room = RoomManager.Instance.GetRoomByName(sceneName);
        }

        if (room == null)
        {
            Debug.LogWarning($"[Playerpref] Could not find saved room with scene name '{sceneName}'.");
            UpdateStatus($"Load failed: room '{sceneName}' not found.");
            yield break;
        }

        yield return RoomManager.Instance.ReloadRoomCoroutine(room);
        CheckpointManager.Instance?.SpawnAtRoom(room);
        Debug.Log($"[Playerpref] Loaded saved room: {sceneName} and spawned at checkpoint.");
        UpdateStatus($"Loaded room: {sceneName}");
    }

    public void LoadSavedSoloLevelingScene()
    {
        if (!PlayerPrefs.HasKey(SavedSoloLevelingSceneKey))
        {
            Debug.LogWarning("[Playerpref] No saved SoloLeveling scene name found.");
            UpdateStatus("Load failed: no saved level scene.");
            return;
        }

        string savedLevelSceneName = PlayerPrefs.GetString(SavedSoloLevelingSceneKey);
        Debug.Log($"[Playerpref] Loading saved SoloLeveling scene name: {savedLevelSceneName}");

        SoloLeveling soloLeveling = FindObjectOfType<SoloLeveling>();
        if (soloLeveling == null)
        {
            Debug.LogWarning("[Playerpref] SoloLeveling instance not found.");
            UpdateStatus("Load failed: SoloLeveling missing.");
            return;
        }

        soloLeveling.LoadLevel(savedLevelSceneName, LoadSavedRoom);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
            Debug.Log($"[IM GOING TO KILL YOU] {message}");
    }

    //SPECIFICALLY RUINS STUFF
    public static bool RuinsUnlocked =>
    PlayerPrefs.GetInt(RuinsUnlockedKey, 0) == 1;

    public static void UnlockRuins()
    {
        PlayerPrefs.SetInt(RuinsUnlockedKey, 1);
        PlayerPrefs.Save();

        Debug.Log("[Playerpref] RUINS UNLOCKED!");
    }

        public static void LockRuins()
    {
        PlayerPrefs.SetInt(RuinsUnlockedKey, 0);
        PlayerPrefs.Save();

        Debug.Log("[Playerpref] RUINS LOCKED!");
    }
}