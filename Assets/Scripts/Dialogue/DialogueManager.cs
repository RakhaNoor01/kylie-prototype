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

    [Header("UI References - WAJIB DI ASSIGN SEMUA")]
    public GameObject dialoguePanel;
    public Image leftPortrait;
    public Image rightPortrait;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject nextIndicator;

    [Header("Player Portrait (fallback)")]
    public Sprite playerPortraitSprite;
    public string playerDisplayName = "You";

    [Header("Player Lock")]
    public Rigidbody2D playerRigidbody;
    public float unlockDelay = 1f;

    [Header("Typing Animation")]
    [Tooltip("Kecepatan fade-in per huruf (detik)")]
    public float typingSpeed = 0.035f;

    private RigidbodyConstraints2D originalConstraints;

    private DialogueData currentData;
    private string currentNpcName;
    private Sprite currentNpcPortrait;
    private Sprite currentNpcStayPortrait;
    private Sprite currentPlayerStayPortrait;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private Action onDialogueComplete;

    private Coroutine currentAnimCoroutine;
    private Coroutine typingCoroutine;

    private Vector2 originalLeftPos;
    private Vector2 originalRightPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nextIndicator != null) nextIndicator.SetActive(false);

        if (playerRigidbody != null)
            originalConstraints = playerRigidbody.constraints;

        if (leftPortrait != null) originalLeftPos = leftPortrait.rectTransform.anchoredPosition;
        if (rightPortrait != null) originalRightPos = rightPortrait.rectTransform.anchoredPosition;
    }

    public void StartDialogue(DialogueData data, string npcName, Sprite npcPortrait, Action onComplete = null)
    {
        if (data == null || data.lines.Count == 0) return;

        currentData = data;
        currentNpcName = npcName;
        currentNpcPortrait = npcPortrait;
        currentNpcStayPortrait = data.npcStayPortrait;
        currentPlayerStayPortrait = data.playerStayPortrait;

        currentLineIndex = 0;
        isDialogueActive = true;
        onDialogueComplete = onComplete;

        LockPlayer();
        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = currentData.lines[currentLineIndex];

        speakerNameText.text = line.speaker == Speaker.NPC ? currentNpcName : playerDisplayName;

        Image speakerPortrait = line.speaker == Speaker.NPC ? leftPortrait : rightPortrait;
        Image stayPortrait    = line.speaker == Speaker.NPC ? rightPortrait : leftPortrait;

        ResetPortraitTransform(leftPortrait);
        ResetPortraitTransform(rightPortrait);

        // Speaker portrait
        Sprite speakerSprite = line.portrait != null 
            ? line.portrait 
            : (line.speaker == Speaker.NPC ? currentNpcPortrait : playerPortraitSprite);
        if (speakerSprite != null) speakerPortrait.sprite = speakerSprite;

        // Stay portrait
        Sprite staySprite = line.speaker == Speaker.NPC 
            ? (currentPlayerStayPortrait ?? playerPortraitSprite)
            : (currentNpcStayPortrait ?? currentNpcPortrait);
        if (staySprite != null) stayPortrait.sprite = staySprite;

        // Portrait animation hanya untuk yang bicara
        if (currentAnimCoroutine != null) StopCoroutine(currentAnimCoroutine);
        switch (line.animType)
        {
            case PortraitAnimType.Bounce: currentAnimCoroutine = StartCoroutine(DoBounce(speakerPortrait)); break;
            case PortraitAnimType.Spin:   currentAnimCoroutine = StartCoroutine(DoSpin(speakerPortrait)); break;
            case PortraitAnimType.Flip:   currentAnimCoroutine = StartCoroutine(DoFlip(speakerPortrait)); break;
        }

        // ── FADE-IN TYPEWRITER (satu per satu dari atas) ──
        dialogueText.text = line.text;
        dialogueText.ForceMeshUpdate();

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(FadeTypeText());
    }

    private IEnumerator FadeTypeText()
    {
        var textInfo = dialogueText.textInfo;
        int characterCount = textInfo.characterCount;

        // Reset semua huruf ke transparan dulu
        for (int i = 0; i < characterCount; i++)
            SetCharacterAlpha(i, 0);

        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        nextIndicator.SetActive(false);

        // Fade in satu per satu
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
        int vertexIndex = charInfo.vertexIndex;
        var colors = dialogueText.textInfo.meshInfo[materialIndex].colors32;

        colors[vertexIndex + 0].a = alpha;
        colors[vertexIndex + 1].a = alpha;
        colors[vertexIndex + 2].a = alpha;
        colors[vertexIndex + 3].a = alpha;
    }

    private void ResetPortraitTransform(Image portrait)
    {
        if (portrait == null) return;
        portrait.rectTransform.localScale = Vector3.one;
        portrait.rectTransform.rotation = Quaternion.identity;
        portrait.rectTransform.anchoredPosition = (portrait == leftPortrait) ? originalLeftPos : originalRightPos;
    }

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
        dialoguePanel.SetActive(false);
        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
        StartCoroutine(DelayedUnlockPlayer());
    }

    private void LockPlayer()
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
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

    private IEnumerator DoBounce(Image portrait)
    {
        if (portrait == null) yield break;
        RectTransform rt = portrait.rectTransform;
        Vector2 originalPos = rt.anchoredPosition;
        float jumpHeight = 30f;
        float upDuration = 0.12f;
        float downDuration = 0.18f;

        float t = 0f;
        while (t < 1f) { t += Time.deltaTime / upDuration; float eased = 1 - Mathf.Pow(1 - t, 3f); rt.anchoredPosition = originalPos + Vector2.up * (jumpHeight * eased); yield return null; }
        t = 0f;
        while (t < 1f) { t += Time.deltaTime / downDuration; float eased = Mathf.Pow(t, 2f); rt.anchoredPosition = Vector2.Lerp(originalPos + Vector2.up * jumpHeight, originalPos, eased); yield return null; }
        rt.anchoredPosition = originalPos;
    }

    private IEnumerator DoSpin(Image portrait)
    {
        float t = 0;
        while (t < 1) { t += Time.deltaTime / 0.35f; portrait.rectTransform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 360, t)); yield return null; }
        portrait.rectTransform.rotation = Quaternion.identity;
    }

    private IEnumerator DoFlip(Image portrait)
    {
        Vector3 original = portrait.rectTransform.localScale;
        float t = 0;
        while (t < 1) { t += Time.deltaTime / 0.15f; portrait.rectTransform.localScale = Vector3.Lerp(original, new Vector3(-original.x, original.y, original.z), t); yield return null; }
        t = 0;
        while (t < 1) { t += Time.deltaTime / 0.15f; portrait.rectTransform.localScale = Vector3.Lerp(new Vector3(-original.x, original.y, original.z), original, t); yield return null; }
    }

    private void Update()
    {
        if (isDialogueActive && Input.GetButtonDown("Submit"))
        {
            if (typingCoroutine != null)
            {
                // SKIP → langsung full opaque
                StopCoroutine(typingCoroutine);
                var textInfo = dialogueText.textInfo;
                for (int i = 0; i < textInfo.characterCount; i++)
                    SetCharacterAlpha(i, 255);
                dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                typingCoroutine = null;
                nextIndicator.SetActive(true);
            }
            else
            {
                nextIndicator.SetActive(false);
                AdvanceDialogue();
            }
        }
    }

    public bool IsDialogueActive => isDialogueActive;
}