using UnityEngine;
using UnityEngine.SceneManagement;

public class SoloLeveling : MonoBehaviour
{
    [Tooltip("Scene that will be loaded on top of the levels, this is the scene that contains the player")]
    public SceneField player;

    public void LoadLevel(string sceneName)
    {
        if (sceneName == null || string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[LevelLoader] Provided SceneField is null or has no scene name.");
            return;
        }

        SceneManager.LoadScene(player.SceneName);
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }
}
