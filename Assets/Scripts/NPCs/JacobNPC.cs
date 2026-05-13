using UnityEngine;

public class JacobNPC : MonoBehaviour
{
    public Animator       animator;
    public SpriteRenderer spriteRenderer;

    [Header("DIALOGUE")]
    public DialogueData dialogue;
    public Sprite       npcPortrait;
    public Sprite       dialogueBoxSprite;

    public SpriteRenderer interactionIndicator;

    public float bobHeight = 0.1f;
    public float bobSpeed  = 3f;

    // Jacob sprite aslinya menghadap KIRI → defaultFacingRight = false.
    [Header("FACING")]
    public bool defaultFacingRight = false;

    private Transform   player;
    private Rigidbody2D playerRb;
    private bool        playerInRange;
    private Vector3     indicatorOrigin;

    private void Start()
    {
        if (interactionIndicator != null)
            indicatorOrigin = interactionIndicator.transform.localPosition;
    }

    private void Update()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player   = go.transform;
                playerRb = go.GetComponent<Rigidbody2D>();
            }
            return;
        }

        // ── Flip menghadap player ─────────────────────────────────
        if (spriteRenderer != null)
        {
            bool playerIsRight = player.position.x > transform.position.x;
            // Jacob default kiri: flipX true = hadap kanan
            spriteRenderer.flipX = defaultFacingRight ? !playerIsRight : playerIsRight;
        }

        // ── Sembunyikan indikator saat dialogue aktif ─────────────
        bool dialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
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
            && !dialogueActive)
        {
            DialogueManager.Instance.StartDialogue(dialogue, npcPortrait, dialogueBoxSprite);
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