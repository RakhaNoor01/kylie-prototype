using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveLoadUI : MonoBehaviour
{
    // Call this from a UI Button to save the current checkpoint
    public void SavePlayer()
    {
        if (CheckpointManager.Instance != null)
        {
            SaveSystem.SavePlayerData(CheckpointManager.Instance);
            Debug.Log("[SaveLoadUI] Saved checkpoint.");
        }
        else
        {
            Debug.LogWarning("[SaveLoadUI] CheckpointManager.Instance is null — cannot save.");
        }
    }

    // Call this from a UI Button to load the saved checkpoint
    public void LoadPlayer()
    {
        var data = SaveSystem.LoadPlayerData();
        if (data == null || !data.hasCheckpoint)
        {
            Debug.Log("[SaveLoadUI] No saved checkpoint to load.");
            return;
        }

        // If the saved checkpoint is in a different scene, warn the user.
        if (SceneManager.GetActiveScene().name != data.sceneName)
        {
            Debug.LogWarning($"[SaveLoadUI] Saved checkpoint is in scene '{data.sceneName}' but current scene is '{SceneManager.GetActiveScene().name}'. Load the scene first.");
        }

        // Move the player if available
        var playerObj = Slopburger.instance != null ? Slopburger.instance.gameObject : null;
        if (playerObj != null)
        {
            playerObj.transform.position = data.GetCheckpointPosition();
            Debug.Log($"[SaveLoadUI] Player moved to saved checkpoint at {data.GetCheckpointPosition()}.");
        }
        else
        {
            Debug.LogWarning("[SaveLoadUI] Player object not found (Slopburger.instance is null).");
        }

        // Update checkpoint manager state as well
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetCheckpoint(data.GetCheckpointPosition(), data.sceneName, false);
        }
    }
}
