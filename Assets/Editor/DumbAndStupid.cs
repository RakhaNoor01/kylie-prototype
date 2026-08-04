using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class DumbAndStupid
{
    private const string MenuPath = "Tools/Always Start From Main Menu";
    private const string PrefKey = "ForceStartSceneEnabled";
    private const string ScenePath = 
        "Assets/Scenes/Menu/Main Menu.unity";

    static DumbAndStupid()
    {
        // Delay the execution slightly to ensure Unity has fully loaded
        EditorApplication.delayCall += ApplyStartSceneSetting;
    }

    [MenuItem(MenuPath)]
    public static void ToggleAction()
    {
        // Toggle the saved boolean state
        bool currentState = EditorPrefs.GetBool(PrefKey, false);
        EditorPrefs.SetBool(PrefKey, !currentState);

        ApplyStartSceneSetting();
    }

    [MenuItem(MenuPath, true)]
    public static bool ToggleActionValidate()
    {
        // Places a checkmark in the menu if enabled
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, false));
        return true;
    }

    private static void ApplyStartSceneSetting()
    {
        bool isEnabled = EditorPrefs.GetBool(PrefKey, false);

        if (isEnabled)
        {
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (startScene != null)
            {
                EditorSceneManager.playModeStartScene = startScene;
            }
        }
        else
        {
            // Reset to default (Unity plays the currently open scene)
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
