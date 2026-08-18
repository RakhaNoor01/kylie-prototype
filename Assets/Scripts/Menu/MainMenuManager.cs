// MainMenuManager.cs - FULL CODE (dengan Credits Escape fix + Ruins auto-open)
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("Canvas Groups")]
    public CanvasGroup groupMainMenu;
    public CanvasGroup groupSettings;
    public CanvasGroup groupCredits;
    public CanvasGroup groupLevelSelect;

    [Header("References")]
    public MenuNavigator navigator;
    public SettingsMenuManager settingsMenu;
    public LevelSelectManager levelSelectManager;

    [Header("Durasi Transisi")]
    public float transitionOutDuration = 0.22f;
    public float transitionInDuration = 0.32f;

    private Tween _currentTween;
    private bool _isInCredits = false;
    private bool _isInLevelSelect = false;

    private void Awake()
    {
        if (navigator == null) navigator = GetComponent<MenuNavigator>();
        if (settingsMenu == null) settingsMenu = GetComponentInChildren<SettingsMenuManager>();

        GroupOn(groupMainMenu);
        GroupOff(groupSettings);
        GroupOff(groupCredits);
        GroupOff(groupLevelSelect);
    }

    private void Start()
    {
        navigator.ResetSelection();

        // 🔥 CEK FLAG: APAKAH PLAYER DATANG DARI RUINS?
        if (PauseMenuManager.IsFromRuins)
        {
            Debug.Log("[MainMenuManager] Player came from Ruins! Auto-opening Level Select...");
            
            // Reset flag dulu biar ga ke-trigger lagi
            PauseMenuManager.ResetFromRuinsFlag();

            // Buka Level Select otomatis + highlight Ruins
            StartCoroutine(Co_AutoOpenLevelSelect());
        }
    }

    private IEnumerator Co_AutoOpenLevelSelect()
    {
        // Tunggu 1 frame biar UI siap
        yield return null;

        if (levelSelectManager == null)
        {
            Debug.LogWarning("[MainMenuManager] levelSelectManager belum di-set di Inspector");
            yield break;
        }

        // Siapkan transisi
        levelSelectManager.PrepareTransition();

        // Animasi pindah ke Level Select
        _currentTween?.Kill();
        yield return StartCoroutine(Co_SwapPanels(groupMainMenu, groupLevelSelect, zoomIn: false));

        _isInLevelSelect = true;
        levelSelectManager.OnEnter();

        // 🔥 BUKA PANEL RUINS DI LEVEL SELECT
        // Coba panggil method OpenRuinsPanel() kalau ada
        var method = levelSelectManager.GetType().GetMethod("OpenRuinsPanel");
        if (method != null)
        {
            method.Invoke(levelSelectManager, null);
            Debug.Log("[MainMenuManager] Called OpenRuinsPanel() via reflection");
        }
        else
        {
            // Alternative: coba panggil SelectLevel dengan index Ruins
            var selectMethod = levelSelectManager.GetType().GetMethod("SelectLevel");
            if (selectMethod != null)
            {
                // Asumsi Ruins adalah level index terakhir
                int ruinsIndex = 3; // Sesuaikan dengan index Ruins di LevelSelectManager
                selectMethod.Invoke(levelSelectManager, new object[] { ruinsIndex });
                Debug.Log($"[MainMenuManager] Called SelectLevel({ruinsIndex}) via reflection");
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] LevelSelectManager doesn't have OpenRuinsPanel() or SelectLevel() method!");
            }
        }
    }

    private void Update()
    {
        // ESC untuk keluar dari Credits
        if (_isInCredits && Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[MainMenuManager] ESC pressed in Credits - closing...");
            CloseCredits();
        }

        // ESC untuk keluar dari Level Select
        if (_isInLevelSelect && Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[MainMenuManager] ESC pressed in Level Select - closing...");
            CloseLevelSelect();
        }
    }

    public void OnClickPlay()
    {
        Debug.Log("[MainMenuManager] OnClickPlay ditekan");
        StartCoroutine(Co_ToLevelSelect());
    }

    public void CloseLevelSelect() => StartCoroutine(Co_BackFromLevelSelect());

    private IEnumerator Co_ToLevelSelect()
    {
        if (levelSelectManager == null)
        {
            Debug.LogWarning("[MainMenuManager] levelSelectManager belum di-set di Inspector");
            yield break;
        }

        levelSelectManager.PrepareTransition();

        _currentTween?.Kill();
        yield return StartCoroutine(Co_SwapPanels(groupMainMenu, groupLevelSelect, zoomIn: false));

        _isInLevelSelect = true;
        levelSelectManager.OnEnter();
    }

    private IEnumerator Co_BackFromLevelSelect()
    {
        _isInLevelSelect = false;
        levelSelectManager?.OnExit();

        _currentTween?.Kill();
        yield return StartCoroutine(Co_SwapPanels(groupLevelSelect, groupMainMenu, zoomIn: true));

        navigator.RestoreSelection();
    }

    public void OnClickOptions() => StartCoroutine(Co_ToOptions());

    public void CloseSettings() => StartCoroutine(Co_BackToMainMenu());

    private IEnumerator Co_ToOptions()
    {
        settingsMenu.Open();

        _currentTween?.Kill();
        yield return StartCoroutine(Co_SwapPanels(groupMainMenu, groupSettings, zoomIn: false));
    }

    private IEnumerator Co_BackToMainMenu()
    {
        _currentTween?.Kill();
        yield return StartCoroutine(Co_SwapPanels(groupSettings, groupMainMenu, zoomIn: true));

        navigator.RestoreSelection();
    }

    public void OnClickCredits() => StartCoroutine(Co_ToCredits());

    public void CloseCredits() => StartCoroutine(Co_BackFromCredits());

    private IEnumerator Co_ToCredits()
    {
        _currentTween?.Kill();
        yield return StartCoroutine(Co_SwapPanels(groupMainMenu, groupCredits, zoomIn: false));
        _isInCredits = true;
    }

    private IEnumerator Co_BackFromCredits()
    {
        _isInCredits = false;
        _currentTween?.Kill();
        yield return StartCoroutine(Co_SwapPanels(groupCredits, groupMainMenu, zoomIn: true));

        navigator.RestoreSelection();
    }

    public void OnClickExit()
    {
        Debug.Log("[MainMenuManager] Quit Game ditekan");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator Co_SwapPanels(CanvasGroup from, CanvasGroup to, bool zoomIn)
    {
        SetBlocking(from, false);

        RectTransform fromRect = from.GetComponent<RectTransform>();
        _currentTween = fromRect.DOScale(0f, transitionOutDuration)
            .SetEase(Ease.InCubic);
        yield return _currentTween.WaitForCompletion();

        GroupOff(from);
        fromRect.localScale = Vector3.one;

        GroupOn(to);
        SetBlocking(to, false);

        RectTransform toRect = to.GetComponent<RectTransform>();
        if (zoomIn)
        {
            toRect.localScale = Vector3.one * 0.7f;
            _currentTween = toRect.DOScale(1f, transitionInDuration)
                .SetEase(Ease.OutBack);
        }
        else
        {
            toRect.localScale = Vector3.one * 1.1f;
            _currentTween = toRect.DOScale(1f, transitionInDuration)
                .SetEase(Ease.OutBack);
        }
        yield return _currentTween.WaitForCompletion();

        SetBlocking(to, true);
    }

    private void GroupOn(CanvasGroup g)
    {
        if (g == null)
        {
            Debug.LogError("[MainMenuManager] CanvasGroup kosong — cek Inspector!", this);
            return;
        }
        g.alpha = 1f;
        g.interactable = true;
        g.blocksRaycasts = true;
    }

    private void GroupOff(CanvasGroup g)
    {
        if (g == null)
        {
            Debug.LogError("[MainMenuManager] CanvasGroup kosong — cek Inspector!", this);
            return;
        }
        g.alpha = 0f;
        g.interactable = false;
        g.blocksRaycasts = false;
    }

    private void SetBlocking(CanvasGroup g, bool enabled)
    {
        g.interactable = enabled;
        g.blocksRaycasts = enabled;
    }
}