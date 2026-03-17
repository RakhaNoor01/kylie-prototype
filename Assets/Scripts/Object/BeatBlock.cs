using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

[System.Serializable]
public class BeatBlockGroup
{
    public GameObject container;
}

public class BeatBlock : MonoBehaviour
{
    public float interval = 1f;
    public float offAlpha = 0.2f;
    public float indicator = 0.5f;
    public float indicatorAlpha = 0.75f;
    public List<BeatBlockGroup> beatBlocks;

    private int _currentIndex = 0;

    private void Start()
    {
        if (beatBlocks == null || beatBlocks.Count == 0) return;

        // Initialise: first group is active (alpha=1, colliders solid), rest are off
        for (int i = 0; i < beatBlocks.Count; i++)
            SetGroupState(beatBlocks[i], i == 0 ? 1f : offAlpha, isTrigger: i != 0);

        StartCoroutine(BeatRoutine());
    }

    private IEnumerator BeatRoutine()
    {
        while (true)
        {
            int nextIndex = (_currentIndex + 1) % beatBlocks.Count;

            // --- indicator flash: (interval - indicator) seconds into the beat ---
            yield return new WaitForSeconds(interval - indicator);
            SetGroupAlpha(beatBlocks[nextIndex], indicatorAlpha);

            // --- beat fires at full interval ---
            yield return new WaitForSeconds(indicator);

            // Turn the current (outgoing) group off
            SetGroupState(beatBlocks[_currentIndex], offAlpha, isTrigger: true);

            // Activate the next group
            SetGroupState(beatBlocks[nextIndex], 1f, isTrigger: false);

            _currentIndex = nextIndex;
        }
    }

    // helpers

    /// <summary>Sets alpha AND collider trigger state for every child renderer/collider.</summary>
    private void SetGroupState(BeatBlockGroup group, float alpha, bool isTrigger)
    {
        if (group?.container == null) return;
        SetGroupAlpha(group, alpha);
        SetGroupTrigger(group, isTrigger);
    }

    private void SetGroupAlpha(BeatBlockGroup group, float alpha)
    {
        if (group?.container == null) return;

        // SpriteRenderers
        foreach (var sr in group.container.GetComponentsInChildren<SpriteRenderer>())
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        // Tilemap

        var tile = group.container.GetComponent<Tilemap>();
        if (tile != null) {
            Color c = tile.color;
            c.a = alpha;
            tile.color = c;
        }
    }

    private void SetGroupTrigger(BeatBlockGroup group, bool isTrigger)
    {
        if (group?.container == null) return;

        foreach (var col in group.container.GetComponentsInChildren<Collider2D>())
            col.isTrigger = isTrigger;

        var tile = group.container.GetComponent<Collider>();
        if (tile != null) tile.isTrigger = isTrigger;
    }
}