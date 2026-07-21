using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pasang di GameObject "PauseManager" pada tiap scene level (Forest / Mountain / Ruins).
///
/// HIERARCHY (contoh):
/// [PauseManager]                  ← pasang PauseMenuManager.cs di sini
///
/// [Canvas]
///   └── Group_Pause               ← CanvasGroup, isi field pauseGroup dengan ini
///         ├── Text "PAUSED"
///         ├── Btn_Continue        ← pasang MenuButton.cs (SAMA seperti tombol Main Menu)
///         ├── Btn_BackToMenu      ← pasang MenuButton.cs
///         ├── Btn_Exit            ← pasang MenuButton.cs
///         └── ControlGuide        ← sprite hint ENTER/Panah/ESC (statis, gak perlu script apa2,
///                                    otomatis ikut muncul/ilang bareng Group_Pause)
///
/// BEDA DARI MAIN MENU / LEVEL SELECT:
/// - TIDAK ADA slide/reveal transition buat panel Pause-nya sendiri. Sesuai spek:
///   "tidak perlu transisi in out pause, cukup hover button aja". Jadi Group_Pause cuma
///   di-toggle alpha 0/1 instan (GroupOn/GroupOff), animasi cuma ada di hover tombol
///   (ditangani MenuButton.cs, reuse dari Main Menu).
/// - Game BENERAN di-pause (Time.timeScale = 0) selama panel ini aktif, bukan cuma visual.
///   Konsekuensinya: script gameplay yang pakai Time.deltaTime otomatis freeze — itu memang
///   yang diinginkan ("gamenya otomatis paused").
/// - "Back to Main Menu" pakai circle wipe + load scene, PERSIS seperti waktu pilih level
///   di MainMenuManager.Co_LoadScene().
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("Panel Pause")]
    [Tooltip("CanvasGroup yang membungkus seluruh panel Pause (judul, tombol, control guide). " +
             "Cuma di-toggle alpha 0/1 instan, TIDAK ada animasi slide/fade in-out.")]
    public CanvasGroup pauseGroup;

    [Header("Tombol (urutan: Continue, Back to Main Menu, Exit)")]
    public MenuButton[] buttons;

    [Header("Back to Main Menu — Circle Wipe")]
    [Tooltip("RectTransform Image lingkaran putih, sama seperti punya MainMenuManager. Default: inactive.")]
    public CircleWipeController circleWipe;

    [Tooltip("Fallback kalau circleWipe kosong: CanvasGroup buat fade layar polos ke hitam/putih sebelum pindah scene.")]
    public CanvasGroup panelFadeFallback;

    [Tooltip("Nama scene Main Menu yang akan di-load.")]
    public string mainMenuSceneName = "Main Menu";

    [Tooltip("Durasi fade fallback kalau circleWipe kosong (detik, unscaled).")]
    public float fallbackFadeDuration = 0.5f;

    public bool IsPaused { get; private set; } = false;

    private int _index = 0;
    private bool _navActive = false;

    private void Awake()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            buttons[i].onHover = () => Select(idx);
            buttons[i].onClick = () => Confirm();
            buttons[i].SetSelected(false);
        }

        GroupOff(pauseGroup);
    }

    private void Update()
    {
        // ESC selalu jadi toggle pause/resume, gak peduli state navigasi internal.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) Resume();
            else Pause();
            return;
        }

        if (!_navActive) return;

        bool moved = false;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            _index = (_index - 1 + buttons.Length) % buttons.Length;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            _index = (_index + 1) % buttons.Length;
            moved = true;
        }
        if (moved) Refresh();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            Confirm();
    }

    // ── Pause / Resume ───────────────────────────────────────────────────────

    /// <summary>Panggil ini dari mana pun (tombol pause di HUD, dsb) buat munculin panel Pause.</summary>
    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;

        _index     = 0;
        _navActive = true;
        GroupOn(pauseGroup);
        Refresh();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused    = false;
        _navActive  = false;
        Time.timeScale = 1f;

        GroupOff(pauseGroup);
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void Select(int idx)
    {
        if (!_navActive) return;
        _index = idx;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetSelected(i == _index);
    }

    private void Confirm()
    {
        if (!_navActive) return;

        switch (_index)
        {
            case 0: Resume();         break;
            case 1: OnClickBackToMainMenu(); break;
            case 2: OnClickExit();    break;
        }
    }

    // ── Back to Main Menu (Circle Wipe, sama pola kaya MainMenuManager.Co_LoadScene) ──

    public void OnClickBackToMainMenu()
    {
        if (!IsPaused) return;
        _navActive = false;
        StartCoroutine(Co_BackToMainMenu());
    }

    private IEnumerator Co_BackToMainMenu()
    {
        // Balikin timeScale dulu SEBELUM pindah scene — kalau enggak, scene Main Menu
        // yang baru di-load bakal ikut freeze juga (Time.timeScale kebawa antar-scene).
        Time.timeScale = 1f;
        IsPaused = false;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuSceneName);
        asyncLoad.allowSceneActivation = false;

        if (circleWipe != null)
            yield return StartCoroutine(circleWipe.WipeIn());
        else if (panelFadeFallback != null)
            yield return StartCoroutine(Co_FadeGroup(panelFadeFallback, 0f, 1f, fallbackFadeDuration));

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;
    }

    private IEnumerator Co_FadeGroup(CanvasGroup g, float from, float to, float duration)
    {
        g.alpha = from;
        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime; // unscaled, biar tetep jalan walau timeScale sempat 0
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(e / duration));
            yield return null;
        }
        g.alpha = to;
    }

    // ── Exit ──────────────────────────────────────────────────────────────────

    public void OnClickExit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void GroupOn(CanvasGroup g)  { g.alpha = 1f; g.interactable = true;  g.blocksRaycasts = true;  }
    private void GroupOff(CanvasGroup g) { g.alpha = 0f; g.interactable = false; g.blocksRaycasts = false; }
}