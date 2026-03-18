using UnityEngine;

public class CosmicCloneTrigger : MonoBehaviour
{
    [Header("Settings")]
    public bool ccState;
    [Tooltip("Clone Delay")]
    public float delay = 2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CosmicClone clone = FindFirstObjectByType<CosmicClone>();
        if (clone == null) return;

        if (ccState && !clone.Active)
            clone.Activate(delay, transform.position);
        else if (!ccState && clone.Active)
            clone.Deactivate();
    }
}