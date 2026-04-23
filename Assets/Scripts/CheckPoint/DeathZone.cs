using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            TryKillPlayer(other.gameObject);
    }

    private void TryKillPlayer(GameObject playerObject)
    {
        Vector2 dir = (playerObject.transform.position - gameObject.transform.position).normalized;

        PlayerHealth health = playerObject.GetComponent<PlayerHealth>();
        if (health != null)
            health.Die(-dir);
    }
}