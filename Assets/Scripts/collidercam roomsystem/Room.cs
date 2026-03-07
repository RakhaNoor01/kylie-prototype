using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    // objects that are actually in the room
    [Header("Room Content")]
    public Tilemap content;

    [Header("Adjacency")]
    public List<Room> adjacentRooms = new List<Room>();

    // Tracks all rooms the player is currently overlapping, in entry order
    private static readonly List<Room> overlappingRooms = new List<Room>();

    // called by RoomManager
    public void Load() => content.gameObject.SetActive(true);
    public void Unload() => content.gameObject.SetActive(false);

    // trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!overlappingRooms.Contains(this))
            overlappingRooms.Add(this);

        RoomManager.Instance.EnterRoom(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        overlappingRooms.Remove(this);

        // Only act if we were the active room
        if (RoomManager.Instance.CurrentRoom != this) return;

        if (overlappingRooms.Count > 0)
        {
            // Fall back to the most recently entered room still overlapping
            RoomManager.Instance.EnterRoom(overlappingRooms[overlappingRooms.Count - 1]);
        }
        // If fully clear, stay in the last room — don't unload, player is just between rooms
    }
}