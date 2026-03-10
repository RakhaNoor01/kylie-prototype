using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private void Start()
    {
        // Find the player by tag and move them to this spawn point at start
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = transform.position;
        }
    }
}