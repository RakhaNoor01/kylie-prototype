using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Rooms")]
    public Room firstRoom;
    public List<Room> allRooms = new List<Room>();

    // The room the player is currently in
    public Room CurrentRoom { get; private set; }

    void Awake()
    {
        // slenderman: collect my rooms
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        allRooms.Clear();
        allRooms.AddRange(FindObjectsByType<Room>(FindObjectsSortMode.None));
    }

    void Start()
    {
        //firstRoom takes priority if assigned, otherwise falls back to allRooms[0] as before.
        Room startRoom = firstRoom ?? (allRooms.Count > 0 ? allRooms[0] : null);
        if (startRoom != null)
            LoadRoom(startRoom);
        else
            foreach (var room in allRooms)
                room.Unload();
    }

    /// <summary>Called by Room.OnTriggerEnter2D when the player steps into a room.</summary>
    public void EnterRoom(Room room)
    {
        if (room == CurrentRoom) return;

        CurrentRoom = room;
        LoadRoom(room);
    }

    /// <summary>Enable a room and its adjacent rooms; unload everything else.</summary>
    public void LoadRoom(Room room)
    {
        var shouldBeActive = new HashSet<Room> { room };
        foreach (var adjacent in room.adjacentRooms)
            if (adjacent != null)
                shouldBeActive.Add(adjacent);

        foreach (var r in allRooms)
        {
            if (shouldBeActive.Contains(r)) r.Load();
            else r.Unload();
        }
    }

    /// <summary>Force-unload every room.</summary>
    public void UnloadAll()
    {
        CurrentRoom = null;
        foreach (var room in allRooms)
            room.Unload();
    }
}