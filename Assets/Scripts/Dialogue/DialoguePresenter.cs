using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using TarodevController;

/// <summary>
/// Dialogue Presenter kustom ala Celeste, cocok dengan hierarchy:
///
/// DialogueCanvas
/// └─ DialoguePanel
///    ├─ DialogueBox
///    ├─ Player (Portrait, Speaker, Dialogue, NextIndicator)
///    └─ NPC    (Portrait, Dialogue, Speaker, NextIndicator)
///
/// PENTING soal hierarchy: "Player" dan "NPC" itu SIBLING dari
/// "DialogueBox" (sama-sama anak "DialoguePanel"), BUKAN anak dari
/// "DialogueBox". Makanya menggeser RectTransform DialogueBox saja
/// TIDAK ikut menggeser Player/NPC. Reparenting Player/NPC ke dalam
/// DialogueBox juga sudah dicoba dan bikin layout bentrok/rusak
/// (portrait & text saling tindih), jadi hierarchy dibiarkan seperti
/// semula.
///
/// FIX TRANSISI (final):
/// Bukan fade in/out, tapi Player/NPC digeser (anchoredPosition) BARENGAN
/// dengan box & kamera, pakai jarak (dropDistance) yang sama kayak box,
/// jadi keliatan seolah-olah dia "nempel" ikut box padahal secara
/// hierarchy terpisah:
/// 1. OnDialogueStartedAsync cuma freeze player, TIDAK animasi apa-apa.
/// 2. Baris pertama masuk (RunLineAsync) -> konten panel yang benar
///    (portrait/nama/ekspresi) di-set DULU, posisi panel dipaksa ke titik
///    "hidden" (sejajar posisi box waktu tersembunyi).
/// 3. Baru box + kamera + posisi panel dianimasikan BARENGAN (WhenAll)
///    dari hidden -> shown, jadi Player/NPC keliatan ikut geser turun
///    bareng box, bukan pop / fade.
/// 4. Pas keluar, panel aktif ikut digeser balik ke posisi hidden bareng
///    box slide-up & kamera zoom-out, baru semuanya di-nonaktifin.
/// 5. Kalau cuma ganti pembicara (Player <-> NPC) TANPA nutup box (sesi
///    dialog masih sama), panel baru langsung ditaruh di posisi shown
///    tanpa animasi ulang (persis kayak sebelumnya).
///
/// Yarn Spinner secara default nunggu komponen seperti "LineAdvancer"
/// buat kasih sinyal "lanjut baris berikutnya" lewat token.NextLineToken.
/// Karena project ini tidak punya LineAdvancer di scene, presenter ini
/// polling tombol "Submit" LANGSUNG, jadi advance baris tidak bergantung
/// komponen lain.
///
/// Tidak memakai choice, jadi RunOptionsAsync tidak perlu di-override
/// (default-nya sudah "tidak memilih apa-apa").
/// </summary>
public class CelesteDialoguePresenter : DialoguePresenterBase
{
    [Serializable]
    public class SpeakerPanel
    {
        [Tooltip("GameObject 'Player' atau 'NPC' itu sendiri, untuk di-aktif/nonaktifkan")]
        public GameObject root;
        public Image portrait;
        public TMP_Text speakerText;
        public TMP_Text dialogueText;
        public GameObject nextIndicator;
    }

    [Header("Referensi Panel")]
    [SerializeField] GameObject dialogueBoxRoot;   // object "DialogueBox" ATAU "DialoguePanel" (parent dari box), JANGAN DialogueCanvas!
    [SerializeField] RectTransform dialogueBoxRect; // RectTransform dari "DialogueBox" (posisi awal = posisi tampil)
    [Tooltip("Komponen Image DI GAMEOBJECT 'DialogueBox' itu sendiri (background box-nya). " +
             "Dipakai buat ganti sprite background sesuai karakter yang lagi ngomong " +
             "(field 'Dialogue Box Background' di asset DialogueCharacter).")]
    [SerializeField] Image dialogueBoxImage;
    [SerializeField] SpeakerPanel playerPanel;     // object "Player"
    [SerializeField] SpeakerPanel npcPanel;        // object "NPC"

    [Header("Data Karakter")]
    [SerializeField] List<DialogueCharacter> characters;

    [Header("Efek Ketik")]
    [SerializeField] float secondsPerCharacter = 0.02f;

    [Header("Input")]
    [Tooltip("Nama Input Axis/Button (Input Manager legacy) buat skip ketikan & lanjut baris. Harus sama dengan yang dipakai DwiNPC.")]
    [SerializeField] string submitButtonName = "Submit";

