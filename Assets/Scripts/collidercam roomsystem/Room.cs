// Room.cs
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Room Content")]
    public string sceneName;

    [Header("Adjacency")]
    public List<Room> adjacentRooms = new List<Room>();

    [Header("Ligthing")]
    [Range(0f, 1f)]
    public float globalLightIntensity = 1f;
    public Color globalLightColor = Color.white;

    private static readonly List<Room> overlappingRooms = new List<Room>();

    private GameObject player;

    private void Awake()
    {
        player = Slopburger.instance.gameObject;

        HashSet<Room> unique = new HashSet<Room>();

        for (int i = adjacentRooms.Count - 1; i >= 0; i--)
        {
            var room = adjacentRooms[i];

            if (room != null && !unique.Add(room))
            {
                Debug.LogWarning($"Duplicate adjacent room '{room.name}' removed from '{name}'.", this);
                adjacentRooms.RemoveAt(i);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject != player) return;
        if (!overlappingRooms.Contains(this))
            overlappingRooms.Add(this);
        if (CheckpointManager.Instance.IsRespawning) return;
        RoomManager.Instance.EnterRoom(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject != player) return;
        overlappingRooms.Remove(this);

        if (RoomManager.Instance.CurrentRoom != this) return;
        if (overlappingRooms.Count > 0)
            RoomManager.Instance.EnterRoom(overlappingRooms[overlappingRooms.Count - 1]);
    }

    void OnValidate()
    {
        // always guarantees bidirectional relations between adjacent rooms
        foreach (var room in adjacentRooms)
        {
            if (room != null && !room.adjacentRooms.Contains(this))
            {
                room.adjacentRooms.Add(this);
            }
        }

        for (int i = adjacentRooms.Count - 1; i >= 0; i--)
        {
            if (adjacentRooms[i] == this)
            {
                Debug.LogWarning($"Room '{name}' cannot be adjacent to itself. Removing entry.", this);
                adjacentRooms.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (adjacentRooms == null)
            return;

        Gizmos.color = Color.cyan;

        Vector3 start = transform.position;

        foreach (var room in adjacentRooms)
        {
            if (room == null)
                continue;

            Vector3 end = room.transform.position;

            Gizmos.DrawLine(start, end);

            Gizmos.DrawWireSphere(end, 2f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(gameObject.transform.position, gameObject.transform.localScale);
    }
}