// Room.cs
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Room Content")]
    public string roomName;

    [Header("Adjacency")]
    public List<Room> adjacentRooms = new List<Room>();

    private static readonly List<Room> overlappingRooms = new List<Room>();

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

        if (RoomManager.Instance.CurrentRoom != this) return;
        if (overlappingRooms.Count > 0)
            RoomManager.Instance.EnterRoom(overlappingRooms[overlappingRooms.Count - 1]);
    }
}