using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Pasang di GameObject "PauseManager" pada tiap scene level (Forest / Mountain / Ruins).
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    // ===== STATIC FLAG UNTUK RUINS =====
    private static bool _isFromRuins = false;

    public static void SetFromRuins(bool fromRuins)
    {
        _isFromRuins = fromRuins;
        Debug.Log($"[PauseMenuManager] SetFromRuins: {fromRuins}");
    }

    public static bool IsFromRuins => _isFromRuins;

    public static void ResetFromRuinsFlag()
    {
        _isFromRuins = false;
        Debug.Log("[PauseMenuManager] ResetFromRuinsFlag");
    }

    // ===== EXISTING CODE =====
    [Header("Panel Pause")]
    public CanvasGroup pauseGroup;

    [Header("Tombol (urutan WAJIB: 0=Resume, 1=Continue from Checkpoint, 2=Quit to Main Menu)")]
    public MenuButton[] buttons;

    [Header("Checkpoint (Playerpref)")]
    public Playerpref playerPref;

    [Header("Quit to Main Menu — Circle Wipe")]
    public CircleWipeController circleWipe;

    public CanvasGroup panelFadeFallback;
    public string mainMenuSceneName = "Main Menu";
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
            buttons[i].onSubmit = () => ConfirmIndex(idx);
            buttons[i].SetSelected(false);
        }

        GroupOff(pauseGroup);
    }

    private void Update()
    {
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

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;

        if (playerPref == null)
            playerPref = FindObjectOfType<Playerpref>();

        _index = 0;
        _navActive = true;
        GroupOn(pauseGroup);
        Refresh();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        _navActive = false;
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

    public void OnClickContinueFromCheckpoint()
    {
        if (!IsPaused) return;

        if (playerPref == null)
            playerPref = FindObjectOfType<Playerpref>();

        if (playerPref == null || !Playerpref.HasSavedCheckpoint)
        {
            Debug.Log("[PauseMenuManager] Continue from Checkpoint ditekan tapi belum ada checkpoint tersimpan.");
            return;
        }

        _navActive = false;
        IsPaused = false;
        GroupOff(pauseGroup);

        playerPref.LoadSavedRoom();
    }

    // ── Quit to Main Menu (save checkpoint + Circle Wipe) ──────────────────────

    public void OnClickQuitToMainMenu()
    {
        if (!IsPaused) return;

        _navActive = false;
        StartCoroutine(Co_QuitToMainMenu());
    }

    public void QuitToMainMenu()
    {
        _navActive = false;
        StartCoroutine(Co_QuitToMainMenu());
    }

    private IEnumerator Co_QuitToMainMenu()
    {
        if (playerPref == null)
            playerPref = FindObjectOfType<Playerpref>();

        playerPref?.SaveCurrentRoom();

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

    private IEnumerator Co_FadeGroupDOTween(CanvasGroup g, float from, float to, float duration)
    {
        g.alpha = from;
        bool done = false;

        g.DOFade(to, duration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true)
            .OnComplete(() => done = true);

        while (!done) yield return null;
    }

    private void GroupOn(CanvasGroup g) { g.alpha = 1f; g.interactable = true; g.blocksRaycasts = true; }
    private void GroupOff(CanvasGroup g) { g.alpha = 0f; g.interactable = false; g.blocksRaycasts = false; }
}