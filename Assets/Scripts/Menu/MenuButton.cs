using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pasang di tiap tombol (Btn_Play, Btn_Settings, dst).
///
/// HIERARCHY per tombol (sesuai struktur yang sudah dipakai):
/// [Button_Play]                ← pasang MenuButton.cs di sini (root, ini yang di-scale saat selected)
///   ├── Icon                   ← Image, sprite ditukar normal/hover (instan, tidak fade)
///   ├── ButtonUI                ← Image, versi normal shape tombol
///   ├── ButtonHoverUI           ← Image, versi hover shape tombol
///   └── Text (TMP)             ← tidak disentuh script ini
///
/// AUTO-FIND: field iconImage / buttonNormalUI / buttonHoverUI dicari otomatis
/// berdasarkan nama child persis ("Icon", "ButtonUI", "ButtonHoverUI") kalau slotnya
/// dibiarkan kosong di Inspector.
///
/// TIDAK ADA FADE: ButtonUI/ButtonHoverUI/Icon langsung ditukar instan (alpha 0/1
/// langsung, bukan di-lerp). Feedback-nya justru dari animasi "pop" (scale membesar
/// dengan sedikit overshoot/mantul) saat tombol di-hover/dipilih.
/// </summary>
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("References (auto-find by child name kalau kosong)")]
    public Image iconImage;
    public CanvasGroup buttonNormalUI;   // child "ButtonUI"
    public CanvasGroup buttonHoverUI;    // child "ButtonHoverUI"

    [Header("Icon Sprites - Normal / Hover")]
    public Sprite iconNormalSprite;
    public Sprite iconHoverSprite;

    [Header("Pop Animation on Select")]
    [Tooltip("Skala tombol saat dipilih/hover (1 = ukuran normal)")]
    public float selectedScale = 1.1f;

    [Tooltip("Durasi animasi pop/mantul")]
    public float popDuration = 0.18f;

    [HideInInspector] public System.Action onHover;
    [HideInInspector] public System.Action onClick;

    // ── Internal ──────────────────────────────────────────────────────────────

    private RectTransform _rect;
    private Vector3 _originalScale;
    private Coroutine _popCoroutine;
    private bool _isSelected     = false;
    private bool _isInitialized  = false;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        ForceInitializeState(false);
    }

    /// <summary>
    /// Memastikan referensi RectTransform & child (Icon/ButtonUI/ButtonHoverUI) terisi.
    /// Auto-find dijalankan sekali saja, aman dipanggil berkali-kali.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_rect == null)
        {
            _rect = GetComponent<RectTransform>();
            _originalScale = _rect.localScale;
        }

        if (iconImage == null)
        {
            Transform t = transform.Find("Icon");
            if (t != null) iconImage = t.GetComponent<Image>();
        }

        if (buttonNormalUI == null)
        {
            Transform t = transform.Find("ButtonUI");
            if (t != null) buttonNormalUI = GetOrAddCanvasGroup(t);
        }

        if (buttonHoverUI == null)
        {
            Transform t = transform.Find("ButtonHoverUI");
            if (t != null) buttonHoverUI = GetOrAddCanvasGroup(t);
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(Transform t)
    {
        CanvasGroup cg = t.GetComponent<CanvasGroup>();
        if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
        // Layer visual saja, biar tidak rebutan raycast dengan root tombol
        cg.blocksRaycasts = false;
        cg.interactable   = false;
        return cg;
    }

    public void SetSelected(bool selected)
    {
        EnsureInitialized();

        if (_isInitialized && _isSelected == selected) return;

        _isSelected    = selected;
        _isInitialized = true;

        // Swap instan, tidak ada fade
        if (iconImage != null && iconNormalSprite != null && iconHoverSprite != null)
            iconImage.sprite = selected ? iconHoverSprite : iconNormalSprite;

        if (buttonNormalUI != null) buttonNormalUI.alpha = selected ? 0f : 1f;
        if (buttonHoverUI  != null) buttonHoverUI.alpha  = selected ? 1f : 0f;

        // Animasi hover: pop/mantul di scale root tombol
        if (_popCoroutine != null) StopCoroutine(_popCoroutine);
        _popCoroutine = StartCoroutine(Co_Pop(selected));
    }

    /// <summary>Reset ke tampilan normal (dipanggil saat navigator deactivate)</summary>
    public void ResetPosition()
    {
        if (_popCoroutine != null) StopCoroutine(_popCoroutine);
        ForceInitializeState(false);
    }

    /// <summary>Dipanggil ulang saat PrepareSlide (intro) supaya baseline scale tetap benar</summary>
    public void RefreshOriginalScale()
    {
        EnsureInitialized();
        _originalScale = _rect.localScale;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ForceInitializeState(bool selected)
    {
        EnsureInitialized();

        _isSelected    = selected;
        _isInitialized = true;

        if (iconImage != null && iconNormalSprite != null && iconHoverSprite != null)
            iconImage.sprite = selected ? iconHoverSprite : iconNormalSprite;

        if (buttonNormalUI != null) buttonNormalUI.alpha = selected ? 0f : 1f;
        if (buttonHoverUI  != null) buttonHoverUI.alpha  = selected ? 1f : 0f;

        _rect.localScale = selected ? _originalScale * selectedScale : _originalScale;
    }

    /// <summary>
    /// Animasi "pop": saat selected, tombol membesar dengan sedikit overshoot (ease-out-back)
    /// biar terasa mantul. Saat deselect, mengecil normal (ease-out-cubic).
    /// </summary>
    private IEnumerator Co_Pop(bool selected)
    {
        Vector3 startScale  = _rect.localScale;
        Vector3 targetScale = selected ? _originalScale * selectedScale : _originalScale;

        float e = 0f;
        while (e < popDuration)
        {
            // unscaledDeltaTime, BUKAN deltaTime — supaya animasi pop tetap jalan
            // meskipun Time.timeScale = 0 (dipakai pas Pause Menu aktif).
            e += Time.unscaledDeltaTime;
            float tRaw = Mathf.Clamp01(e / popDuration);
            float t = selected ? EaseOutBack(tRaw) : EaseOutCubic(tRaw);

            if (_rect != null)
                _rect.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);

            yield return null;
        }

        if (_rect != null)
            _rect.localScale = targetScale;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public void OnPointerEnter(PointerEventData eventData) => onHover?.Invoke();
    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke();
}