using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Settings")]
    public bool isStartingPoint = false;

    [Tooltip("if this field is not null, the checkpoint will respawn at the setLocation instead of this gameObject's transform")]
    public Transform setLocation;

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

        var cpPos = setLocation == null ? transform.position : setLocation.position;

        if (isStartingPoint && CheckpointManager.Instance != null && !CheckpointManager.Instance.HasCheckpoint)
        {
            ActivateCheckpoint(); // very first load, no checkpoint saved yet
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            if (currentlyActiveCheckpoint != null && currentlyActiveCheckpoint != this)
                currentlyActiveCheckpoint.DeactivateCheckpoint();

            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        isActivated = true;
        currentlyActiveCheckpoint = this;

        var cpPos = setLocation == null ? transform.position : setLocation.position;

        if (CheckpointManager.Instance != null)
        {
            // Pass the scene this checkpoint lives in
            string sceneName = gameObject.scene.name;
            CheckpointManager.Instance.SetCheckpoint(cpPos, sceneName, isStartingPoint);
        }

        if (checkpointCollider != null)
            checkpointCollider.enabled = false;

        Debug.Log($"Checkpoint activated at: {cpPos}"); // FIXED: log cpPos not transform.position
    }

    private void DeactivateCheckpoint()
    {
        isActivated = false;

        if (checkpointCollider != null)
            checkpointCollider.enabled = true;
    }

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