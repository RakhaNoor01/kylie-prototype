using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Pasang di tiap tombol (Btn_Play, Btn_Controls, Btn_Exit).
/// </summary>
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("GameObject Arrow ◄")]
    public GameObject arrowIndicator;

    [Tooltip("CanvasGroup untuk background shape kuning (hover/selected)")]
    public CanvasGroup hoverShapeGroup;

    [Header("Shift on Select")]
    [Tooltip("Seberapa jauh tombol geser ke kanan saat dipilih (pixels)")]
    public float selectShiftX = 18f;

    [Tooltip("Durasi animasi geser dan fade shape")]
    public float shiftDuration = 0.15f;

    [HideInInspector] public System.Action onHover;
    [HideInInspector] public System.Action onClick;

    private RectTransform _rect;
    private Vector2 _homePos;
    private Coroutine _shiftCoroutine;
    private bool _isSelected = false;
    private bool _isInitialized = false;

    private void Awake()
    {
        // Memastikan inisialisasi lokal aman saat giliran Awake-nya jalan
        EnsureInitialized();
    }

    private void Start()
    {
        // Memastikan kondisi awal benar-benar bersih saat game dimulai (Fix Semua Panah Muncul)
        ForceInitializeState(false);
    }

    /// <summary>
    /// Fungsi pengaman untuk menjamin komponen RectTransform tidak null 
    /// meskipun dipanggil lebih awal oleh script lain (MenuNavigator).
    /// </summary>
    private void EnsureInitialized()
    {
        if (_rect == null)
        {
            _rect = GetComponent<RectTransform>();
            _homePos = _rect.anchoredPosition;
        }
    }

    public void SetSelected(bool selected)
    {
        EnsureInitialized();

        // Jika sudah dalam state yang sama DAN sudah terinisialisasi secara eksternal, lewati proses
        if (_isInitialized && _isSelected == selected) return;
        
        _isSelected = selected;
        _isInitialized = true;

        if (arrowIndicator != null)
            arrowIndicator.SetActive(selected);

        // Jalankan Animasi geser tombol + Fade In/Out Shape Kuning
        if (_shiftCoroutine != null) StopCoroutine(_shiftCoroutine);
        _shiftCoroutine = StartCoroutine(Co_Shift(selected));
    }

    /// <summary>Reset ke posisi home (dipanggil saat navigator deactivate)</summary>
    public void ResetPosition()
    {
        if (_shiftCoroutine != null) StopCoroutine(_shiftCoroutine);
        ForceInitializeState(false);
    }

    private void ForceInitializeState(bool selected)
    {
        EnsureInitialized();

        _isSelected = selected;
        _isInitialized = true;
        
        if (arrowIndicator != null) 
            arrowIndicator.SetActive(selected);
            
        if (hoverShapeGroup != null) 
            hoverShapeGroup.alpha = selected ? 1f : 0f;
            
        _rect.anchoredPosition = selected ? (_homePos + new Vector2(selectShiftX, 0f)) : _homePos;
    }

    private IEnumerator Co_Shift(bool shiftRight)
    {
        EnsureInitialized();

        Vector2 startPos = _rect.anchoredPosition;
        Vector2 targetPos = _homePos + (shiftRight ? new Vector2(selectShiftX, 0f) : Vector2.zero);

        float startAlpha = hoverShapeGroup != null ? hoverShapeGroup.alpha : 0f;
        float targetAlpha = shiftRight ? 1f : 0f;

        float e = 0f;
        while (e < shiftDuration)
        {
            e += Time.deltaTime;
            // Menggunakan Ease Out Cubic agar transisi terasa empuk
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / shiftDuration), 3f);
            
            if (_rect != null)
                _rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            
            if (hoverShapeGroup != null)
                hoverShapeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }
        
        if (_rect != null) 
            _rect.anchoredPosition = targetPos;
            
        if (hoverShapeGroup != null) 
            hoverShapeGroup.alpha = targetAlpha;
    }

    // Saat PrepareSlide dipanggil ulang (intro), update homePos
    public void RefreshHomePos()
    {
        EnsureInitialized();
        _homePos = _rect.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData) => onHover?.Invoke();
    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke();
}