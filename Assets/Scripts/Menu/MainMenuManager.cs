using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Canvas Groups")]
    public CanvasGroup groupMainMenu;
    public CanvasGroup groupLevelSelect;
    public CanvasGroup groupControlsGuide;
    public CanvasGroup panelFade;

    [Header("Circle Wipe")]
    [Tooltip("RectTransform Image lingkaran putih di tengah layar. Default: inactive.")]
    public CircleWipeController circleWipe;

    [Header("Main Menu Elements")]
    public RectTransform logo;
    [Tooltip("Urutan: Play, Controls, Exit")]
    public RectTransform[] menuButtons;

    [Header("Scene Names")]
    public string forestSceneName   = "Level_Forest";
    public string mountainSceneName = "Level_Mountain";

    [Header("Timing")]
    public float introFadeDuration   = 1f;
    public float logoDelay           = 0.3f;
    public float buttonStagger       = 0.18f;   
    public float buttonSlideDuration = 0.4f;
    public float panelTransDuration  = 0.45f;
    public float circleWipeDuration  = 0.6f;

    [Header("Logo Floating Animation")]
    [Tooltip("Kecepatan naik turun logo")]
    public float logoFloatSpeed = 2.5f;
    [Tooltip("Ketinggian ayunan naik turun logo")]
    public float logoFloatAmplitude = 12f;

    public enum MenuState { Intro, MainMenu, LevelSelect, ControlsGuide, Transitioning }
    public MenuState CurrentState { get; private set; } = MenuState.Intro;

    private float _canvasWidth;
    private MenuAnimator _anim;
    private LevelSelectManager _levelSelect;
    private MenuNavigator _navigator;

    // Untuk melacak posisi dasar logo agar tidak bentrok dengan animasi geser intro
    private Vector2 _logoBasePosition;
    private bool _isLogoBaseCaptured = false;

    private void Awake()
    {
        _anim        = GetComponent<MenuAnimator>();
        _levelSelect = GetComponent<LevelSelectManager>();
        _navigator   = GetComponent<MenuNavigator>();

        Canvas canvas = FindFirstObjectByType<Canvas>();
        _canvasWidth  = ((RectTransform)canvas.transform).rect.width;

        GroupOff(groupMainMenu);
        GroupOff(groupLevelSelect);
        GroupOff(groupControlsGuide);

        panelFade.alpha          = 1f;
        panelFade.interactable   = false;
        panelFade.blocksRaycasts = false;
    }

    private void Start() => StartCoroutine(Co_Intro());

    private void Update()
    {
        // Jalankan efek melayang naik-turun logo
        AnimateLogoFloat();

        if (CurrentState == MenuState.Transitioning) return;

        if (CurrentState == MenuState.ControlsGuide)
        {
            if (Input.GetKeyDown(KeyCode.W)       || Input.GetKeyDown(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.UpArrow)  || Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.Escape)   || Input.GetMouseButtonDown(0))
                CloseControlsGuide();
        }

        if (CurrentState == MenuState.LevelSelect)
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseLevelSelect();
    }

    private void AnimateLogoFloat()
    {
        if (logo == null) return;

        // Mulai melayang hanya saat intro selesai agar posisi geser awal rapi
        if (CurrentState != MenuState.Intro)
        {
            if (!_isLogoBaseCaptured)
            {
                _logoBasePosition = logo.anchoredPosition;
                _isLogoBaseCaptured = true;
            }

            float newY = _logoBasePosition.y + Mathf.Sin(Time.time * logoFloatSpeed) * logoFloatAmplitude;
            logo.anchoredPosition = new Vector2(logo.anchoredPosition.x, newY);
        }
        else
        {
            _isLogoBaseCaptured = false;
        }
    }

    // ── Intro ─────────────────────────────────────────────────────────────────

    private IEnumerator Co_Intro()
    {
        CurrentState = MenuState.Transitioning;

        yield return StartCoroutine(_anim.FadeGroup(panelFade, 1f, 0f, introFadeDuration));
        yield return new WaitForSeconds(logoDelay);

        GroupOn(groupMainMenu);
        yield return null; 
        yield return null; 

        _anim.PrepareSlide(logo, offsetX: -300f);
        foreach (var btn in menuButtons)
            _anim.PrepareSlide(btn, offsetX: -380f);  

        yield return null; 

        StartCoroutine(_anim.SlideToHome(logo, buttonSlideDuration));
        yield return new WaitForSeconds(buttonStagger);

        foreach (var btn in menuButtons)
        {
            StartCoroutine(_anim.SlideToHome(btn, buttonSlideDuration));
            yield return new WaitForSeconds(buttonStagger);
        }

        yield return new WaitForSeconds(buttonSlideDuration);
        CurrentState = MenuState.MainMenu;
        _navigator.Activate();
    }

    // ── Main Menu ↔ Level Select ───────────────────────────────────────────────

    public void OnClickPlay()
    {
        if (CurrentState != MenuState.MainMenu) return;
        StartCoroutine(Co_ToLevelSelect());
    }

    private IEnumerator Co_ToLevelSelect()
    {
        CurrentState = MenuState.Transitioning;
        _navigator.Deactivate();

        // Paksa kartu mengecil sebelum panel Journey digeser masuk
        _levelSelect.PrepareTransition();

        yield return StartCoroutine(_anim.SlideOutLeft(groupMainMenu, panelTransDuration, _canvasWidth));
        GroupOff(groupMainMenu);

        GroupOn(groupLevelSelect);
        yield return StartCoroutine(_anim.SlideInRight(groupLevelSelect, panelTransDuration, _canvasWidth));

        CurrentState = MenuState.LevelSelect;
        _levelSelect.OnEnter(); 
    }

    public void CloseLevelSelect()
    {
        if (CurrentState != MenuState.LevelSelect) return;
        StartCoroutine(Co_BackFromLevelSelect());
    }

    private IEnumerator Co_BackFromLevelSelect()
    {
        CurrentState = MenuState.Transitioning;

        _levelSelect.OnExit();

        yield return StartCoroutine(_anim.SlideOutRight(groupLevelSelect, panelTransDuration, _canvasWidth));
        GroupOff(groupLevelSelect);

        GroupOn(groupMainMenu);
        
        // Aktifkan hover Main Menu di SINI agar tombol langsung terlihat aktif saat panel bergerak masuk
        _navigator.Activate();
        
        yield return StartCoroutine(_anim.SlideInLeft(groupMainMenu, panelTransDuration, _canvasWidth));

        CurrentState = MenuState.MainMenu;
    }

    // ── Main Menu ↔ Controls Guide ─────────────────────────────────────────────

    public void OnClickControls()
    {
        if (CurrentState != MenuState.MainMenu) return;
        StartCoroutine(Co_ToControls());
    }

    private IEnumerator Co_ToControls()
    {
        CurrentState = MenuState.Transitioning;
        GroupOn(groupControlsGuide);
        yield return StartCoroutine(_anim.SlideInRight(groupControlsGuide, panelTransDuration, _canvasWidth));
        CurrentState = MenuState.ControlsGuide;
    }

    public void CloseControlsGuide()
    {
        if (CurrentState != MenuState.ControlsGuide) return;
        StartCoroutine(Co_CloseControls());
    }

    private IEnumerator Co_CloseControls()
    {
        CurrentState = MenuState.Transitioning;
        yield return StartCoroutine(_anim.SlideOutRight(groupControlsGuide, panelTransDuration, _canvasWidth));
        GroupOff(groupControlsGuide);
        CurrentState = MenuState.MainMenu;
    }

    public void OnClickControlsBackground() => CloseControlsGuide();

    // ── Load Level (Circle Wipe) ───────────────────────────────────────────────

    public void LoadForest()   => StartCoroutine(Co_LoadScene(forestSceneName));
    public void LoadMountain() => StartCoroutine(Co_LoadScene(mountainSceneName));

    private IEnumerator Co_LoadScene(string sceneName)
    {
        CurrentState = MenuState.Transitioning;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        if (circleWipe != null)
            yield return StartCoroutine(circleWipe.WipeIn());
        else
            yield return StartCoroutine(_anim.FadeGroup(panelFade, 0f, 1f, 0.5f));

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;
    }

    public void OnClickExit()
    {
        if (CurrentState != MenuState.MainMenu) return;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public void GroupOn(CanvasGroup g)  { g.alpha = 1f; g.interactable = true;  g.blocksRaycasts = true;  }
    public void GroupOff(CanvasGroup g) { g.alpha = 0f; g.interactable = false; g.blocksRaycasts = false; }
}