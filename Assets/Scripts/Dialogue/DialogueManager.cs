using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public static event Action OnDialogueStarted;
    public static event Action OnDialogueEnded;

    // ─────────────────────────────────────────────
    //  UI REFERENCES
    // ─────────────────────────────────────────────
    [Header("UI References - WAJIB DI ASSIGN SEMUA")]
    public GameObject dialoguePanel;
    public Image leftPortrait;
    public Image rightPortrait;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject nextIndicator;

    // ─────────────────────────────────────────────
    //  DEFAULT NAMES  (fallback jika DialogueData tidak isi nama)
    // ─────────────────────────────────────────────
    [Header("Default Names (Fallback)")]
    [Tooltip("Nama NPC fallback jika DialogueData.npcDisplayName kosong.")]
    public string defaultNpcName = "NPC";

    [Tooltip("Nama Player fallback jika DialogueData.playerDisplayName kosong.")]
    public string defaultPlayerName = "You";

    // ─────────────────────────────────────────────
    //  PLAYER PORTRAIT
    // ─────────────────────────────────────────────
    [Header("Player Portrait (Fallback)")]
    public Sprite playerPortraitSprite;

    // ─────────────────────────────────────────────
    //  PLAYER LOCK
    // ─────────────────────────────────────────────
    [Header("Player Lock")]
    public Rigidbody2D playerRigidbody;
    public float unlockDelay = 1f;

    // ─────────────────────────────────────────────
    //  TYPING ANIMATION
    // ─────────────────────────────────────────────
    [Header("Typing Animation")]
    [Tooltip("Kecepatan fade-in per huruf (detik)")]
    public float typingSpeed = 0.035f;

    // ─────────────────────────────────────────────
    //  AUDIO
    // ─────────────────────────────────────────────
    [Header("Voice Audio")]
    [Tooltip("AudioSource yang dipakai untuk memutar voice clip dari DialogueData.")]
    public AudioSource voiceAudioSource;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private RigidbodyConstraints2D originalConstraints;

    private DialogueData currentData;
    private Sprite currentNpcPortrait;
    private string resolvedNpcName;
    private string resolvedPlayerName;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private Action onDialogueComplete;

    private Coroutine currentAnimCoroutine;
    private Coroutine typingCoroutine;

    private Vector2 originalLeftPos;
    private Vector2 originalRightPos;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nextIndicator != null) nextIndicator.SetActive(false);

        if (playerRigidbody != null)
            originalConstraints = playerRigidbody.constraints;

        if (leftPortrait != null)  originalLeftPos  = leftPortrait.rectTransform.anchoredPosition;
        if (rightPortrait != null) originalRightPos = rightPortrait.rectTransform.anchoredPosition;

        // Buat AudioSource otomatis jika tidak di-assign
        if (voiceAudioSource == null)
            voiceAudioSource = gameObject.AddComponent<AudioSource>();
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Mulai dialogue.
    /// npcNameOverride → opsional, untuk override nama NPC dari script NPC.
    /// Nama di DialogueData lebih prioritas dari override ini.
    /// </summary>
    public void StartDialogue(
        DialogueData data,
        Sprite npcPortrait,
        Action onComplete = null,
        string npcNameOverride = null)
    {
        if (data == null || data.lines == null || data.lines.Count == 0) return;

        currentData       = data;
        currentNpcPortrait = npcPortrait;
        onDialogueComplete = onComplete;

        // Resolve nama: DialogueData > override parameter > default fallback
        resolvedNpcName    = !string.IsNullOrEmpty(data.npcDisplayName)    ? data.npcDisplayName    :
                             !string.IsNullOrEmpty(npcNameOverride)         ? npcNameOverride         :
                             defaultNpcName;

        resolvedPlayerName = !string.IsNullOrEmpty(data.playerDisplayName) ? data.playerDisplayName :
                             defaultPlayerName;

        currentLineIndex  = 0;
        isDialogueActive  = true;

        LockPlayer();
        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    public bool IsDialogueActive => isDialogueActive;

    // ─────────────────────────────────────────────
    //  INTERNAL – SHOW LINE
    // ─────────────────────────────────────────────
    private void ShowCurrentLine()
    {
        DialogueLine line = currentData.lines[currentLineIndex];

        bool npcIsSpeaking = line.speaker == Speaker.NPC;

        speakerNameText.text = npcIsSpeaking ? resolvedNpcName : resolvedPlayerName;

        Image speakerPortrait = npcIsSpeaking ? leftPortrait  : rightPortrait;
        Image stayPortrait    = npcIsSpeaking ? rightPortrait : leftPortrait;

        ResetPortraitTransform(leftPortrait);
        ResetPortraitTransform(rightPortrait);

        // Portrait yang sedang bicara
        Sprite speakerSprite = line.portrait != null
            ? line.portrait
            : (npcIsSpeaking ? currentNpcPortrait : playerPortraitSprite);
        if (speakerSprite != null) speakerPortrait.sprite = speakerSprite;

        // Portrait yang diam (stay)
        Sprite staySprite = npcIsSpeaking
            ? (currentData.playerStayPortrait ?? playerPortraitSprite)
            : (currentData.npcStayPortrait    ?? currentNpcPortrait);
        if (staySprite != null) stayPortrait.sprite = staySprite;

        // Animasi portrait untuk yang bicara
        if (currentAnimCoroutine != null) StopCoroutine(currentAnimCoroutine);
        switch (line.animType)
        {
            case PortraitAnimType.Bounce: currentAnimCoroutine = StartCoroutine(DoBounce(speakerPortrait)); break;
            case PortraitAnimType.Spin:   currentAnimCoroutine = StartCoroutine(DoSpin(speakerPortrait));   break;
            case PortraitAnimType.Flip:   currentAnimCoroutine = StartCoroutine(DoFlip(speakerPortrait));   break;
        }

        // Voice SFX
        PlayVoiceClip();

        // Typewriter fade-in
        dialogueText.text = line.text;
        dialogueText.ForceMeshUpdate();

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(FadeTypeText());
    }

    private void PlayVoiceClip()
    {
        if (voiceAudioSource == null || currentData.voiceClip == null) return;
        voiceAudioSource.Stop();
        voiceAudioSource.clip = currentData.voiceClip;
        voiceAudioSource.Play();
    }

    // ─────────────────────────────────────────────
    //  TYPEWRITER
    // ─────────────────────────────────────────────
    private IEnumerator FadeTypeText()
    {
        var textInfo = dialogueText.textInfo;
        int characterCount = textInfo.characterCount;

        for (int i = 0; i < characterCount; i++)
            SetCharacterAlpha(i, 0);
        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        nextIndicator.SetActive(false);

        for (int i = 0; i < characterCount; i++)
        {
            SetCharacterAlpha(i, 255);
            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return new WaitForSeconds(typingSpeed);
        }

        nextIndicator.SetActive(true);
        typingCoroutine = null;
    }

    private void SetCharacterAlpha(int charIndex, byte alpha)
    {
        if (charIndex >= dialogueText.textInfo.characterCount) return;
        var charInfo = dialogueText.textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex   = charInfo.vertexIndex;
        var colors = dialogueText.textInfo.meshInfo[materialIndex].colors32;

        colors[vertexIndex + 0].a = alpha;
        colors[vertexIndex + 1].a = alpha;
        colors[vertexIndex + 2].a = alpha;
        colors[vertexIndex + 3].a = alpha;
    }

    // ─────────────────────────────────────────────
    //  ADVANCE / END
    // ─────────────────────────────────────────────
    public void AdvanceDialogue()
    {
        if (!isDialogueActive) return;

        currentLineIndex++;
        if (currentLineIndex < currentData.lines.Count)
            ShowCurrentLine();
        else
            EndDialogue();
    }

    private void EndDialogue()
    {
        isDialogueActive = false;

        if (voiceAudioSource != null) voiceAudioSource.Stop();

        dialoguePanel.SetActive(false);
        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
        StartCoroutine(DelayedUnlockPlayer());
    }

    // ─────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (!isDialogueActive) return;
        if (!Input.GetButtonDown("Submit")) return;

        if (typingCoroutine != null)
        {
            // Skip → tampilkan semua huruf sekaligus
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            var textInfo = dialogueText.textInfo;
            for (int i = 0; i < textInfo.characterCount; i++)
                SetCharacterAlpha(i, 255);
            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            nextIndicator.SetActive(true);
        }
        else
        {
            nextIndicator.SetActive(false);
            AdvanceDialogue();
        }
    }

    // ─────────────────────────────────────────────
    //  PLAYER LOCK / UNLOCK
    // ─────────────────────────────────────────────
    private void LockPlayer()
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.constraints    = RigidbodyConstraints2D.FreezeAll;
        }
        OnDialogueStarted?.Invoke();
    }

    private IEnumerator DelayedUnlockPlayer()
    {
        yield return new WaitForSeconds(unlockDelay);
        if (playerRigidbody != null)
            playerRigidbody.constraints = originalConstraints;
        OnDialogueEnded?.Invoke();
    }

    // ─────────────────────────────────────────────
    //  PORTRAIT HELPERS
    // ─────────────────────────────────────────────
    private void ResetPortraitTransform(Image portrait)
    {
        if (portrait == null) return;
        portrait.rectTransform.localScale       = Vector3.one;
        portrait.rectTransform.rotation         = Quaternion.identity;
        portrait.rectTransform.anchoredPosition = (portrait == leftPortrait) ? originalLeftPos : originalRightPos;
    }

    private IEnumerator DoBounce(Image portrait)
    {
        if (portrait == null) yield break;
        RectTransform rt = portrait.rectTransform;
        Vector2 originalPos = rt.anchoredPosition;
        float jumpHeight  = 30f;
        float upDuration  = 0.12f;
        float downDuration = 0.18f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / upDuration;
            float eased = 1 - Mathf.Pow(1 - Mathf.Clamp01(t), 3f);
            rt.anchoredPosition = originalPos + Vector2.up * (jumpHeight * eased);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / downDuration;
            float eased = Mathf.Pow(Mathf.Clamp01(t), 2f);
            rt.anchoredPosition = Vector2.Lerp(originalPos + Vector2.up * jumpHeight, originalPos, eased);
            yield return null;
        }

        rt.anchoredPosition = originalPos;
    }

    private IEnumerator DoSpin(Image portrait)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            portrait.rectTransform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 360, Mathf.Clamp01(t)));
            yield return null;
        }
        portrait.rectTransform.rotation = Quaternion.identity;
    }

    private IEnumerator DoFlip(Image portrait)
    {
        Vector3 original = portrait.rectTransform.localScale;
        Vector3 flipped  = new Vector3(-original.x, original.y, original.z);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.15f;
            portrait.rectTransform.localScale = Vector3.Lerp(original, flipped, Mathf.Clamp01(t));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.15f;
            portrait.rectTransform.localScale = Vector3.Lerp(flipped, original, Mathf.Clamp01(t));
            yield return null;
        }
    }
}