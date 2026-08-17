using UnityEngine;

public class MountainEnding : MonoBehaviour
{
    private PauseMenuManager pauseMenu;
    private bool triggered;

    private void Awake()
    {
        pauseMenu = FindObjectOfType<PauseMenuManager>();

        if (pauseMenu == null)
        {
            Debug.LogError("[MountainEnding] Could not find PauseMenuManager!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // 1. UNLOCK RUINS
        Playerpref.UnlockRuins();
        Debug.Log("[MountainEnding] Ruins UNLOCKED!");

        // 2. SET FLAG bahwa player dari Ruins
        PauseMenuManager.SetFromRuins(true);

        // 3. QUIT TO MAIN MENU
        PauseMenuManager pauseMenu = FindObjectOfType<PauseMenuManager>();

        if (pauseMenu != null)
        {
            pauseMenu.QuitToMainMenu();
        }
        else
        {
            Debug.LogError("[MountainEnding] PauseMenuManager not found!");
        }
    }
}