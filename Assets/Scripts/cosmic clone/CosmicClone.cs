using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;   // ← dibungkus agar tidak error saat build
#endif

public class CosmicClone : MonoBehaviour
{
    [Header("References")]
    public GameObject playerVisual;

    [Header("Movement")]
    public float recordingRate = 0.05f;

    [Header("Sprites")]
    public GameObject spriteContainer;
    public List<SpriteRenderer> sprites;
    public List<SpriteMask> spritemasks;

    [Header("Afterimage")]
    public List<SpriteRenderer> aftImgSprites;
    public string aftImgLayer = "Default";
    public float aftImgDuration = 0.3f;
    public float aftImgSpawnDistance = 0.1f;

    [Header("Particles")]
    public ParticleSystem psSpawn;
    public ParticleSystem psStart;
    public ParticleSystem psStart2;
    public ParticleSystem psDespawn;

    // internal state 
    private float delay;
    private bool active;
    private bool waiting;

    public bool Active => active;

    // position recording
    private readonly List<(float time, Vector3 pos)> posRecord = new();

    // sprite recording
    private readonly List<(float time, Sprite sprite, float scale)> sprRecord = new();
    private SpriteRenderer playerSR;   // player's SpriteRenderer (source of truth)
    private Sprite lastSprite;
    private float lastScale;

    private Coroutine recordCoroutine;
    private Coroutine aftImgCoroutine;

    private Collider2D col;
    private PlayerHealth plrHp;

    private Vector3 lastAftImgPos;

    private void Start()
    {
        col = GetComponent<Collider2D>();
        plrHp = FindFirstObjectByType<PlayerHealth>();
        Deactivate();

        plrHp.death += Deactivate;
    }

    // waiting for delay
    public void Activate(float startDelay, Vector2 startpos, Collider2D thing)
    {
        if (playerVisual == null)
        {
            playerVisual = thing.GetComponentInChildren<PlayerAnimator>().gameObject;
        }

        transform.position = startpos;

        delay = startDelay;
        active = true;
        waiting = true;

        posRecord.Clear();
        sprRecord.Clear();

        playerSR = playerVisual.GetComponent<SpriteRenderer>();
        lastSprite = null;

        recordCoroutine ??= StartCoroutine(RecordLoop());

        StartCoroutine(ChaseStart(delay));

        psSpawn.Play();
    }

    
    // start moving
    private IEnumerator ChaseStart(float delay)
    {
        yield return new WaitForSeconds(delay);
        psSpawn.Stop();
        psStart.Play();
        psStart2.Play();

        col.enabled = true;

        waiting = false;
        spriteContainer.SetActive(true);
        aftImgCoroutine ??= StartCoroutine(AfterimageLoop());
    }

    // stopping
    public void Deactivate()
    {
        if (waiting) return;
        active = false;

        if (recordCoroutine != null) { StopCoroutine(recordCoroutine); recordCoroutine = null; }
        if (aftImgCoroutine != null) { StopCoroutine(aftImgCoroutine); aftImgCoroutine = null; }

        DOTween.Kill(transform);

        psDespawn.Play();
        spriteContainer.SetActive(false);

        col.enabled = false;
    }

    // recording loop 
    private IEnumerator RecordLoop()
    {
        while (active || waiting)
        {
            float now = Time.time;

            // position 
            posRecord.Add((now, playerVisual.transform.position));

            // dispatch playback for the position recorded <delay> seconds ago
            DispatchPositionPlayback(now);

            //  sprite & scale change detection 
            if (playerSR != null && playerSR.sprite != lastSprite)
            {
                lastSprite = playerSR.sprite;
                lastScale = Mathf.Sign(playerVisual.transform.localScale.x);
                sprRecord.Add((now, lastSprite, lastScale));
            }

            // dispatch sprite playback
            DispatchSpritePlayback(now);

            // prune old records
            PruneRecords(now);

            yield return new WaitForSeconds(recordingRate);
        }
    }

