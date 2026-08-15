using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Pasang di GameObject "PauseManager" pada tiap scene level (Forest / Mountain / Ruins).
///
/// HIERARCHY (contoh):
/// [PauseManager]                  ← pasang PauseMenuManager.cs di sini
///
/// [Canvas]
///   └── Group_Pause               ← CanvasGroup, isi field pauseGroup dengan ini
///         ├── Text "PAUSED"
///         ├── Btn_Resume                  ← MenuButton.cs, index 0
///         ├── Btn_ContinueFromCheckpoint  ← MenuButton.cs, index 1
///         ├── Btn_QuitToMainMenu          ← MenuButton.cs, index 2
///         └── ControlGuide        ← sprite hint ENTER/Panah/ESC (statis, otomatis
///                                    ikut muncul/ilang bareng Group_Pause)
///
public class PauseMenuManager : MonoBehaviour
{
    [Header("Panel Pause")]
    [Tooltip("CanvasGroup yang membungkus seluruh panel Pause (judul, tombol, control guide). " +
             "Cuma di-toggle alpha 0/1 instan, TIDAK ada animasi slide/fade in-out untuk panel ini.")]
    public CanvasGroup pauseGroup;

    [Header("Tombol (urutan WAJIB: 0=Resume, 1=Continue from Checkpoint, 2=Quit to Main Menu)")]
    [Tooltip("Ketiga tombol ini harus selalu aktif/visible di scene, jangan pernah di-SetActive(false).")]
    public MenuButton[] buttons;

    [Header("Checkpoint (Playerpref)")]
    [Tooltip("Referensi ke komponen Playerpref di scene ini. Kalau kosong, otomatis " +
             "di-cari lewat FindObjectOfType<Playerpref>() saat Awake/Pause.")]
    public Playerpref playerPref;

    [Header("Quit to Main Menu — Circle Wipe")]
    [Tooltip("RectTransform Image lingkaran putih, sama seperti punya MainMenuManager. Default: inactive.")]
    public CircleWipeController circleWipe;

    [Tooltip("Fallback kalau circleWipe kosong: CanvasGroup buat fade layar polos ke hitam/putih " +
             "sebelum pindah scene. Fade-nya pakai DOTween (unscaled time).")]
    public CanvasGroup panelFadeFallback;

    [Tooltip("Nama scene Main Menu yang akan di-load.")]
    public string mainMenuSceneName = "Main Menu";

    [Tooltip("Durasi fade fallback kalau circleWipe kosong (detik, unscaled).")]
    public float fallbackFadeDuration = 0.5f;

    public bool IsPaused { get; private set; } = false;

    private const int BTN_RESUME = 0;
    private const int BTN_CONTINUE_CHECKPOINT = 1;
    private const int BTN_QUIT_TO_MENU = 2;

    private int _index = 0;
    private bool _navActive = false;

    private void Awake()
    {
        if (playerPref == null)
            playerPref = FindObjectOfType<Playerpref>();

        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            buttons[i].onHover = () => Select(idx);
            // NOTE: field di MenuButton.cs namanya "onSubmit" (bukan "onClick").
            // Kita pass idx langsung biar klik mouse gak bergantung ke _index terakhir
            // dari hover (lebih aman daripada baca _index doang).
            buttons[i].onSubmit = () => ConfirmIndex(idx);
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
            ConfirmIndex(_index);
    }

    // ── Pause / Resume ───────────────────────────────────────────────────────

    /// <summary>Panggil ini dari mana pun (tombol pause di HUD, dsb) buat munculin panel Pause.</summary>
    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;

        if (playerPref == null)
            playerPref = FindObjectOfType<Playerpref>();

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

    private void ConfirmIndex(int idx)
    {
        if (!_navActive) return;
        _index = idx;

        switch (idx)
        {
            case BTN_RESUME:              Resume();                        break;
            case BTN_CONTINUE_CHECKPOINT: OnClickContinueFromCheckpoint(); break;
            case BTN_QUIT_TO_MENU:        OnClickQuitToMainMenu();         break;
        }
    }

    // ── Continue from Checkpoint ─────────────────────────────────────────────

    /// <summary>
    /// Reload checkpoint terakhir yang tersimpan (lewat Playerpref.LoadSavedRoom()).
    /// Tombolnya SELALU bisa diklik. Kalau belum ada checkpoint tersimpan sama sekali,
    /// ini sengaja no-op (gak ada apa2 yang kejadian) sesuai spek.
    /// </summary>
    public void OnClickContinueFromCheckpoint()
    {
        if (!IsPaused) return;

        if (playerPref == null)
            playerPref = FindObjectOfType<Playerpref>();

        if (playerPref == null || !Playerpref.HasSavedCheckpoint)
        {
            // Belum pernah checkpoint sama sekali -> diam aja, panel tetap kebuka.
            Debug.Log("[PauseMenuManager] Continue from Checkpoint ditekan tapi belum ada checkpoint tersimpan.");
            return;
        }

        _navActive = false;
        IsPaused   = false;
        GroupOff(pauseGroup);

        // Playerpref.LoadSavedRoom() sendiri yang bakal set Time.timeScale = 1f
        // dan handle reload room + spawn di checkpoint.
        playerPref.LoadSavedRoom();
    }

    // ── Quit to Main Menu (save checkpoint + Circle Wipe) ──────────────────────

    /// <summary>
    /// Simpan posisi/room sekarang sebagai checkpoint, lalu circle wipe + load Main Menu,
    /// persis pola MainMenuManager.Co_LoadScene().
    /// </summary>
    public void OnClickQuitToMainMenu()
    {
        if (!IsPaused) return;
        _navActive = false;
        StartCoroutine(Co_QuitToMainMenu());
    }

    private IEnumerator Co_QuitToMainMenu()
    {
        if (playerPref == null)
            playerPref = FindObjectOfType<Playerpref>();

        // Simpan checkpoint dulu SEBELUM pindah scene. Kalau belum ada room valid
        // buat disave, Playerpref.SaveCurrentRoom() sendiri yang nge-warn & skip --
        // jadi aman dipanggil kapan aja, termasuk sebelum player pernah checkpoint.
        playerPref?.SaveCurrentRoom();

        // Balikin timeScale dulu SEBELUM pindah scene — kalau enggak, scene Main Menu
        // yang baru di-load bakal ikut freeze juga (Time.timeScale kebawa antar-scene).
        Time.timeScale = 1f;
        IsPaused = false;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuSceneName);
        asyncLoad.allowSceneActivation = false;

        if (circleWipe != null)
            yield return StartCoroutine(circleWipe.WipeIn());
        else if (panelFadeFallback != null)
            yield return Co_FadeGroupDOTween(panelFadeFallback, 0f, 1f, fallbackFadeDuration);

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;
    }

    /// <summary>Fade CanvasGroup pakai DOTween (unscaled, tetep jalan walau timeScale 0).</summary>
    private IEnumerator Co_FadeGroupDOTween(CanvasGroup g, float from, float to, float duration)
    {
        g.alpha = from;
        bool done = false;

        g.DOFade(to, duration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true) // unscaled time
            .OnComplete(() => done = true);

        while (!done) yield return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void GroupOn(CanvasGroup g)  { g.alpha = 1f; g.interactable = true;  g.blocksRaycasts = true;  }
    private void GroupOff(CanvasGroup g) { g.alpha = 0f; g.interactable = false; g.blocksRaycasts = false; }
}