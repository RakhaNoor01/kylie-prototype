using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "player-save.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static void SavePlayerData(CheckpointManager checkpointManager)
    {
        if (checkpointManager == null)
            return;

        PlayerData data = new PlayerData();
        data.hasCheckpoint = checkpointManager.HasCheckpoint;
        data.sceneName = checkpointManager.GetCheckpointScene();
        data.SetCheckpointPosition(checkpointManager.GetCheckpointPosition());

        SavePlayerData(data);
    }

    public static void SavePlayerData(PlayerData data)
    {
        if (data == null)
            return;

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Failed to save player data: {ex.Message}");
        }
    }

    public static PlayerData LoadPlayerData()
    {
        try
        {
            if (!File.Exists(SavePath))
                return null;

            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonUtility.FromJson<PlayerData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Failed to load player data: {ex.Message}");
            return null;
        }
    }

    public static void ClearSave()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Failed to clear save data: {ex.Message}");
        }
    }
}