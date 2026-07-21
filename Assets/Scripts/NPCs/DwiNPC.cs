using UnityEngine;
using Yarn.Unity;

public class DwiNPC : MonoBehaviour
{
    public Animator      animator;
    public SpriteRenderer spriteRenderer;

    [Header("DIALOGUE (Yarn Spinner)")]
    [Tooltip("Kalau dikosongin, otomatis dicari di scene (di-retry tiap frame sampai ketemu, " +
             "penting kalau scene Persistent yang isinya DialogueRunner baru selesai load belakangan)")]
    public DialogueRunner dialogueRunner;

    [Tooltip("Nama node (title:) di file .yarn yang dijalankan saat player interaksi, contoh: 'Dwi_Start'")]
    public string startNode = "Dwi_Start";

    public SpriteRenderer interactionIndicator;

    public float bobHeight = 0.1f;
    public float bobSpeed  = 3f;

    // Dwi default sprite menghadap KANAN.
    // Jika sprite aslinya menghadap KIRI, ubah defaultFacingRight = false.
    [Header("FACING")]
    public bool defaultFacingRight = true;

    private Transform player;
    private bool      playerInRange;
    private Vector3   indicatorOrigin;

    // Dulu pencarian DialogueRunner cuma dilakukan sekali di Start().
    // Kalau scene "Persistent" (tempat DialogueRunner hidup) belum selesai
    // di-load pas Start() NPC ini jalan, dialogueRunner selamanya null dan
    // interaksi diam-diam gagal tanpa error apapun. Sekarang di-retry tiap
    // Update() sampai benar-benar ketemu, sama kayak pencarian "player".
    private bool loggedRunnerFound;

    private void Start()
    {
        if (interactionIndicator != null)
            indicatorOrigin = interactionIndicator.transform.localPosition;

        TryFindDialogueRunner();
    }

    private void Update()
    {
        if (dialogueRunner == null)
        {
            TryFindDialogueRunner();
        }

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.transform;
            }
            return;
        }

        // ── Flip menghadap player ─────────────────────────────────
        if (spriteRenderer != null)
        {
            bool playerIsRight = player.position.x > transform.position.x;
            // flipX true = balik dari arah default
            spriteRenderer.flipX = defaultFacingRight ? !playerIsRight : playerIsRight;
        }

        bool dialogueActive = dialogueRunner != null && dialogueRunner.IsDialogueRunning;

        // ── Sembunyikan indikator saat dialogue aktif ─────────────
        if (interactionIndicator != null)
        {
            bool shouldShow = playerInRange && !dialogueActive;
            interactionIndicator.enabled = shouldShow;

            if (shouldShow)
            {
                float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                interactionIndicator.transform.localPosition =
                    indicatorOrigin + new Vector3(0, y, 0);
            }
        }

        // ── Trigger dialogue ──────────────────────────────────────
        if (playerInRange
            && Input.GetButtonDown("Submit")
            && !dialogueActive
            && dialogueRunner != null)
        {
            dialogueRunner.StartDialogue(startNode);
        }
    }

    void TryFindDialogueRunner()
    {
        if (dialogueRunner != null) return;

        dialogueRunner = FindAnyObjectByType<DialogueRunner>();

        if (dialogueRunner != null && !loggedRunnerFound)
        {
            loggedRunnerFound = true;
            // Boleh dihapus setelah yakin semuanya jalan normal.
            Debug.Log("[DwiNPC] DialogueRunner ketemu: " + dialogueRunner.name, this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (interactionIndicator != null)
            interactionIndicator.enabled = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactionIndicator != null)
            interactionIndicator.enabled = false;
    }
}