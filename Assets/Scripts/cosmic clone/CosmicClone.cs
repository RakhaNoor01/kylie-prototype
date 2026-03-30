using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float aftImgInterval = 0.05f;

    [Header("Particles")]
    public ParticleSystem psSpawn;
    public ParticleSystem psStart;
    public ParticleSystem psStart2;
    public ParticleSystem psDespawn;

    // internal state 
    private float _delay;
    private bool _active;
    private bool _waiting;

    public bool Active => _active;

    // position recording
    private readonly List<(float time, Vector3 pos)> _posRecord = new();

    // sprite recording
    private readonly List<(float time, Sprite sprite, float scale)> _sprRecord = new();
    private SpriteRenderer _playerSR;   // player's SpriteRenderer (source of truth)
    private Sprite _lastSprite;
    private float _lastScale;

    private Coroutine _recordCoroutine;
    private Coroutine _aftImgCoroutine;

    private Collider2D _col;

    // public API 

    private void Start()
    {
        _col = GetComponent<Collider2D>();
        Deactivate();
    }

    // waiting for delay
    public void Activate(float delay, Vector2 startpos)
    {
        transform.position = startpos;

        _delay = delay;
        _active = true;
        _waiting = true;

        _posRecord.Clear();
        _sprRecord.Clear();

        _playerSR = playerVisual.GetComponent<SpriteRenderer>();
        _lastSprite = null;

        _recordCoroutine ??= StartCoroutine(RecordLoop());

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

        _col.enabled = true;

        _waiting = false;
        spriteContainer.SetActive(true);
        _aftImgCoroutine ??= StartCoroutine(AfterimageLoop());
    }

    // stopping
    public void Deactivate()
    {
        _active = false;

        if (_recordCoroutine != null) { StopCoroutine(_recordCoroutine); _recordCoroutine = null; }
        if (_aftImgCoroutine != null) { StopCoroutine(_aftImgCoroutine); _aftImgCoroutine = null; }

        DOTween.Kill(transform);

        psDespawn.Play();
        spriteContainer.SetActive(false);

        _col.enabled = false;
    }

    // recording loop 
    private IEnumerator RecordLoop()
    {
        while (_active || _waiting)
        {
            float now = Time.time;

            // position 
            _posRecord.Add((now, playerVisual.transform.position));

            // dispatch playback for the position recorded <delay> seconds ago
            DispatchPositionPlayback(now);

            //  sprite & scale change detection 
            if (_playerSR != null && _playerSR.sprite != _lastSprite)
            {
                _lastSprite = _playerSR.sprite;
                _lastScale = Mathf.Sign(playerVisual.transform.localScale.x);
                _sprRecord.Add((now, _lastSprite, _lastScale));
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
        float targetTime = now - _delay;
        if (_posRecord.Count == 0) return;

        // find the two bracketing samples
        int idx = FindLastIndexBefore(_posRecord, targetTime, r => r.time);
        if (idx < 0) return;

        Vector3 targetPos;
        if (idx + 1 < _posRecord.Count)
        {
            // lerp between the two surrounding samples for smooth motion
            var a = _posRecord[idx];
            var b = _posRecord[idx + 1];
            float t = Mathf.InverseLerp(a.time, b.time, targetTime);
            targetPos = Vector3.Lerp(a.pos, b.pos, t);
        }
        else
        {
            targetPos = _posRecord[idx].pos;
        }

        // tween to that position over one recordingRate tick
        transform.DOMove(targetPos, recordingRate).SetEase(Ease.Linear);
    }

    private void DispatchSpritePlayback(float now)
    {
        float targetTime = now - _delay;
        if (_sprRecord.Count == 0) return;

        int idx = FindLastIndexBefore(_sprRecord, targetTime, r => r.time);
        if (idx < 0) return;

        Sprite s = _sprRecord[idx].sprite;
        var scale = _sprRecord[idx].scale;

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
        while (_active && !_waiting)
        {
            SpawnAfterimage();
            yield return new WaitForSeconds(aftImgInterval);
        }
    }

    private void SpawnAfterimage()
    {
        foreach (var sr in aftImgSprites)
        {
            if (sr == null || sr.sprite == null) continue;

            // create afterimage
            var aftimg = Instantiate(sr.gameObject, sr.transform.position, sr.transform.rotation);
            var af_sr = aftimg.GetComponent<SpriteRenderer>();
            af_sr.sortingLayerName = aftImgLayer;

            // fade out then destroy
            af_sr.DOFade(0f, aftImgDuration)
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
        float cutoff = now - _delay - recordingRate * 2f;  // small safety margin

        while (_posRecord.Count > 1 && _posRecord[1].time < cutoff)
            _posRecord.RemoveAt(0);

        while (_sprRecord.Count > 1 && _sprRecord[1].time < cutoff)
            _sprRecord.RemoveAt(0);
    }
}