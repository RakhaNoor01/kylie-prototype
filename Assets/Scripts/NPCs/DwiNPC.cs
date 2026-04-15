using UnityEngine;

public class DwiNPC : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("=== PORTRAIT CUSTOM ===")]
    public Sprite dialoguePortrait;

    [Header("=== TARGET & DIALOGUE ===")]
    public Transform walkTarget;
    public DialogueData firstDialogue;
    public DialogueData secondDialogue;
    public string npcName = "Dwi";

    [Header("=== SETTINGS ===")]
    public float moveSpeed = 3f;

    [Header("=== ANIMATION NAMES ===")]
    public string idleAnimName = "dwi-idle";
    public string talkAnimName = "dwi-talk";
    public string walkAnimName = "dwi-walk";

    private enum State { Idle, Talking, Walking, IdleAtDestination, Done }
    private State currentState = State.Idle;

    private int dialogueStage = 0;

    private Transform player;
    private bool playerInRange = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        ChangeState(State.Idle);
    }

    private void Update()
    {
        if (currentState == State.Idle || currentState == State.IdleAtDestination)
        {
            spriteRenderer.flipX = player.position.x < transform.position.x;
        }

        if (playerInRange
            && Input.GetButtonDown("Submit")
            && !DialogueManager.Instance.IsDialogueActive
            && currentState != State.Walking
            && currentState != State.Done)
        {
            DoInteract();
        }

        if (currentState == State.Walking)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, walkTarget.position, step);
            spriteRenderer.flipX = walkTarget.position.x < transform.position.x;

            if (Vector2.Distance(transform.position, walkTarget.position) < 0.05f)
            {
                transform.position = walkTarget.position;
                dialogueStage = 1;
                ChangeState(State.IdleAtDestination);
            }
        }
    }

    private void DoInteract()
    {
        spriteRenderer.flipX = player.position.x < transform.position.x;

        if (dialogueStage == 0 && currentState == State.Idle)
        {
            ChangeState(State.Talking);
            DialogueManager.Instance.StartDialogue(firstDialogue, npcName, dialoguePortrait, OnFirstDialogueComplete);
        }
        else if (dialogueStage == 1 && currentState == State.IdleAtDestination)
        {
            ChangeState(State.Talking);
            DialogueManager.Instance.StartDialogue(secondDialogue, npcName, dialoguePortrait, OnSecondDialogueComplete);
        }
    }

    private void OnFirstDialogueComplete()
    {
        ChangeState(State.Walking);
    }

    private void OnSecondDialogueComplete()
    {
        dialogueStage = 2;
        ChangeState(State.Done);
    }

    private void ChangeState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Idle:
            case State.IdleAtDestination:
            case State.Done:
                ForcePlay(idleAnimName);
                break;
            case State.Talking:
                ForcePlay(talkAnimName);
                break;
            case State.Walking:
                ForcePlay(walkAnimName);
                break;
        }
    }

    // Rebind() reset SELURUH state machine animator dari awal,
    // jadi parameter / transition apapun yang masih "nyangkut" ikut direset.
    private void ForcePlay(string stateName)
    {
        animator.Rebind();
        animator.Update(0f);
        animator.Play(stateName, -1, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}