using System.Collections;
using UnityEngine;

/// <summary>
/// Loading screen reusable — survive antar scene, dipakai untuk:
///   1) Main Menu → Gameplay (saat pilih level)
///   2) Gameplay → Main Menu (saat keluar/quit ke menu)
///
/// HIERARCHY SETUP:
/// [LoadingScreen]                  ← GameObject terpisah, taruh di scene Main Menu
///   ├── LoadingScreenController.cs ← script ini
///   └── Canvas (Screen Space Overlay, Sort Order TINGGI misal 999)
///         ├── BG_Black             ← Image fullscreen, warna hitam solid, alpha 1
///         └── Spinner              ← Image kecil pojok kanan bawah (ikon loading)
///                                     Anchor: bottom-right
///
/// CARA PAKAI dari script lain (misal MainMenuManager):
///   LoadingScreenController.Instance.Show();
///   ... lakukan proses loading (AsyncOperation, dsb) ...
///   LoadingScreenController.Instance.Hide();
///
/// Singleton + DontDestroyOnLoad sendiri — TIDAK bergantung pada PersistOnLoad
/// atau SoloLeveling, supaya bisa dipanggil dari scene manapun (menu atau gameplay).
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance { get; private set; }

    [Header("References")]
    [Tooltip("Root Canvas/GameObject yang berisi BG hitam + spinner. Akan di-aktif/non-aktifkan.")]
    public GameObject root;

    [Tooltip("RectTransform spinner — yang akan diputar terus saat loading screen aktif")]
    public RectTransform spinner;

    [Header("Settings")]
    [Tooltip("Kecepatan putar spinner (derajat per detik)")]
    public float spinSpeed = 220f;

    [Tooltip("Durasi fade in/out BG hitam (detik)")]
    public float fadeDuration = 0.25f;

    private CanvasGroup _canvasGroup;
    private Coroutine _spinCoroutine;
    private Coroutine _fadeCoroutine;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton guard — kalau sudah ada instance (misal balik dari scene lain
        // yang juga punya LoadingScreen), hancurkan duplikat ini.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (root != null)
        {
            _canvasGroup = root.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = root.AddComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
            root.SetActive(false);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Tampilkan loading screen (fade in BG hitam, mulai putar spinner)</summary>
    public IEnumerator Show()
    {
        if (root == null) yield break;

        root.SetActive(true);

        if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
        _spinCoroutine = StartCoroutine(Co_Spin());

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        yield return StartCoroutine(Co_Fade(0f, 1f));
    }

    /// <summary>Sembunyikan loading screen (fade out, stop spinner)</summary>
    public IEnumerator Hide()
    {
        if (root == null) yield break;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        yield return StartCoroutine(Co_Fade(1f, 0f));

        if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);

        root.SetActive(false);
    }

    /// <summary>Tampilkan instant tanpa fade (untuk kondisi darurat / langsung butuh hitam)</summary>
    public void ShowInstant()
    {
        if (root == null) return;
        root.SetActive(true);
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
        _spinCoroutine = StartCoroutine(Co_Spin());
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private IEnumerator Co_Fade(float from, float to)
    {
        if (_canvasGroup == null) yield break;

        _canvasGroup.alpha = from;
        float e = 0f;
        while (e < fadeDuration)
        {
            e += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(e / fadeDuration));
            yield return null;
        }
        _canvasGroup.alpha = to;
    }

    private IEnumerator Co_Spin()
    {
        if (spinner == null) yield break;

        while (true)
        {
            spinner.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
            yield return null;
        }
    }
}