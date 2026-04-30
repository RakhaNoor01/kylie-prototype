using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // ── DialogueCanvas          ← GameObject ini (tempat script)
    //      └── DialoguePanel     ← dialoguePanel
    //            ├── DialogueBox ← dialogueBoxImage (Image)
    //            ├── NextIndicator ← indicator (RectTransform)
    //            ├── Player      ← rightRoot
    //            │     ├── Portrait  ← rightPortrait
    //            │     ├── Speaker   ← rightName
    //            │     └── Dialogue  ← rightText
    //            └── NPC         ← leftRoot
    //                  ├── Portrait  ← leftPortrait
    //                  ├── Speaker   ← leftName
    //                  └── Dialogue  ← leftText

    [Header("── DialoguePanel ──────────────────────")]
    public GameObject dialoguePanel;            // DialoguePanel
    public Image      dialogueBoxImage;         // DialoguePanel/DialogueBox → Image
    public Sprite     playerDialogueBoxSprite;  // sprite kotak dialog Player (kyliedialoguebox)

    [Header("── NextIndicator ──────────────────────")]
    public RectTransform npcIndicator;      // indicator di sisi NPC (kiri)
    public RectTransform playerIndicator;   // indicator di sisi Player (kanan)
    public float indicatorBobHeight = 10f;
    public float indicatorBobSpeed  = 3f;

    [Header("── NPC ────────────────────────────────")]
    public GameObject      leftRoot;        // NPC
    public Image           leftPortrait;    // NPC/Portrait
    public TextMeshProUGUI leftName;        // NPC/Speaker
    public TextMeshProUGUI leftText;        // NPC/Dialogue

    [Header("── Player ─────────────────────────────")]
    public GameObject      rightRoot;       // Player
    public Image           rightPortrait;   // Player/Portrait
    public TextMeshProUGUI rightName;       // Player/Speaker
    public TextMeshProUGUI rightText;       // Player/Dialogue
    [Tooltip("Portrait default player (fallback jika DialogueLine tidak set portrait)")]
    public Sprite          playerPortrait;

    [Header("── Player Movement ────────────────────")]
    public float unlockDelay = 0.5f;

    [Header("── Typing ──────────────────────────────")]
    public float typingSpeed     = 0.035f;
    public float charPopDuration = 0.14f;
    public float charPopScale    = 1.55f;

    [Header("── Panel Animation ────────────────────")]
    public float panelSlideOffset = 55f;
    public float panelInDuration  = 0.22f;
    public float panelOutDuration = 0.16f;

    // ── internal ─────────────────────────────────────────────────
    private RectTransform panelRect;
    private CanvasGroup   panelGroup;
    private Vector2       panelOriginPos;
    private Vector2       npcIndicatorOrigin;
    private Vector2       playerIndicatorOrigin;

    private DialogueData currentData;
    private Sprite       currentNpcPortrait;
    private Sprite       currentNpcBoxSprite;
    private Sprite       currentPlayerPortrait;
    private Action       onDialogueComplete;

    private int  lineIndex;
    private bool isTyping;
    private bool isActive;
    private bool isPanelAnimating;

    private Coroutine typingCoroutine;

    private Rigidbody2D            playerRb;
    private RigidbodyConstraints2D originalConstraints;

    // ── lifecycle ─────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Resolve komponen dari DialoguePanel
        panelRect  = dialoguePanel.GetComponent<RectTransform>();
        panelGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
            panelGroup = dialoguePanel.AddComponent<CanvasGroup>();

        panelOriginPos        = panelRect.anchoredPosition;
        if (npcIndicator != null)    npcIndicatorOrigin    = npcIndicator.anchoredPosition;
        if (playerIndicator != null) playerIndicatorOrigin = playerIndicator.anchoredPosition;

        dialoguePanel.SetActive(false);

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            playerRb = playerGo.GetComponent<Rigidbody2D>();
            if (playerRb != null)
                originalConstraints = playerRb.constraints;
        }
    }

    // ── public API ────────────────────────────────────────────────
    public bool IsDialogueActive => isActive;

    /// <param name="npcBoxSprite">Sprite kotak dialog milik NPC ini. Drag di inspector NPC.</param>
    public void StartDialogue(DialogueData data, Sprite npcPortrait,
                              Sprite npcBoxSprite = null, Action onComplete = null)
    {
        if (data == null || data.lines.Count == 0) return;

        currentData            = data;
        currentNpcPortrait     = npcPortrait;
        currentNpcBoxSprite    = npcBoxSprite;
        currentPlayerPortrait  = playerPortrait;   // portrait default player dari Inspector
        onDialogueComplete     = onComplete;

        lineIndex = 0;
        isActive  = true;

        LockPlayer();
        StartCoroutine(OpenPanel());
    }

    // ── panel animation ───────────────────────────────────────────
    private IEnumerator OpenPanel()
    {
        isPanelAnimating = true;

        // Set state awal SEBELUM SetActive agar tidak ada flash frame
        panelGroup.alpha           = 0f;
        panelRect.anchoredPosition = panelOriginPos - new Vector2(0, panelSlideOffset);
        dialoguePanel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < panelInDuration)
        {
            elapsed += Time.deltaTime;
            float ease = EaseOutCubic(Mathf.Clamp01(elapsed / panelInDuration));
            panelGroup.alpha           = ease;
            panelRect.anchoredPosition = Vector2.Lerp(
                panelOriginPos - new Vector2(0, panelSlideOffset),
                panelOriginPos, ease);
            yield return null;
        }

        panelGroup.alpha           = 1f;
        panelRect.anchoredPosition = panelOriginPos;
        isPanelAnimating           = false;

        ShowLine();
    }

    private IEnumerator ClosePanel()
    {
        isPanelAnimating = true;
        if (npcIndicator != null)    npcIndicator.gameObject.SetActive(false);
        if (playerIndicator != null) playerIndicator.gameObject.SetActive(false);

        float   elapsed  = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 endPos   = startPos - new Vector2(0, panelSlideOffset);

        while (elapsed < panelOutDuration)
        {
            elapsed += Time.deltaTime;
            float ease = EaseInCubic(Mathf.Clamp01(elapsed / panelOutDuration));
            panelGroup.alpha           = 1f - ease;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            yield return null;
        }

        dialoguePanel.SetActive(false);
        panelRect.anchoredPosition = panelOriginPos;
        isPanelAnimating           = false;

        onDialogueComplete?.Invoke();
        StartCoroutine(UnlockPlayer());
    }

    // ── line display ──────────────────────────────────────────────
    private void ShowLine()
    {
        // ── Stop coroutine lama & bersihkan kedua text field dulu ─
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        isTyping = false;

        leftText.text                  = "";
        rightText.text                 = "";
        leftText.maxVisibleCharacters  = 0;
        rightText.maxVisibleCharacters = 0;
        leftText.ForceMeshUpdate();
        rightText.ForceMeshUpdate();

        var  line  = currentData.lines[lineIndex];
        bool isNPC = line.speaker == Speaker.NPC;

        leftRoot.SetActive(isNPC);
        rightRoot.SetActive(!isNPC);

        // Swap sprite kotak dialog sesuai speaker
        if (dialogueBoxImage != null)
        {
            if (isNPC && currentNpcBoxSprite != null)
                dialogueBoxImage.sprite = currentNpcBoxSprite;
            else if (!isNPC && playerDialogueBoxSprite != null)
                dialogueBoxImage.sprite = playerDialogueBoxSprite;
        }

        if (isNPC)
        {
            leftName.text       = string.IsNullOrEmpty(currentData.npcDisplayName) ? "NPC" : currentData.npcDisplayName;
            leftPortrait.sprite = line.portrait != null ? line.portrait : currentNpcPortrait;
            leftText.text       = line.text;
            leftText.maxVisibleCharacters = 0;
            leftText.ForceMeshUpdate();
            StartTyping(leftText);
            MoveIndicator(true);
        }
        else
        {
            rightName.text       = string.IsNullOrEmpty(currentData.playerDisplayName) ? "You" : currentData.playerDisplayName;
            rightPortrait.sprite = line.portrait != null ? line.portrait : currentPlayerPortrait;
            rightText.text       = line.text;
            rightText.maxVisibleCharacters = 0;
            rightText.ForceMeshUpdate();
            StartTyping(rightText);
            MoveIndicator(false);
        }
    }

    // ── typing (Celeste style) ────────────────────────────────────
    private void StartTyping(TextMeshProUGUI target)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeCeleste(target));
    }

    private IEnumerator TypeCeleste(TextMeshProUGUI tmp)
    {
        isTyping = true;

        tmp.maxVisibleCharacters = 0;
        tmp.ForceMeshUpdate();
        int total = tmp.textInfo.characterCount;

        // originalVerts[i] = posisi layout asli 4 vertex karakter ke-i
        var originalVerts = new Dictionary<int, Vector3[]>();
        // popElapsed[i]    = waktu berjalan sejak karakter ke-i muncul
        var popElapsed = new Dictionary<int, float>();

        int   revealed      = 0;
        float timeSinceLast = typingSpeed; // langsung reveal karakter pertama

        while (revealed < total || popElapsed.Count > 0)
        {
            timeSinceLast += Time.deltaTime;

            // ── Reveal karakter baru ───────────────────────────
            bool newRevealed = false;
            while (timeSinceLast >= typingSpeed && revealed < total)
            {
                timeSinceLast -= typingSpeed;
                revealed++;
                tmp.maxVisibleCharacters = revealed;
                popElapsed[revealed - 1] = 0f;
                newRevealed = true;
            }

            if (newRevealed || popElapsed.Count > 0)
            {
                // ForceMeshUpdate reset semua vert ke posisi layout →
                // kita re-apply animasi pop dari originalVerts yang sudah tersimpan.
                tmp.ForceMeshUpdate();
                var textInfo = tmp.textInfo;

                // Simpan originalVerts untuk karakter yang baru pertama kali muncul
                foreach (int idx in new List<int>(popElapsed.Keys))
                {
                    if (originalVerts.ContainsKey(idx)) continue;
                    if (idx >= textInfo.characterCount) continue;
                    var ci = textInfo.characterInfo[idx];
                    if (!ci.isVisible) continue;

                    int vi  = ci.vertexIndex;
                    int mi  = ci.materialReferenceIndex;
                    var src = textInfo.meshInfo[mi].vertices;
                    originalVerts[idx] = new Vector3[] { src[vi], src[vi+1], src[vi+2], src[vi+3] };
                }

                // Terapkan pop scale ke semua karakter yang sedang animasi
                var toRemove = new List<int>();
                foreach (int idx in new List<int>(popElapsed.Keys))
                {
                    popElapsed[idx] += Time.deltaTime;
                    float t = Mathf.Clamp01(popElapsed[idx] / charPopDuration);

                    if (!originalVerts.ContainsKey(idx)) { if (t >= 1f) toRemove.Add(idx); continue; }

                    var ci = textInfo.characterInfo[idx];
                    if (!ci.isVisible) { if (t >= 1f) toRemove.Add(idx); continue; }

                    int      vi    = ci.vertexIndex;
                    int      mi    = ci.materialReferenceIndex;
                    Vector3[] verts = textInfo.meshInfo[mi].vertices;
                    Vector3[] orig  = originalVerts[idx];

                    float   scale  = Mathf.Lerp(charPopScale, 1f, EaseOutBack(t));
                    Vector3 center = (orig[0] + orig[1] + orig[2] + orig[3]) * 0.25f;

                    for (int v = 0; v < 4; v++)
                        verts[vi + v] = center + (orig[v] - center) * scale;

                    if (t >= 1f) toRemove.Add(idx);
                }

                foreach (int idx in toRemove) popElapsed.Remove(idx);
                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }

            yield return null;
        }

        isTyping = false;
    }

    // ── input ─────────────────────────────────────────────────────
    private void Update()
    {
        if (!isActive || isPanelAnimating) return;

        // NextIndicator bob
        float bobY = Mathf.Sin(Time.time * indicatorBobSpeed) * indicatorBobHeight;
        if (npcIndicator != null && npcIndicator.gameObject.activeSelf)
            npcIndicator.anchoredPosition = npcIndicatorOrigin + new Vector2(0, bobY);
        if (playerIndicator != null && playerIndicator.gameObject.activeSelf)
            playerIndicator.anchoredPosition = playerIndicatorOrigin + new Vector2(0, bobY);

        if (!Input.GetButtonDown("Submit")) return;

        if (isTyping)
            SkipTyping();
        else
            NextLine();
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = null;
        isTyping = false;

        leftText.maxVisibleCharacters  = int.MaxValue;
        rightText.maxVisibleCharacters = int.MaxValue;
        leftText.ForceMeshUpdate();
        rightText.ForceMeshUpdate();
    }

    private void NextLine()
    {
        lineIndex++;
        if (lineIndex < currentData.lines.Count)
            ShowLine();
        else
            EndDialogue();
    }

    private void EndDialogue()
    {
        isActive = false;
        StartCoroutine(ClosePanel());
    }

    // ── helpers ───────────────────────────────────────────────────
    private void MoveIndicator(bool isNPC)
    {
        // Tampilkan hanya indicator yang sesuai speaker, sembunyikan yang lain
        if (npcIndicator != null)    npcIndicator.gameObject.SetActive(isNPC);
        if (playerIndicator != null) playerIndicator.gameObject.SetActive(!isNPC);
    }

    private void LockPlayer()
    {
        if (playerRb == null) return;
        playerRb.linearVelocity = Vector2.zero;
        playerRb.constraints    = RigidbodyConstraints2D.FreezeAll;
    }

    private IEnumerator UnlockPlayer()
    {
        yield return new WaitForSeconds(unlockDelay);
        if (playerRb != null)
            playerRb.constraints = originalConstraints;
    }

    // ── easing ────────────────────────────────────────────────────
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t)  => t * t * t;
}