    // playback helpers 

    private void DispatchPositionPlayback(float now)
    {
        float targetTime = now - delay;
        if (posRecord.Count == 0) return;

        // find the two bracketing samples
        int idx = FindLastIndexBefore(posRecord, targetTime, r => r.time);
        if (idx < 0) return;

        Vector3 targetPos;
        if (idx + 1 < posRecord.Count)
        {
            // lerp between the two surrounding samples for smooth motion
            var a = posRecord[idx];
            var b = posRecord[idx + 1];
            float t = Mathf.InverseLerp(a.time, b.time, targetTime);
            targetPos = Vector3.Lerp(a.pos, b.pos, t);
        }
        else
        {
            targetPos = posRecord[idx].pos;
        }

        // tween to that position over one recordingRate tick
        transform.DOMove(targetPos, recordingRate).SetEase(Ease.Linear);
    }

    private void DispatchSpritePlayback(float now)
    {
        float targetTime = now - delay;
        if (sprRecord.Count == 0) return;

        int idx = FindLastIndexBefore(sprRecord, targetTime, r => r.time);
        if (idx < 0) return;

        Sprite s = sprRecord[idx].sprite;
        var scale = sprRecord[idx].scale;

        foreach (var sr in sprites)
        {
            if (sr == null) continue;
            sr.sprite = s;
        }

        foreach (var smask in spritemasks)
        {
            if (smask == null) continue;
            smask.sprite = s;
        }

        var spriteCon = spriteContainer.transform.localScale;
        spriteContainer.transform.localScale = new Vector2(Mathf.Abs(spriteCon.x) * scale, spriteCon.y);
    }

    // afterimage loop 

    private IEnumerator AfterimageLoop()
    {
        lastAftImgPos = transform.position;

        while (active && !waiting)
        {
            float moved = Vector3.Distance(transform.position, lastAftImgPos);
            if (moved >= aftImgSpawnDistance)
            {
                // catch up: spawn one afterimage per full interval travelled
                int count = Mathf.FloorToInt(moved / aftImgSpawnDistance);
                for (int i = 0; i < count; i++)
                    SpawnAfterimage();

                lastAftImgPos = transform.position;
            }

            yield return null;
        }
    }

    private void SpawnAfterimage()
    {
        float containerScaleX = spriteContainer.transform.localScale.x;

        foreach (var sr in aftImgSprites)
        {
            if (sr == null || sr.sprite == null) continue;

            var aftimg = Instantiate(sr.gameObject, sr.transform.position, sr.transform.rotation);

            var aftScale = aftimg.transform.localScale;
            aftimg.transform.localScale = new Vector3(
                Mathf.Abs(aftScale.x) * Mathf.Sign(containerScaleX),
                aftScale.y,
                aftScale.z
            );

            var afsr = aftimg.GetComponent<SpriteRenderer>();
            afsr.sortingLayerName = aftImgLayer;

            afsr.DOFade(0f, aftImgDuration)
                 .OnComplete(() => Destroy(aftimg));
        }
    }

    // generic helpers 

    /// Returns the index of the last record whose timestamp <= targetTime.
    private static int FindLastIndexBefore<T>(List<T> list, float targetTime, Func<T, float> getTime)
    {
        int lo = 0, hi = list.Count - 1, result = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (getTime(list[mid]) <= targetTime) { result = mid; lo = mid + 1; }
            else { hi = mid - 1; }
        }
        return result;
    }

    /// Removes records that are old enough that they will never be needed.
    private void PruneRecords(float now)
    {
        float cutoff = now - delay - recordingRate * 2f;  // small safety margin

        while (posRecord.Count > 1 && posRecord[1].time < cutoff)
            posRecord.RemoveAt(0);

        while (sprRecord.Count > 1 && sprRecord[1].time < cutoff)
            sprRecord.RemoveAt(0);
    }
}