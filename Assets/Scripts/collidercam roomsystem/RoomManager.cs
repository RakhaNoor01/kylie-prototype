// RoomManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Rooms")]
    public Room firstRoom;
    public List<Room> allRooms = new List<Room>();

    public Room CurrentRoom { get; private set; }

    // Tracks which room scenes are currently loaded
    private readonly HashSet<string> loadedScenes = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        allRooms.Clear();
        allRooms.AddRange(FindObjectsByType<Room>(FindObjectsSortMode.None));
    }

    // Call this instead of the old Start() — safe for level selection too
    public void InitializeStartRoom()
    {
        Room startRoom = firstRoom ?? (allRooms.Count > 0 ? allRooms[0] : null);
        if (startRoom == null) return;

        CurrentRoom = startRoom;
        StartCoroutine(InitCoroutine(startRoom));
    }

    private IEnumerator InitCoroutine(Room room)
    {
        var shouldBeLoaded = new HashSet<string>();
        if (!string.IsNullOrEmpty(room.sceneName))
            shouldBeLoaded.Add(room.sceneName);
        foreach (var adjacent in room.adjacentRooms)
            if (adjacent != null && !string.IsNullOrEmpty(adjacent.sceneName))
                shouldBeLoaded.Add(adjacent.sceneName);

        yield return StartCoroutine(SyncScenesCoroutine(shouldBeLoaded));

        // After scenes are loaded, tell CheckpointManager to place the player
        CheckpointManager.Instance?.SpawnAtRoom(room);
    }

    public void EnterRoom(Room room)
    {
        if (room == CurrentRoom) return;
        CurrentRoom = room;
        LoadRoom(room);
    }

    public void LoadRoom(Room room)
    {
        var shouldBeLoaded = new HashSet<string>();

        if (!string.IsNullOrEmpty(room.sceneName))
            shouldBeLoaded.Add(room.sceneName);

        foreach (var adjacent in room.adjacentRooms)
            if (adjacent != null && !string.IsNullOrEmpty(adjacent.sceneName))
                shouldBeLoaded.Add(adjacent.sceneName);

        StartCoroutine(SyncScenesCoroutine(shouldBeLoaded));
    }

    public void UnloadAll()
    {
        CurrentRoom = null;
        StartCoroutine(UnloadAllCoroutine());
    }

    private IEnumerator SyncScenesCoroutine(HashSet<string> shouldBeLoaded)
    {
        // Load scenes that should be active but aren't
        foreach (var sceneName in shouldBeLoaded)
        {
            // Scene may already be loaded before RoomManager runs
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                loadedScenes.Add(sceneName); // track the already loaded room
                continue;
            }

            if (!loadedScenes.Contains(sceneName))
            {
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                loadedScenes.Add(sceneName);
            }
        }

        // Unload scenes that are loaded but shouldn't be
        var toUnload = new List<string>();
        foreach (var sceneName in loadedScenes)
            if (!shouldBeLoaded.Contains(sceneName))
                toUnload.Add(sceneName);

        foreach (var sceneName in toUnload)
        {
            yield return SceneManager.UnloadSceneAsync(sceneName);
            loadedScenes.Remove(sceneName);
        }
    }

    private IEnumerator UnloadAllCoroutine()
    {
        foreach (var sceneName in new List<string>(loadedScenes))
        {
            yield return SceneManager.UnloadSceneAsync(sceneName);
        }
        loadedScenes.Clear();
    }

    public IEnumerator ReloadRoomCoroutine(Room room)
    {
        var shouldBeLoaded = new HashSet<string>();
        if (!string.IsNullOrEmpty(room.sceneName))
            shouldBeLoaded.Add(room.sceneName);
        foreach (var adjacent in room.adjacentRooms)
            if (adjacent != null && !string.IsNullOrEmpty(adjacent.sceneName))
                shouldBeLoaded.Add(adjacent.sceneName);

        // Unload all currently loaded room scenes first
        var toUnload = new List<string>(loadedScenes);
        foreach (var sceneName in toUnload)
        {
            yield return SceneManager.UnloadSceneAsync(sceneName);
            loadedScenes.Remove(sceneName);
        }

        // Fresh load
        foreach (var sceneName in shouldBeLoaded)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            loadedScenes.Add(sceneName);
        }

        CurrentRoom = room;
    }

    public Room GetRoomByName(string sceneName)
    {
        return allRooms.Find(r => r.sceneName == sceneName);
    }
}