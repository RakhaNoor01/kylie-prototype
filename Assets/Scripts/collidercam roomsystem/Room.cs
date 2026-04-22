// Room.cs
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Room Content")]
    public string sceneName;

    [Header("Adjacency")]
    public List<Room> adjacentRooms = new List<Room>();

    private static readonly List<Room> overlappingRooms = new List<Room>();

    private void Awake()
    {
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
        if (!other.CompareTag("Player")) return;
        if (!overlappingRooms.Contains(this))
            overlappingRooms.Add(this);
        if (CheckpointManager.Instance.IsRespawning) return;
        RoomManager.Instance.EnterRoom(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
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
}