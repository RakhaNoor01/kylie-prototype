using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnableIfScene : MonoBehaviour
{
    public string theSceneInQuestion;
    public List<GameObject> things = new List<GameObject>();

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ToggleStuff(scene, true);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        ToggleStuff(scene, false);
    }

    private void ToggleStuff(Scene scene, bool state)
    {
        if (!theSceneInQuestion.Equals(scene.name)) return;

        foreach (GameObject thing in things)
        {
            thing.SetActive(state);
        }
    }
}