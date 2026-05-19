#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RoomAdjacencyEditor
{
    [MenuItem("CONTEXT/Room/Assign Closest Adjacent Room")]
    private static void AssignClosestAdjacentRoom(MenuCommand command)
    {
        Room targetRoom = (Room)command.context;
        AssignClosest(targetRoom);
    }

    // Also available via the top menu for any selected Room GameObjects
    [MenuItem("Tools/Rooms/Assign Closest Adjacent Room")]
    private static void AssignClosestAdjacentRoomTopMenu()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Room room = go.GetComponent<Room>();
            if (room != null)
                AssignClosest(room);
        }
    }

    [MenuItem("Tools/Rooms/Assign Closest Adjacent Room", validate = true)]
    private static bool ValidateAssignClosest()
    {
        return Selection.gameObjects.Any(go => go.GetComponent<Room>() != null);
    }

    private static void AssignClosest(Room targetRoom)
    {
        // Gather all Room components in the scene, excluding the target itself
        Room[] allRooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None)
            .Where(r => r != targetRoom)
            .ToArray();

        if (allRooms.Length == 0)
        {
            Debug.LogWarning($"[RoomAdjacencyEditor] No other rooms found in the scene.", targetRoom);
            return;
        }

        // Sort by distance from targetRoom
        IEnumerable<Room> sorted = allRooms
            .OrderBy(r => Vector3.Distance(targetRoom.transform.position, r.transform.position));

        // Pick the closest room not already adjacent; fall back to 2nd closest, etc.
        Room chosen = null;
        foreach (Room candidate in sorted)
        {
            if (!targetRoom.adjacentRooms.Contains(candidate))
            {
                chosen = candidate;
                break;
            }
        }

        if (chosen == null)
        {
            Debug.LogWarning($"[RoomAdjacencyEditor] All nearby rooms are already adjacent to '{targetRoom.name}'.", targetRoom);
            return;
        }

        // Record undo for both objects so the change is undoable in the editor
        Undo.RecordObject(targetRoom, "Assign Closest Adjacent Room");
        Undo.RecordObject(chosen, "Assign Closest Adjacent Room (bidirectional)");

        targetRoom.adjacentRooms.Add(chosen);

        // Mirror the relationship (Room.OnValidate does this at validation time,
        // but we do it here immediately so the change is visible right away)
        if (!chosen.adjacentRooms.Contains(targetRoom))
            chosen.adjacentRooms.Add(targetRoom);

        EditorUtility.SetDirty(targetRoom);
        EditorUtility.SetDirty(chosen);

        Debug.Log($"[RoomAdjacencyEditor] '{chosen.name}' added as adjacent room to '{targetRoom.name}'.", targetRoom);
    }
}
#endif