    [Header("Transisi Box & Panel (turun dari atas)")]
    [Tooltip("Seberapa jauh box DAN panel Player/NPC digeser ke atas layar saat disembunyikan. " +
             "Player/NPC dipakein jarak yang SAMA biar keliatan ikut nempel gerak bareng box, " +
             "walau secara hierarchy mereka sibling terpisah.")]
    [SerializeField] float dropDistance = 400f;
    [SerializeField] float boxTransitionDuration = 0.35f;

    [Header("Transisi Kamera (zoom saat dialogue)")]
    [SerializeField] Camera targetCamera;
    [SerializeField] float zoomedOrthographicSize = 3f;
    [SerializeField] float cameraTransitionDuration = 0.35f;

    Dictionary<string, DialogueCharacter> lookup;

    Vector2 shownBoxPosition;
    Vector2 hiddenBoxPosition;
    float defaultOrthographicSize;

    // Sprite background DialogueBox yang kepasang dari awal (sebelum Play),
    // dipakai sebagai fallback kalau karakter yang ngomong tidak punya
    // dialogueBoxBackground sendiri.
    Sprite defaultDialogueBoxSprite;

    // RectTransform + posisi shown/hidden buat Player & NPC, dipakai biar
    // mereka bisa digeser manual bareng box (mereka bukan child dari box).
    RectTransform playerPanelRect;
    RectTransform npcPanelRect;
    Vector2 playerShownPosition;
    Vector2 playerHiddenPosition;
    Vector2 npcShownPosition;
    Vector2 npcHiddenPosition;

    // True kalau box/kamera/panel belum dianimasikan masuk buat SESI dialog
    // saat ini. Dipakai biar animasi masuk cuma jalan sekali, di baris
    // PERTAMA, setelah panel Player/NPC yang benar udah ke-set kontennya.
    bool pendingEntrance = true;

    // Buat freeze player selama dialogue. Sengaja TIDAK edit PlayerController.cs
    // sama sekali — cukup disable komponennya + freeze Rigidbody2D dari luar,
    // lalu restore lagi pas dialogue selesai. Pakai PlayerController.Instance
    // (singleton yang sudah ada di script Tarodev kalian).
    Rigidbody2D playerRb;
    RigidbodyConstraints2D playerOriginalConstraints;

    void Awake()
    {
        lookup = new Dictionary<string, DialogueCharacter>();
        foreach (var c in characters)
        {
            if (c != null && !string.IsNullOrEmpty(c.characterName))
            {
                lookup[c.characterName] = c;
            }
        }

        if (dialogueBoxRect != null)
        {
            shownBoxPosition = dialogueBoxRect.anchoredPosition;
            hiddenBoxPosition = shownBoxPosition + new Vector2(0f, dropDistance);
            dialogueBoxRect.anchoredPosition = hiddenBoxPosition;
        }

        if (targetCamera != null)
        {
            defaultOrthographicSize = targetCamera.orthographicSize;
        }

        if (dialogueBoxImage != null)
        {
            defaultDialogueBoxSprite = dialogueBoxImage.sprite;
        }

        // Simpan posisi asli Player/NPC (posisi "shown"-nya), lalu hitung
        // posisi "hidden" pakai jarak yang SAMA kayak box (dropDistance),
        // biar pas dianimasikan bareng, gerakannya senada sama box.
        playerPanelRect = playerPanel.root != null ? playerPanel.root.GetComponent<RectTransform>() : null;
        npcPanelRect    = npcPanel.root != null ? npcPanel.root.GetComponent<RectTransform>() : null;

        if (playerPanelRect != null)
        {
            playerShownPosition = playerPanelRect.anchoredPosition;
            playerHiddenPosition = playerShownPosition + new Vector2(0f, dropDistance);
        }

        if (npcPanelRect != null)
        {
            npcShownPosition = npcPanelRect.anchoredPosition;
            npcHiddenPosition = npcShownPosition + new Vector2(0f, dropDistance);
        }

        if (dialogueBoxRoot != null) dialogueBoxRoot.SetActive(false);
        SetPanelActive(playerPanel, false);
        SetPanelActive(npcPanel, false);
    }

    public override async YarnTask OnDialogueStartedAsync()
    {
        FreezePlayer();

        // Animasi masuk (box+kamera+geser panel) SENGAJA ditunda sampai baris
        // pertama masuk di RunLineAsync, biar panel Player/NPC yang benar
        // udah ke-load (portrait/nama) SEBELUM animasinya jalan.
        pendingEntrance = true;

        await YarnTask.CompletedTask;
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        bool playerWasActive = playerPanel.root != null && playerPanel.root.activeSelf;

        RectTransform activeRect   = playerWasActive ? playerPanelRect : npcPanelRect;
        Vector2       activeHidden = playerWasActive ? playerHiddenPosition : npcHiddenPosition;

        YarnTask cameraTask = AnimateCameraSize(defaultOrthographicSize, cameraTransitionDuration);
        YarnTask boxTask    = AnimateRectPosition(dialogueBoxRect, hiddenBoxPosition, boxTransitionDuration);
        YarnTask panelTask  = AnimateRectPosition(activeRect, activeHidden, boxTransitionDuration);
        await YarnTask.WhenAll(cameraTask, boxTask, panelTask);

        if (dialogueBoxRoot != null) dialogueBoxRoot.SetActive(false);
        SetPanelActive(playerPanel, false);
        SetPanelActive(npcPanel, false);

        UnfreezePlayer();
    }

