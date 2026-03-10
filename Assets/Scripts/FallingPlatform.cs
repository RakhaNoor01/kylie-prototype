using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [Header("Fall Settings")]
    public float fallDelay = 0f;
    
    private Rigidbody2D rb;
    private bool isFalling = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Start as kinematic (no gravity)
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 1f;
        }
    }

    public void StartFalling()
    {
        if (isFalling || rb == null) return;
        
        isFalling = true;
        
        if (fallDelay > 0f)
        {
            StartCoroutine(FallAfterDelay());
        }
        else
        {
            BeginFall();
        }
    }

    private System.Collections.IEnumerator FallAfterDelay()
    {
        yield return new WaitForSeconds(fallDelay);
        BeginFall();
    }

    private void BeginFall()
    {
        // Switch from kinematic to dynamic so gravity affects it
        rb.bodyType = RigidbodyType2D.Dynamic;
    }
}
