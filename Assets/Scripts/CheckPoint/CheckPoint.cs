using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Settings")]
    public bool isStartingPoint = false;

    [Header("Visual Feedback")]
    public Sprite inactiveSprite; // Gray flag
    public Sprite activeSprite;   // Colored flag
    public AudioClip activateSound;

    private SpriteRenderer spriteRenderer;
    private Collider2D checkpointCollider;
    private bool isActivated = false;

    // Static variable to track the currently active checkpoint
    private static Checkpoint currentlyActiveCheckpoint = null;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        checkpointCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        // Set initial sprite to inactive
        if (spriteRenderer != null && inactiveSprite != null)
            spriteRenderer.sprite = inactiveSprite;

        // If this is the starting point, activate it
        if (isStartingPoint)
        {
            // Deactivate any previously active checkpoint
            if (currentlyActiveCheckpoint != null && currentlyActiveCheckpoint != this)
                currentlyActiveCheckpoint.DeactivateCheckpoint();

            ActivateCheckpoint();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            // Deactivate the previous checkpoint if it exists
            if (currentlyActiveCheckpoint != null && currentlyActiveCheckpoint != this)
            {
                currentlyActiveCheckpoint.DeactivateCheckpoint();
            }

            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        isActivated = true;
        currentlyActiveCheckpoint = this;

        // Tell the manager this is the new spawn point
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.SetCheckpoint(transform.position);

        // Update visual to active
        if (spriteRenderer != null && activeSprite != null)
            spriteRenderer.sprite = activeSprite;

        // Play sound
        if (activateSound != null)
            AudioSource.PlayClipAtPoint(activateSound, transform.position);

        // Disable trigger so it can't be activated again (until deactivated)
        if (checkpointCollider != null)
            checkpointCollider.enabled = false;

        Debug.Log($"Checkpoint activated at: {transform.position}");
    }

    // New method to deactivate this checkpoint
    private void DeactivateCheckpoint()
    {
        isActivated = false;

        // Change back to inactive sprite
        if (spriteRenderer != null && inactiveSprite != null)
            spriteRenderer.sprite = inactiveSprite;

        // Re-enable trigger so it can be activated again if player comes back
        if (checkpointCollider != null)
            checkpointCollider.enabled = true;

        Debug.Log($"Checkpoint deactivated at: {transform.position}");
    }

    // Optional: Visualize in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.gray;
        if (GetComponent<Collider2D>() != null)
        {
            Collider2D col = GetComponent<Collider2D>();
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.bounds.size);
        }
    }
}