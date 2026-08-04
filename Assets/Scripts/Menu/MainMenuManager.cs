// MainMenuManager.cs - FULL CODE (dengan Credits Escape fix)
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("Canvas Groups")]
    public CanvasGroup groupMainMenu;
    public CanvasGroup groupSettings;
    public CanvasGroup groupCredits;

    [Header("References")]
    public MenuNavigator navigator;
    public SettingsMenuManager settingsMenu;

    [Header("Durasi Transisi")]
    public float transitionOutDuration = 0.22f;
    public float transitionInDuration = 0.32f;

    [Header("Start Journey")]
    public string startSceneName;

    private Tween _currentTween;
    private bool _isInCredits = false;

    private void Awake()
    {
        if (navigator == null) navigator = GetComponent<MenuNavigator>();
        if (settingsMenu == null) settingsMenu = GetComponentInChildren<SettingsMenuManager>();

        GroupOn(groupMainMenu);
        GroupOff(groupSettings);
        GroupOff(groupCredits);
    }

    private void Start()
    {
        navigator.ResetSelection();
    }

    private void Update()
    {
        // ESC untuk keluar dari Credits
        if (_isInCredits && Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[MainMenuManager] ESC pressed in Credits - closing...");
            CloseCredits();
        }
    }

    public void OnClickPlay()
    {
        if (string.IsNullOrEmpty(startSceneName))
        {
            Debug.LogWarning("[MainMenuManager] startSceneName belum diisi di Inspector");
            return;
        }
        SceneManager.LoadScene(startSceneName);
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
            toRect.localScale = Vector3.one * 1.3f;
            _currentTween = toRect.DOScale(1f, transitionInDuration)
                .SetEase(Ease.OutBack);
        }
        yield return _currentTween.WaitForCompletion();

        SetBlocking(to, true);
    }

    private void GroupOn(CanvasGroup g)
    {
        g.alpha = 1f;
        g.interactable = true;
        g.blocksRaycasts = true;
    }

    private void GroupOff(CanvasGroup g)
    {
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