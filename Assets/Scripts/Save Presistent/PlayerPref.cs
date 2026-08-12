using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class Playerpref : MonoBehaviour
{
    public TMP_Text statusText;
    private const string SavedRoomPrefsKey = "SavedRoomSceneName";

    public static bool HasSavedCheckpoint => PlayerPrefs.HasKey(SavedRoomPrefsKey);
    public static string GetSavedRoomName => PlayerPrefs.GetString(SavedRoomPrefsKey, "");

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

        PlayerPrefs.SetString(SavedRoomPrefsKey, currentRoom.sceneName);
        PlayerPrefs.Save();
        Debug.Log($"[Playerpref] Saved current room: {currentRoom.sceneName}");
        UpdateStatus($"Saved room: {currentRoom.sceneName}");
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

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}