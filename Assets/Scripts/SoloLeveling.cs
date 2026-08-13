using UnityEngine;
using UnityEngine.SceneManagement;

public class SoloLeveling : MonoBehaviour
{
    [Tooltip("Scene that will be loaded on top of the levels, this is the scene that contains the player")]
    public string player;
    public static string playerStatic = "Persistent";
    public static string lastLoadedLevelSceneName = "";

    public void LoadLevel(string sceneName)
    {
        playerStatic = player;
        lastLoadedLevelSceneName = sceneName;

        if (string.IsNullOrEmpty(sceneName)) { Debug.LogWarning("[SoloLeveling] LoadLevel called with empty sceneName."); return; }

        SceneManager.LoadScene(playerStatic);
        // Use the callback overload so we init *after* both scenes exist
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).completed += _ =>
        {
            RoomManager.Instance?.InitializeStartRoom();
        };
    }
}
