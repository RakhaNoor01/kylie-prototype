using UnityEngine;
public class FallingRock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDetection playerDetection;
    [SerializeField] private DeathZone deathZone;

    [Header("Gravity Settings")]
    [SerializeField] private float gravityScale = 1f;

    private Rigidbody2D _rb;
    private bool _isFalling = false;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        if (playerDetection == null)
        {
            playerDetection = GetComponent<PlayerDetection>();
        }

        if (deathZone == null)
        {
            deathZone = GetComponent<DeathZone>();
        }

        if (_rb != null)
        {
            _rb.isKinematic = true;
        }

        if (deathZone != null)
        {
            deathZone.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Subscribe to player detection event
        if (playerDetection != null)
        {
            playerDetection.OnPlayerDetected += OnPlayerDetected;
        }
        else
        {
            Debug.LogWarning($"FallingRock on {gameObject.name} has no PlayerDetection reference!", gameObject);
        }
    }

    private void OnDestroy()
    {
        if (playerDetection != null)
        {
            playerDetection.OnPlayerDetected -= OnPlayerDetected;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Rock collided with: {collision.gameObject.name} on layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        
            Debug.Log($"Hit.");
            if (deathZone != null)
            {
                deathZone.gameObject.SetActive(false);
            }
            
            int rockLayer = gameObject.layer;
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(rockLayer, playerLayer);
            }
            
            _isFalling = false;
        
    }

    private void OnPlayerDetected()
    {
        if (!_isFalling)
        {
            StartFalling();
        }
    }

    private void StartFalling()
    {
        _isFalling = true;

 
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.gravityScale = gravityScale;
        }

 
        if (deathZone != null)
        {
            deathZone.gameObject.SetActive(true);
        }
    }
    public void ResetRock()
    {
        _isFalling = false;

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector2.zero;
        }

        if (deathZone != null)
        {
            deathZone.gameObject.SetActive(false);
        }

        if (playerDetection != null)
        {
            playerDetection.ResetDetection();
        }
    }
}
