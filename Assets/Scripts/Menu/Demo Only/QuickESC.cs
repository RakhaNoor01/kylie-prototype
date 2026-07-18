using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hold ESC selama 1 detik di scene gameplay → loading screen → balik ke Main Menu.
///
/// SETUP:
/// [HoldEscToMenu]            ← GameObject baru di scene gameplay (Forest/Mountain)
///   └── HoldEscToMenu.cs     ← script ini
///
/// Tidak perlu wiring apapun selain isi Main Menu Scene Name di Inspector.
/// </summary>
public class HoldEscToMenu : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Berapa detik ESC harus ditahan sebelum trigger")]
    public float holdDuration = 1f;

    [Tooltip("Nama scene Main Menu untuk kembali")]
    public string mainMenuSceneName = "MainMenu";

    private float _holdTimer = 0f;
    private bool _triggered = false;

    private void Update()
    {
        if (_triggered) return;

        if (Input.GetKey(KeyCode.Escape))
        {
            _holdTimer += Time.deltaTime;

            if (_holdTimer >= holdDuration)
            {
                _triggered = true;
                StartCoroutine(Co_ReturnToMenu());
            }
        }
        else
        {
            // Lepas tombol sebelum durasi tercapai → reset timer
            _holdTimer = 0f;
        }
    }

    private IEnumerator Co_ReturnToMenu()
    {
        if (LoadingScreenController.Instance != null)
            yield return LoadingScreenController.Instance.Show();

        SceneManager.LoadScene(mainMenuSceneName);
    }
}