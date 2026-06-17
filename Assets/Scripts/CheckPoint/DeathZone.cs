using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public bool detonationOnly = false;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Vector2 normal = GetContactNormal(other);

        if (Bomb.theBobm != null)
        {
            var deton = Bomb.theBobm.GetComponent<Bomb>().Detonate(detonationOnly);
            if (deton) return;
        }

        if (detonationOnly) return;

        TryKillPlayer(other.gameObject, normal);
    }

    private void TryKillPlayer(GameObject playerObject, Vector2 dir)
    {
        PlayerHealth health = playerObject.GetComponent<PlayerHealth>();
        if (health != null)
            health.Die(dir);
    }

    private Vector2 GetContactNormal(Collider2D other)
    {
        ContactPoint2D[] contacts = new ContactPoint2D[8];
        int count = other.GetContacts(contacts);

        Vector2 avgNormal = Vector2.zero;
        int validCount = 0;

        for (int i = 0; i < count; i++)
        {
            // Only use contacts that are against this deathzone collider
            if (contacts[i].otherCollider.transform.IsChildOf(transform) ||
                contacts[i].otherCollider == GetComponent<Collider2D>())
            {
                avgNormal += contacts[i].normal;
                validCount++;
            }
        }

        if (validCount > 0)
            return avgNormal.normalized;

        // Fallback: use position-based direction if no contacts found
        return (other.transform.position - transform.position).normalized;
    }
}