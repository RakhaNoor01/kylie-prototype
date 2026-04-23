using UnityEngine;
using System.Collections;

public class JacobNPC : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("=== PORTRAIT CUSTOM ===")]
    public Sprite dialoguePortrait;

    [Header("=== DIALOGUE ===")]
    public DialogueData dialogue;

    [Tooltip("Fallback nama NPC jika DialogueData.npcDisplayName kosong.")]
    public string npcNameFallback = "Jacob";

    [Header("=== INTERACTION INDICATOR ===")]
    public SpriteRenderer interactionIndicator;

    [Header("=== INDICATOR BOB ===")]
    public float bobHeight = 0.1f;
    public float bobSpeed  = 3f;

    [Header("=== ANIMATION NAMES ===")]
    public string idleAnimName = "Cob-Idle";
    public string talkAnimName = "Cob-Talk";

    private enum State { Idle, Talking, Done }
    private State currentState = State.Idle;

    private Transform player;
    private Rigidbody2D playerRb;

    private bool playerInRange = false;
    private Vector3 indicatorOrigin;

    // ─────────────────────────────────────────────
    private void Start()
    {
        if (interactionIndicator != null)
            indicatorOrigin = interactionIndicator.transform.localPosition;

        ChangeState(State.Idle);
        RefreshIndicator();
    }

    private bool TryGetPlayer()
    {
        if (player != null) return true;

        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return false;

        player = go.transform;
        playerRb = go.GetComponent<Rigidbody2D>();

        return true;
    }

    private void Update()
    {
        if (!TryGetPlayer()) return;

        // 🔻 HADAP KE PLAYER
        if ((currentState == State.Idle || currentState == State.Done) && player != null)
        {
            spriteRenderer.flipX = player.position.x > transform.position.x;
        }

        // indicator bobbing
        if (interactionIndicator != null && interactionIndicator.enabled)
        {
            float offsetY = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            interactionIndicator.transform.localPosition =
                indicatorOrigin + new Vector3(0f, offsetY, 0f);
        }

        // interact
        if (playerInRange
            && Input.GetButtonDown("Submit")
            && !DialogueManager.Instance.IsDialogueActive
            && currentState == State.Idle)
        {
            DoInteract();
        }
    }

    // ─────────────────────────────────────────────
    private void DoInteract()
    {
        ChangeState(State.Talking);

        // 🔻 MATIIN RIGIDBODY PLAYER
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.simulated = false;
        }

        DialogueManager.Instance.StartDialogue(
            dialogue,
            dialoguePortrait,
            OnDialogueComplete,
            npcNameFallback);
    }

    private void OnDialogueComplete()
    {
        StartCoroutine(EnablePlayerAfterDelay());
        ChangeState(State.Done);
    }

    private IEnumerator EnablePlayerAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        if (playerRb != null)
            playerRb.simulated = true;
    }

    // ─────────────────────────────────────────────
    private void RefreshIndicator()
    {
        if (interactionIndicator == null) return;

        bool canInteract = currentState == State.Idle && playerInRange;
        interactionIndicator.enabled = canInteract;
    }

    // ─────────────────────────────────────────────
    private void ChangeState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Idle:
            case State.Done:
                ForcePlay(idleAnimName);
                break;
            case State.Talking:
                ForcePlay(talkAnimName);
                break;
        }

        RefreshIndicator();
    }

    private void ForcePlay(string stateName)
    {
        animator.Rebind();
        animator.Update(0f);
        animator.Play(stateName, -1, 0f);
    }

    // ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        RefreshIndicator();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        RefreshIndicator();
    }
}