using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AUDIO MANAGER MENU - AUTO DETECT VERSION
/// - Otomatis cari semua tombol & card di scene
/// - GAK PERLU setup EventTrigger manual
/// - CUKUP TARUH 1 SCRIPT di Canvas, beres!
/// </summary>
public class MenuAudioManager : MonoBehaviour
{
    // ======================== SINGLETON ========================
    public static MenuAudioManager Instance { get; private set; }

    // ======================== REFERENSI ========================
    [Header("=== AUDIO SOURCE ===")]
    [SerializeField] private AudioSource _sfxSource;

    [Header("=== SFX CLIPS ===")]
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip cardHoverClip;
    [SerializeField] private AudioClip cardClickClip;
    [SerializeField] private AudioClip cardPopInClip;

    [Header("=== SETTINGS ===")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private bool autoDetectOnStart = true;
    [SerializeField] private float scanDelay = 0.1f;

    // ======================== PRIVATE ========================
    private List<Button> _buttons = new List<Button>();
    private List<LevelCard> _cards = new List<LevelCard>();
    private bool _isInitialized;
    private Coroutine _popInCoroutine;

    // ======================== LIFECYCLE ========================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        if (autoDetectOnStart)
        {
            Invoke(nameof(ScanAndSubscribe), scanDelay);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnsubscribeAll();
    }

    private void OnDisable()
    {
        if (_popInCoroutine != null)
        {
            StopCoroutine(_popInCoroutine);
            _popInCoroutine = null;
        }
    }

    // ======================== SCAN & SUBSCRIBE ========================

    [ContextMenu("Scan & Subscribe")]
    public void ScanAndSubscribe()
    {
        if (_isInitialized) return;

        UnsubscribeAll();
        _buttons.Clear();
        _cards.Clear();

        // Cari SEMUA tombol di scene
        Button[] foundButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _buttons.AddRange(foundButtons);

        // Cari SEMUA LevelCard di scene
        LevelCard[] foundCards = FindObjectsByType<LevelCard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _cards.AddRange(foundCards);

        Debug.Log($"[MenuAudioManager] Menemukan {_buttons.Count} tombol dan {_cards.Count} card.");

        // Subscribe ke semua tombol
        foreach (var btn in _buttons)
        {
            if (btn == null) continue;
            SubscribeButton(btn);
        }

        // Subscribe ke semua card
        foreach (var card in _cards)
        {
            if (card == null) continue;
            SubscribeCard(card);
        }

        // Subscribe ke LevelSelectManager untuk card pop in
        LevelSelectManager levelSelect = FindObjectOfType<LevelSelectManager>();
        if (levelSelect != null)
        {
            SubscribeLevelSelect(levelSelect);
        }

        _isInitialized = true;
    }

    // ======================== SUBSCRIBE BUTTON ========================

    private void SubscribeButton(Button btn)
    {
        if (btn == null) return;

        // 🔥 FIX: Pake EventTrigger untuk semua event (termasuk click)
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = btn.gameObject.AddComponent<EventTrigger>();
        }

        // Hover
        var entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => PlayButtonHover());
        trigger.triggers.Add(entryEnter);

        // Click (pakai EventTrigger, bukan onClick bawaan)
        var entryClick = new EventTrigger.Entry();
        entryClick.eventID = EventTriggerType.PointerClick;
        entryClick.callback.AddListener((data) => PlayButtonClick());
        trigger.triggers.Add(entryClick);
    }

    // ======================== SUBSCRIBE CARD ========================

    private void SubscribeCard(LevelCard card)
    {
        if (card == null) return;

        EventTrigger trigger = card.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = card.gameObject.AddComponent<EventTrigger>();
        }

        // Hover
        var entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => PlayCardHover());
        trigger.triggers.Add(entryEnter);

        // Click
        var entryClick = new EventTrigger.Entry();
        entryClick.eventID = EventTriggerType.PointerClick;
        entryClick.callback.AddListener((data) => PlayCardClick());
        trigger.triggers.Add(entryClick);
    }

    // ======================== SUBSCRIBE LEVEL SELECT (Card Pop In) ========================

    private void SubscribeLevelSelect(LevelSelectManager manager)
    {
        if (manager == null) return;

        if (_popInCoroutine != null)
        {
            StopCoroutine(_popInCoroutine);
        }
        _popInCoroutine = StartCoroutine(Co_MonitorCardPopIn(manager));
    }

    private IEnumerator Co_MonitorCardPopIn(LevelSelectManager manager)
    {
        // Ambil _cards dari LevelSelectManager pake reflection
        var field = typeof(LevelSelectManager).GetField("_cards",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field == null)
        {
            Debug.LogWarning("[MenuAudioManager] Cannot find _cards field in LevelSelectManager!");
            yield break;
        }

        LevelCard[] cards = field.GetValue(manager) as LevelCard[];
        if (cards == null || cards.Length == 0)
        {
            Debug.LogWarning("[MenuAudioManager] No cards found in LevelSelectManager!");
            yield break;
        }

        float[] lastScales = new float[cards.Length];

        while (true)
        {
            yield return new WaitForSeconds(0.05f);

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;

                float currentScale = cards[i].transform.localScale.magnitude;

                // Scale naik drastis = pop in
                if (lastScales[i] < 0.1f && currentScale > 0.5f)
                {
                    PlayCardPopIn();
                }

                lastScales[i] = currentScale;
            }
        }
    }

    // ======================== UNSUBSCRIBE ========================

    private void UnsubscribeAll()
    {
        // Hapus semua EventTrigger yang ditambahkan
        foreach (var btn in _buttons)
        {
            if (btn == null) continue;
            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                // Hanya hapus yang kita tambahin (biar ga ngerusak trigger lain)
                trigger.triggers.Clear();
            }
        }

        foreach (var card in _cards)
        {
            if (card == null) continue;
            var trigger = card.gameObject.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
            }
        }

        if (_popInCoroutine != null)
        {
            StopCoroutine(_popInCoroutine);
            _popInCoroutine = null;
        }

        _buttons.Clear();
        _cards.Clear();
        _isInitialized = false;
    }

    // ======================== PUBLIC METHODS ========================

    public void PlayButtonHover()
    {
        Debug.Log("[MenuAudioManager] PlayButtonHover");
        PlayOneShot(buttonHoverClip);
    }

    public void PlayButtonClick()
    {
        Debug.Log("[MenuAudioManager] PlayButtonClick");
        PlayOneShot(buttonClickClip);
    }

    public void PlayCardHover()
    {
        Debug.Log("[MenuAudioManager] PlayCardHover");
        PlayOneShot(cardHoverClip);
    }

    public void PlayCardClick()
    {
        Debug.Log("[MenuAudioManager] PlayCardClick");
        PlayOneShot(cardClickClip);
    }

    public void PlayCardPopIn()
    {
        Debug.Log("[MenuAudioManager] PlayCardPopIn");
        PlayOneShot(cardPopInClip);
    }

    // ======================== HELPER ========================

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || _sfxSource == null)
        {
            Debug.LogWarning($"[MenuAudioManager] Clip atau AudioSource null! Clip: {clip?.name}");
            return;
        }
        _sfxSource.pitch = Random.Range(minPitch, maxPitch);
        _sfxSource.PlayOneShot(clip);
    }

    // ======================== REPAIR ========================

    [ContextMenu("Repair - Scan Ulang")]
    public void Repair()
    {
        _isInitialized = false;
        ScanAndSubscribe();
    }
}