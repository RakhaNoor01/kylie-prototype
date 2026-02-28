using UnityEngine;

public class TeleportWaypoint : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Posisi tujuan teleport (belakang breakable wall)")]
    public Transform teleportDestination;

    [Tooltip("Index waypoint setelah teleport (opsional, -1 = otomatis +1)")]
    public int nextWaypointIndex = -1;

    [Header("Idle After Teleport (opsional)")]
    [Tooltip("Berapa detik idle setelah teleport (0 = pakai default dari DogController)")]
    public float idleDurationAfterTeleport = 0f;
}