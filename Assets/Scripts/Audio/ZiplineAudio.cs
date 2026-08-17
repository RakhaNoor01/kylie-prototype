using UnityEngine;
using TarodevController;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PlayerZiplineAudio : MonoBehaviour
{
    [Header("=== CLIPS ===")]
    [SerializeField] private AudioClip mountClip;
    [SerializeField] private AudioClip loopClip;
    [SerializeField] private AudioClip dismountClip;

    [Header("=== PITCH VARIATION ===")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform _followTarget;
    [SerializeField] private PlayerController _player;

    private AudioSource _sfx;
    private AudioSource _loop;
    private bool _isMounted;

    // ========================
    // SYSTEM GLOBAL: Semua Zipline di scene
    // ========================
    private List<Zipline> _allZiplines = new List<Zipline>();
    private Dictionary<Zipline, bool> _ziplineStates = new Dictionary<Zipline, bool>();
    private bool _isScanning;

    // ========================
    // LIFECYCLE
    // ========================

    private void Awake()
    {
        _sfx = GetComponent<AudioSource>();
        _loop = gameObject.AddComponent<AudioSource>();

        SetupAudioSource(_sfx, false, false);
        SetupAudioSource(_loop, false, true);

        // Auto-find PlayerController
        if (_player == null)
        {
            _player = GetComponentInParent<PlayerController>();
            if (_player == null) _player = GetComponentInChildren<PlayerController>();
        }

        if (_player == null)
        {
            Debug.LogError("[PlayerZiplineAudio] PlayerController tidak ditemukan!", this);
            enabled = false;
            return;
        }

        if (_followTarget == null)
        {
            _followTarget = _player.transform;
        }
    }

    private void Start()
    {
        ScanAllZiplines();
    }

    private void Update()
    {
        // Follow player position
        if (_followTarget != null)
        {
            transform.position = _followTarget.position;
        }

        // Polling semua zipline
        CheckAllZiplines();
    }

    private void OnEnable()
    {
        // Subscribe ke scene loaded
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        // Scan ulang setiap kali object di-enable (misal respawn)
        if (gameObject.activeInHierarchy)
        {
            Invoke(nameof(ScanAllZiplines), 0.1f);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe dari scene loaded
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        // Bersihin semua zipline references
        _allZiplines.Clear();
        _ziplineStates.Clear();
    }

    private void OnDestroy()
    {
        if (_isMounted) DismountZipline();
    }

    private void SetupAudioSource(AudioSource source, bool playOnAwake, bool loop)
    {
        source.playOnAwake = playOnAwake;
        source.loop = loop;
        source.spatialBlend = 0f;
    }

    // ========================
    // SCAN ZIPLINE
    // ========================

    [ContextMenu("Scan All Ziplines")]
    private void ScanAllZiplines()
    {
        if (_isScanning) return;
        _isScanning = true;

        // Reset dictionary
        _ziplineStates.Clear();

        // Cari SEMUA Zipline di scene (termasuk yang non-active/non-prefab)
        Zipline[] foundZiplines = FindObjectsByType<Zipline>(FindObjectsSortMode.None);
        _allZiplines.Clear();
        _allZiplines.AddRange(foundZiplines);

        // Inisialisasi state semua zipline = false
        foreach (var zipline in _allZiplines)
        {
            _ziplineStates[zipline] = false;
        }

        Debug.Log($"[PlayerZiplineAudio] Menemukan {_allZiplines.Count} Zipline di scene.");

        // Kalo lagi mounted dan tiba-tiba gak ada zipline aktif, dismount
        if (_isMounted && !IsAnyZiplineActive())
        {
            DismountZipline();
        }

        _isScanning = false;
    }

    // ========================
    // POLLING ZIPLINE
    // ========================

    private void CheckAllZiplines()
    {
        bool anyActive = false;

        foreach (var zipline in _allZiplines)
        {
            if (zipline == null) continue;

            if (zipline.isActive)
            {
                anyActive = true;
                break;
            }
        }

        // Kalo ada zipline aktif → mount
        if (anyActive && !_isMounted)
        {
            MountZipline();
        }
        // Kalo gak ada zipline aktif tapi masih mounted → dismount
        else if (!anyActive && _isMounted)
        {
            DismountZipline();
        }
    }

    private bool IsAnyZiplineActive()
    {
        foreach (var zipline in _allZiplines)
        {
            if (zipline != null && zipline.isActive)
                return true;
        }
        return false;
    }

    // ========================
    // PUBLIC METHODS
    // ========================

    public void MountZipline()
    {
        if (_isMounted) return;
        _isMounted = true;

        if (mountClip)
        {
            _sfx.pitch = Random.Range(minPitch, maxPitch);
            _sfx.PlayOneShot(mountClip);
        }

        if (loopClip)
        {
            _loop.clip = loopClip;
            _loop.pitch = 1f;
            _loop.Play();
        }

        Debug.Log("[PlayerZiplineAudio] Mount Zipline");
    }

    public void DismountZipline()
    {
        if (!_isMounted) return;
        _isMounted = false;

        _loop.Stop();

        if (dismountClip)
        {
            _sfx.pitch = Random.Range(minPitch, maxPitch);
            _sfx.PlayOneShot(dismountClip);
        }

        Debug.Log("[PlayerZiplineAudio] Dismount Zipline");
    }

    public bool IsMounted => _isMounted;

    // ========================
    // SCENE LOADED EVENT
    // ========================

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Scan ulang setiap kali scene berubah (ada zipline baru di-load)
        ScanAllZiplines();
    }
}