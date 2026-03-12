using UnityEngine;

/// <summary>
/// Reusable script that detects the player from above using a raycast.
/// Can be used for any obstacle/mechanism that needs to detect the player below it.
/// </summary>
public class PlayerDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float raycastDistance = 50f;
    [SerializeField] private LayerMask detectionLayer = -1;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;
    
    private bool _playerDetected = false;
    public bool PlayerDetected => _playerDetected;

    public delegate void PlayerDetectionCallback();
    public event PlayerDetectionCallback OnPlayerDetected;

    private void Update()
    {
        CheckForPlayer();
    }

    private void CheckForPlayer()
    {
        
        bool cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;

        // Raycast downwards from this object
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            raycastDistance,
            detectionLayer
        );

        Physics2D.queriesStartInColliders = cachedQueryStartInColliders;

        bool wasDetected = _playerDetected;

        _playerDetected = hit.collider != null && hit.collider.CompareTag("Player");

        if (_playerDetected && !wasDetected)
        {
            OnPlayerDetected?.Invoke();
        }

        // Debug visualization
        if (showDebugRay)
        {
            Color rayColor = _playerDetected ? Color.red : Color.green;
            Debug.DrawRay(transform.position, Vector2.down * raycastDistance, rayColor);
        }
    }

  
    public void ResetDetection()
    {
        _playerDetected = false;
    }
}
