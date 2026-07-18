using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TarodevController;

/// <summary>
/// Mengatur sequence end credits: freeze player → BG hitam fade in →
/// tiap gambar credit fade in/out satu-satu → loading screen → balik ke Main Menu.
///
/// HIERARCHY SETUP (taruh di scene gameplay, atau di Persistent kalau mau reusable):
/// [EndCredits]
///   ├── EndCreditsController.cs
///   └── Canvas (Screen Space Overlay, Sort Order TINGGI, misal 998)
///         ├── BG_Black        ← Image fullscreen hitam, CanvasGroup, alpha 0 default
///         └── CreditImages     ← Parent kosong, isi semua Image credit sebagai child
///               ├── Credit_01  ← Image "THANK YOU FOR PLAYING", CanvasGroup, alpha 0
///               ├── Credit_02  ← Image "DIRECTED BY...", CanvasGroup, alpha 0
///               ├── Credit_03  ← dst...
///               ├── Credit_04
///               └── Credit_05
///
/// Player butuh tag "Player" dan komponen PlayerController untuk bisa di-disable.
/// </summary>
public class EndCreditsController : MonoBehaviour
{
    [Header("Background")]
    [Tooltip("CanvasGroup BG hitam fullscreen — fade in duluan sebelum credit images muncul")]
    public CanvasGroup bgBlack;

    [Header("Credit Images (urutan sesuai array = urutan tampil)")]
    [Tooltip("CanvasGroup tiap gambar credit, urutan sesuai yang mau ditampilkan")]
    public CanvasGroup[] creditImages;

    [Header("Timing")]
    [Tooltip("Durasi fade in BG hitam di awal")]
    public float bgFadeDuration = 0.6f;

    [Tooltip("Durasi fade in tiap gambar credit")]
    public float imageFadeInDuration = 0.8f;

    [Tooltip("Berapa lama tiap gambar credit ditampilkan penuh (alpha=1) sebelum fade out")]
    public float imageHoldDuration = 2f;

    [Tooltip("Durasi fade out tiap gambar credit")]
    public float imageFadeOutDuration = 0.8f;

    [Tooltip("Delay setelah gambar terakhir fade out, sebelum pindah ke Main Menu")]
    public float delayBeforeReturn = 0.3f;

    [Header("Scene")]
    [Tooltip("Nama scene Main Menu untuk kembali setelah credits selesai")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Player Reference (opsional, auto-find lewat tag kalau kosong)")]
    [Tooltip("Drag GameObject Player (yang punya PlayerController.cs) di sini. " +
             "Kalau kosong, akan auto-cari lewat tag 'Player'")]
    public PlayerController playerController;

    private bool _started = false;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Dipanggil dari EndCreditsTrigger saat player masuk room terakhir</summary>
    public void StartCredits()
    {
        if (_started) return;
        _started = true;

        StartCoroutine(Co_PlaySequence());
    }

    // ── Sequence ──────────────────────────────────────────────────────────────

    private IEnumerator Co_PlaySequence()
    {
        // 1) Freeze player (disable komponen controller-nya saja, bukan timeScale)
        FreezePlayer();

        // 2) BG hitam fade in dulu — supaya transisi antar gambar nanti
        //    tidak "blink" (selalu ada hitam solid di belakang)
        yield return StartCoroutine(FadeCanvasGroup(bgBlack, 0f, 1f, bgFadeDuration));

        // 3) Tiap gambar credit: fade in → hold → fade out, satu per satu
        foreach (var img in creditImages)
        {
            if (img == null) continue;

            yield return StartCoroutine(FadeCanvasGroup(img, 0f, 1f, imageFadeInDuration));
            yield return new WaitForSeconds(imageHoldDuration);
            yield return StartCoroutine(FadeCanvasGroup(img, 1f, 0f, imageFadeOutDuration));
        }

        // 4) Delay kecil di BG hitam polos sebelum pindah scene
        yield return new WaitForSeconds(delayBeforeReturn);

        // 5) Tampilkan loading screen (BG sudah hitam dari bgBlack, ShowInstant supaya
        //    tidak ada flicker saat transisi ke LoadingScreenController)
        if (LoadingScreenController.Instance != null)
            LoadingScreenController.Instance.ShowInstant();

        // 6) Pindah ke Main Menu.
        //    NOTE: pakai SceneManager standar di sini (bukan SoloLeveling) karena
        //    kita BALIK ke Main Menu, bukan masuk ke level — tidak perlu additive
        //    scene Persistent lagi. Sesuaikan jika alur kembali kamu berbeda.
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void FreezePlayer()
    {
        PlayerController controller = playerController;

        if (controller == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                controller = playerObj.GetComponent<PlayerController>();
        }

        if (controller != null)
            controller.enabled = false;
        else
            Debug.LogWarning("[EndCreditsController] PlayerController tidak ditemukan untuk di-freeze.");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        cg.alpha = from;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(e / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}