    /// <summary>
    /// Matikan input & gerak player selama dialogue, TANPA edit PlayerController.cs.
    /// Cukup disable komponennya (biar Update/FixedUpdate-nya berhenti baca input
    /// & gerak) dan freeze Rigidbody2D-nya (biar gravitasi/momentum juga berhenti).
    /// </summary>
    void FreezePlayer()
    {
        var controller = PlayerController.Instance;
        if (controller == null) return;

        playerRb = controller.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerOriginalConstraints = playerRb.constraints;
            playerRb.linearVelocity = Vector2.zero;
            playerRb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        controller.enabled = false;
    }

    void UnfreezePlayer()
    {
        var controller = PlayerController.Instance;
        if (controller != null)
        {
            controller.enabled = true;
        }

        if (playerRb != null)
        {
            playerRb.constraints = playerOriginalConstraints;
            playerRb = null;
        }
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speakerName = line.CharacterName;
        DialogueCharacter character = null;
        if (!string.IsNullOrEmpty(speakerName))
        {
            lookup.TryGetValue(speakerName, out character);
        }

        // Player kalau data karakternya dicentang isPlayer, selain itu masuk panel NPC.
        bool isPlayer = character != null && character.isPlayer;
        SpeakerPanel activePanel = isPlayer ? playerPanel : npcPanel;

        RectTransform activeRect   = isPlayer ? playerPanelRect : npcPanelRect;
        Vector2       activeShown  = isPlayer ? playerShownPosition : npcShownPosition;
        Vector2       activeHidden = isPlayer ? playerHiddenPosition : npcHiddenPosition;

        // ── Siapkan konten panel DULUAN (portrait, nama, ekspresi) ────
        // sebelum box/kamera/panel dianimasikan, biar pas animasi jalan,
        // Player/NPC yang ngomong udah "ke-load" bukan nyusul belakangan.
        SetPanelActive(playerPanel, isPlayer);
        SetPanelActive(npcPanel, !isPlayer);

        // Ambil tag ekspresi dari hashtag baris, contoh: "Dwi: Halo! #happy" -> "happy"
        string expressionKey = null;
        if (line.Metadata != null)
        {
            foreach (var tag in line.Metadata)
            {
                if (tag == "lastline") continue; // tag bawaan Yarn Spinner, bukan ekspresi
                expressionKey = tag;
                break;
            }
        }

        ApplyCharacterStyle(activePanel, character, speakerName, expressionKey);
        ApplyDialogueBoxBackground(character);

        if (activePanel.nextIndicator != null) activePanel.nextIndicator.SetActive(false);

        string fullText = line.TextWithoutCharacterName.Text;
        activePanel.dialogueText.text = "";

        if (pendingEntrance)
        {
            pendingEntrance = false;

            // Taruh panel di posisi "hidden" dulu (kontennya udah ke-set di
            // atas), lalu geser BARENGAN box slide + kamera zoom, jadi
            // Player/NPC keliatan ikut "turun" bareng box.
            if (activeRect != null) activeRect.anchoredPosition = activeHidden;

            if (dialogueBoxRoot != null) dialogueBoxRoot.SetActive(true);

            YarnTask cameraTask = AnimateCameraSize(zoomedOrthographicSize, cameraTransitionDuration);
            YarnTask boxTask    = AnimateRectPosition(dialogueBoxRect, shownBoxPosition, boxTransitionDuration);
            YarnTask panelTask  = AnimateRectPosition(activeRect, activeShown, boxTransitionDuration);
            await YarnTask.WhenAll(cameraTask, boxTask, panelTask);
        }
        else
        {
            // Box sudah kebuka, ini cuma ganti siapa yang lagi ngomong —
            // ga perlu re-animasi masuk, langsung taruh di posisi shown.
            if (activeRect != null) activeRect.anchoredPosition = activeShown;
        }

        // ── Efek ketik, cek Submit tiap frame biar bisa di-skip ────
        bool skipTyping = false;
        for (int i = 0; i < fullText.Length; i++)
        {
            activePanel.dialogueText.text += fullText[i];

            float elapsed = 0f;
            while (elapsed < secondsPerCharacter)
            {
                if (WasSubmitPressed(token))
                {
                    skipTyping = true;
                    break;
                }
                elapsed += Time.deltaTime;
                await YarnTask.Yield();
            }

            if (skipTyping) break;
        }

        activePanel.dialogueText.text = fullText;

        // PENTING: pencet Submit yang barusan dipakai buat skip ketikan
        // "aktif" sepanjang frame itu (Input.GetButtonDown tetap true untuk
        // SELURUH frame, bukan cuma sekali dibaca). Kalau kita langsung cek
        // Submit lagi di frame yang sama buat nunggu advance, pencet yang
        // sama bakal ke-anggap dobel: skip ketikan SEKALIGUS langsung minta
        // baris berikutnya -> makanya dialog kerasa "loncat kelewat cepat".
        // Fix-nya: paksa nunggu 1 frame kosong dulu sebelum mulai nunggu
        // Submit yang baru.
        if (skipTyping)
        {
            await YarnTask.Yield();
        }

        // Baris selesai ditampilkan penuh -> tampilkan indikator segitiga panel ini.
        if (activePanel.nextIndicator != null) activePanel.nextIndicator.SetActive(true);

        // ── Tunggu player pencet Submit lagi buat lanjut baris berikutnya ──
        // Polling langsung, TIDAK bergantung ke LineAdvancer / token.NextLineToken,
        // jadi tetap jalan walau tidak ada komponen advancer lain di scene.
        while (!WasSubmitPressed(token))
        {
            await YarnTask.Yield();
        }

        if (activePanel.nextIndicator != null) activePanel.nextIndicator.SetActive(false);

        // Debounce lagi: pencet Submit yang barusan dipakai buat advance ini
        // JANGAN sampai ke-baca ulang oleh baris berikutnya (RunLineAsync bakal
        // langsung dipanggil lagi oleh Yarn abis method ini return). Tanpa ini,
        // baris berikutnya bisa langsung ke-skip+ke-advance juga di frame yang
        // sama -> dialog kerasa "muncul sebentar lalu langsung ilang".
        await YarnTask.Yield();
    }

