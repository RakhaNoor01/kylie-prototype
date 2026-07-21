using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Cards")]
    public LevelCard cardForest;
    public LevelCard cardMountain;
    public LevelCard cardRuins;

    [Header("Selection Visual")]
    [Tooltip("Card yang dipilih membesar segini (dinaikkan biar kontrasnya kelihatan, samain kaya referensi)")]
    public float selectedScale   = 1.15f;
    [Tooltip("Card yang tidak dipilih mengecil segini")]
    public float unselectedScale = 0.85f;
    public float scaleDuration   = 0.2f;

    [Header("Reveal Sequence")]
    [Tooltip("CanvasGroup di teks 'LEVEL SELECT'")]
    public CanvasGroup titleGroup;

    [Tooltip("Image tali/garis putih di belakang portrait. Wajib di-set Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left di Inspector.")]
    public Image ropeLine;

    [Tooltip("CanvasGroup di sprite control_level.png (hint ENTER/Arrow/ESC)")]
    public CanvasGroup controlHintGroup;

    [Header("Reveal Timing")]
    public float titleFadeDuration   = 0.3f;
    public float cardPopDuration     = 0.35f;
    public float cardPopStagger      = 0.12f;
    public float lineDrawDuration    = 0.4f;
    public float controlFadeDuration = 0.25f;

    [Header("Level Scenes")]
    public string forestSceneName = "Level_Forest";
    public string mountainSceneName = "Level_Mountain";
    public string ruinsSceneName = "Level_Ruins";

    [Header("Level Loader")]
    public SoloLeveling soloLeveling;

    private int _selectedIndex = 0;
    private bool _isActive = false;
    private LevelCard[] _cards;

    private void Awake()
    {
        _cards = new LevelCard[] { cardForest, cardMountain, cardRuins };
        for (int i = 0; i < _cards.Length; i++)
            _cards[i].Setup(i, this);

        if (soloLeveling == null)
        {
            Debug.LogWarning("LevelSelectManager: SoloLeveling reference belum di-set di inspector.", this);
        }
        else
        {
            DontDestroyOnLoad(soloLeveling.gameObject);
        }
    }

    private void Update()
    {
        if (!_isActive) return;

        bool moved = false;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            _selectedIndex = Mathf.Min(_cards.Length - 1, _selectedIndex + 1);
            moved = true;
        }

        if (moved) UpdateSelection(animate: true);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            ConfirmSelection();
    }

    /// <summary>
    /// Sembunyikan semua elemen (card, title, line, control hint) sebelum panel Level Select
    /// muncul, supaya reveal sequence di OnEnter() mulai dari kondisi benar-benar kosong.
    /// </summary>
    public void PrepareTransition()
    {
        _selectedIndex = 0;
        foreach (var card in _cards) card.PrepareHidden();

        if (titleGroup != null) titleGroup.alpha = 0f;
        if (ropeLine != null) ropeLine.fillAmount = 0f;
        if (controlHintGroup != null) controlHintGroup.alpha = 0f;
    }

    public void OnEnter()
    {
        _selectedIndex = 0;
        StartCoroutine(Co_RevealSequence());
        // _isActive baru di-set true di akhir Co_RevealSequence(), setelah semua elemen
        // (title, card, garis, hint) selesai muncul. Kalau di-set true dari awal, pemain
        // bisa keburu pencet Enter/panah di tengah animasi pop-in dan langsung load level
        // sebelum card-nya kelihatan penuh.
    }

    /// <summary>
    /// Dipanggil MainMenuManager.Co_BackFromLevelSelect() SEBELUM slide-out mulai.
    /// Cuma matiin input di sini — visual (title/garis/hint/card) SENGAJA tidak
    /// direset instan, biar mereka ikut ke-slide keluar bareng panel apa adanya.
    /// Kalau direset di sini (alpha = 0 dsb), title/garis/hint bakal lenyap dalam
    /// 1 frame SEBELUM panel sempat bergerak — itu yang bikin efek "ngeblink".
    /// Reset yang sebenarnya sudah ditangani PrepareTransition() saat panel ini
    /// dibuka lagi nanti, dan itu terjadi saat panel masih invisible jadi aman.
    /// </summary>
    public void OnExit()
    {
        _isActive = false;
    }

    public void OnCardHover(int index)
    {
        if (!_isActive) return;
        _selectedIndex = index;
        UpdateSelection(animate: true);
    }

    public void OnCardClick(int index)
    {
        if (!_isActive) return;
        _isActive = false;
        _selectedIndex = index;
        ConfirmSelection();
    }

    public void SelectForest()  => OnCardClick(0);
    public void SelectMountain() => OnCardClick(1);
    public void SelectRuins()    => OnCardClick(2);
    public void LoadLevelByName(string sceneName) => soloLeveling?.LoadLevel(sceneName);

    // ── Reveal Sequence ───────────────────────────────────────────────────────
    // Urutan: judul "LEVEL SELECT" → portrait card (staggered) → garis putih
    // menyambung kiri→kanan → control hint (ENTER/Arrow/ESC) muncul terakhir.

    private IEnumerator Co_RevealSequence()
    {
        if (titleGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(titleGroup, 0f, 1f, titleFadeDuration));

        for (int i = 0; i < _cards.Length; i++)
        {
            bool selected = i == _selectedIndex;
            _cards[i].PopIn(selected, selectedScale, unselectedScale, cardPopDuration);
            yield return new WaitForSeconds(cardPopStagger);
        }
        yield return new WaitForSeconds(cardPopDuration);

        if (ropeLine != null)
            yield return StartCoroutine(DrawLine(ropeLine, lineDrawDuration));

        if (controlHintGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(controlHintGroup, 0f, 1f, controlFadeDuration));

        _isActive = true;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup g, float from, float to, float duration)
    {
        g.alpha = from;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(e / duration));
            yield return null;
        }
        g.alpha = to;
    }

    /// <summary>
    /// Menganimasikan Image bertipe Filled/Horizontal dari kosong ke penuh (kiri→kanan),
    /// jadi tali/garis putihnya "tergambar" progresif sesuai posisi X sprite-nya, bukan
    /// langsung nongol semua.
    /// </summary>
    private IEnumerator DrawLine(Image line, float duration)
    {
        line.fillAmount = 0f;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            line.fillAmount = Mathf.Clamp01(e / duration);
            yield return null;
        }
        line.fillAmount = 1f;
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void UpdateSelection(bool animate)
    {
        for (int i = 0; i < _cards.Length; i++)
            _cards[i].SetSelected(i == _selectedIndex, selectedScale, unselectedScale, scaleDuration, animate);
    }

    private void ConfirmSelection()
    {
        if (soloLeveling == null)
        {
            Debug.LogWarning("SoloLeveling reference is missing on LevelSelectManager.");
            return;
        }

        switch (_selectedIndex)
        {
            case 0: soloLeveling.LoadLevel(forestSceneName);   break;
            case 1: soloLeveling.LoadLevel(mountainSceneName); break;
            case 2: soloLeveling.LoadLevel(ruinsSceneName);    break;
        }
    }
}