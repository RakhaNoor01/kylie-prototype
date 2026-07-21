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
    [Tooltip("Urutan: Play, Controls, Credit, Exit")]
    public RectTransform[] menuButtons;

    [Header("Timing")]
    public float introFadeDuration = 1f;
    public float logoDelay = 0.3f;
    public float buttonStagger = 0.18f;
    public float buttonSlideDuration = 0.4f;
    public float panelTransDuration = 0.45f;
    public float circleWipeDuration = 0.6f;

    [Header("Slide Offsets")]
    [Tooltip("Offset X awal logo sebelum slide masuk (logo tetap geser horizontal seperti semula)")]
    public float logoOffsetX = -300f;

    [Tooltip("Offset Y awal tombol sebelum slide ke atas (layout sekarang center, jadi tombol naik dari bawah, bukan dari kiri)")]
    public float buttonOffsetY = -380f;

    [Header("Transition Distance (MANUAL, bukan auto dari Canvas)")]
    [Tooltip("Jarak geser vertikal panel Main Menu <-> Level Select, dalam unit anchoredPosition. " +
             "CARA NENTUIN: Play, pas lagi di Main Menu, klik Group_LevelSelect di Hierarchy, geser " +
             "Pos Y di Inspector sampai keluar penuh dari Game view (Group_MainMenu juga harus hilang " +
             "total pas ini), catat selisih Pos Y dari posisi awal (0) ke posisi itu, isi angkanya di sini. " +
             "Kasih sedikit lebih dari cukup biar aman.")]
    public float verticalSlideDistance = 1200f;

    [Tooltip("Jarak geser horizontal panel Main Menu <-> Controls Guide, caranya sama seperti di atas tapi geser Pos X.")]
    public float horizontalSlideDistance = 1920f;

    [Header("Logo Floating Animation")]
    [Tooltip("Kecepatan naik turun logo")]
    public float logoFloatSpeed = 2.5f;
    [Tooltip("Ketinggian ayunan naik turun logo")]
    public float logoFloatAmplitude = 12f;

    public enum MenuState { Intro, MainMenu, LevelSelect, ControlsGuide, Transitioning }
    public MenuState CurrentState { get; private set; } = MenuState.Intro;

    private float _canvasWidth;
    private float _canvasHeight;
    private MenuAnimator _anim;
    private LevelSelectManager _levelSelect;
    private MenuNavigator _navigator;

    // Untuk melacak posisi dasar logo agar tidak bentrok dengan animasi geser intro
    private Vector2 _logoBasePosition;
    private bool _isLogoBaseCaptured = false;

    private void Awake()
    {
        _anim = GetComponent<MenuAnimator>();
        _levelSelect = GetComponent<LevelSelectManager>();
        _navigator = GetComponent<MenuNavigator>();

        // Jarak slide sekarang FIXED dari Inspector (verticalSlideDistance /
        // horizontalSlideDistance), BUKAN dihitung otomatis dari Canvas lagi.
        // Ini paling konsisten: berapapun ukuran Canvas / device, panel akan selalu
        // digeser sejauh angka yang kamu set manual, gak akan pernah "ketebak salah".
        _canvasWidth = horizontalSlideDistance;
        _canvasHeight = verticalSlideDistance;

        GroupOff(groupMainMenu);
        GroupOff(groupLevelSelect);
        GroupOff(groupControlsGuide);

        panelFade.alpha = 1f;
        panelFade.interactable = false;
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
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
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

        // Logo tetap slide horizontal seperti semula
        _anim.PrepareSlide(logo, offsetX: logoOffsetX);

        // Tombol sekarang center, jadi slide dari bawah ke atas (offset Y negatif)
        foreach (var btn in menuButtons)
            _anim.PrepareSlide(btn, new Vector2(0f, buttonOffsetY));

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

        // Kosongkan dulu isi Level Select (card, title, line, control hint)
        // supaya reveal sequence-nya mulai dari kondisi benar-benar blank
        _levelSelect.PrepareTransition();

        // Slide ke atas: Main Menu keluar, Level Select masuk dari bawah
        yield return StartCoroutine(_anim.SlideOutUp(groupMainMenu, panelTransDuration, _canvasHeight));
        GroupOff(groupMainMenu);
        _navigator.ResetButtonsVisual(); // aman direset sekarang, karena sudah invisible

        GroupOn(groupLevelSelect);
        yield return StartCoroutine(_anim.SlideInUp(groupLevelSelect, panelTransDuration, _canvasHeight));

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

        // OnExit() cuma matiin input (_isActive). Title/garis/hint/card SENGAJA
        // dibiarkan seperti terakhir kelihatan, biar ikut ke-slide turun bareng
        // panel — bukan lenyap instan sebelum panel sempat bergerak.
        _levelSelect.OnExit();

        // Slide ke bawah: Level Select keluar, Main Menu masuk dari atas
        yield return StartCoroutine(_anim.SlideOutDown(groupLevelSelect, panelTransDuration, _canvasHeight));
        GroupOff(groupLevelSelect);
        // Panel sudah invisible di sini — PrepareTransition() di Co_ToLevelSelect
        // nanti yang akan reset ulang (card hidden, title/garis/hint alpha 0)
        // sebelum reveal sequence berikutnya dimulai.

        GroupOn(groupMainMenu);

        // Aktifkan hover Main Menu di SINI agar tombol langsung terlihat aktif saat panel bergerak masuk
        _navigator.Activate();

        yield return StartCoroutine(_anim.SlideInDown(groupMainMenu, panelTransDuration, _canvasHeight));

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

    // ── Main Menu -> Credits (PLACEHOLDER) ─────────────────────────────────────

    /// <summary>
    /// Sementara belum ada panel Credits beneran — buat sekarang cuma buka
    /// developer console bawaan Unity buat kebutuhan debug/testing.
    /// TODO: ganti isi method ini kalau panel/scene Credits udah jadi.
    /// </summary>
    public void OnClickCredits()
    {
        if (CurrentState != MenuState.MainMenu) return;
        Debug.developerConsoleVisible = true;
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

    public void GroupOn(CanvasGroup g) { g.alpha = 1f; g.interactable = true; g.blocksRaycasts = true; }
    public void GroupOff(CanvasGroup g) { g.alpha = 0f; g.interactable = false; g.blocksRaycasts = false; }
}