    /// <summary>
    /// True kalau player pencet tombol Submit FRAME INI, atau kalau ada request
    /// eksternal (mis. dari LineAdvancer, kalau suatu saat ditambah) lewat token.
    /// </summary>
    bool WasSubmitPressed(LineCancellationToken token)
    {
        if (Input.GetButtonDown(submitButtonName)) return true;
        if (token.IsNextLineRequested) return true;
        return false;
    }

    async YarnTask AnimateCameraSize(float targetSize, float duration)
    {
        if (targetCamera == null || duration <= 0f)
        {
            if (targetCamera != null) targetCamera.orthographicSize = targetSize;
            return;
        }

        float startSize = targetCamera.orthographicSize;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            targetCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t / duration);
            await YarnTask.Yield();
        }
        targetCamera.orthographicSize = targetSize;
    }

    /// <summary>
    /// Generic: geser anchoredPosition RectTransform manapun ke targetPosition.
    /// Dipakai buat DialogueBox MAUPUN panel Player/NPC, biar semuanya bisa
    /// dianimasikan barengan lewat YarnTask.WhenAll dan geraknya senada.
    /// </summary>
    async YarnTask AnimateRectPosition(RectTransform rect, Vector2 targetPosition, float duration)
    {
        if (rect == null) return;

        if (duration <= 0f)
        {
            rect.anchoredPosition = targetPosition;
            return;
        }

        Vector2 startPosition = rect.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t / duration);
            await YarnTask.Yield();
        }
        rect.anchoredPosition = targetPosition;
    }

    /// <summary>
    /// Ganti sprite background DialogueBox sesuai karakter yang lagi ngomong.
    /// Kalau karakter tidak punya dialogueBoxBackground sendiri (kosong di
    /// asset DialogueCharacter), balik ke sprite default (sprite awal yang
    /// kepasang di Image DialogueBox sebelum Play).
    /// </summary>
    void ApplyDialogueBoxBackground(DialogueCharacter character)
    {
        if (dialogueBoxImage == null) return;

        Sprite sprite = (character != null && character.dialogueBoxBackground != null)
            ? character.dialogueBoxBackground
            : defaultDialogueBoxSprite;

        dialogueBoxImage.sprite = sprite;
    }

    void ApplyCharacterStyle(SpeakerPanel panel, DialogueCharacter character, string fallbackName, string expressionKey)
    {
        panel.speakerText.text = character != null ? character.characterName : fallbackName;

        if (character == null) return;

        panel.speakerText.color = character.nameColor;

        if (panel.portrait != null)
        {
            Sprite sprite = character.GetSprite(expressionKey);
            panel.portrait.sprite = sprite;
            panel.portrait.gameObject.SetActive(sprite != null);
        }
    }

    void SetPanelActive(SpeakerPanel panel, bool active)
    {
        if (panel != null && panel.root != null)
        {
            panel.root.SetActive(active);
        }
    }
}