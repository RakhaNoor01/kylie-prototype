using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

// BGM Manager untuk Menu (fokus menu dulu)
// Taruh di GameObject "Managers" (persistent)
// TIDAK menyentuh MenuAudioManager sama sekali
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Menu BGM - Drag and Drop")]
    [SerializeField] private AudioClip menuBGM;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.7f;

    private AudioSource audioSource;
    private Tween fadeTween;
    private bool isMenuScene = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup AudioSource khusus BGM (terpisah dari SFX)
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
        audioSource.clip = menuBGM;
    }

    private void Start()
    {
        // Play menu music dengan fade in
        PlayMenuBGM();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cek apakah ini scene Menu
        isMenuScene = scene.name.Contains("Menu") || scene.name.Contains("MainMenu");
        
        if (isMenuScene)
        {
            PlayMenuBGM();
        }
        else
        {
            FadeOutAndStop();
        }
    }

    // ===== PUBLIC METHODS =====

    public void PlayMenuBGM()
    {
        if (menuBGM == null)
        {
            Debug.LogWarning("[BGMManager] Menu BGM clip is null!");
            return;
        }

        // Kalau lagu sama dan sedang playing, skip
        if (audioSource.clip == menuBGM && audioSource.isPlaying)
        {
            return;
        }

        // Fade out lagu lama kalau ada
        if (audioSource.isPlaying)
        {
            FadeOut(() =>
            {
                audioSource.clip = menuBGM;
                audioSource.Play();
                FadeIn();
            });
        }
        else
        {
            audioSource.clip = menuBGM;
            audioSource.Play();
            FadeIn();
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource.isPlaying)
        {
            audioSource.volume = volume;
        }
    }

    public void PauseBGM()
    {
        audioSource.Pause();
    }

    public void ResumeBGM()
    {
        audioSource.UnPause();
    }

    public void StopBGM()
    {
        if (fadeTween != null)
        {
            fadeTween.Kill();
            fadeTween = null;
        }
        audioSource.Stop();
        audioSource.volume = 0f;
    }

    // ===== PRIVATE METHODS =====

    private void FadeIn()
    {
        if (fadeTween != null)
        {
            fadeTween.Kill();
            fadeTween = null;
        }

        audioSource.volume = 0f;
        fadeTween = audioSource.DOFade(volume, fadeInDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                fadeTween = null;
                Debug.Log("[BGMManager] BGM fade in complete");
            });
    }

    private void FadeOut(System.Action onComplete = null)
    {
        if (fadeTween != null)
        {
            fadeTween.Kill();
            fadeTween = null;
        }

        fadeTween = audioSource.DOFade(0f, fadeOutDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                fadeTween = null;
                Debug.Log("[BGMManager] BGM fade out complete");
                onComplete?.Invoke();
            });
    }

    private void FadeOutAndStop()
    {
        if (audioSource.isPlaying)
        {
            FadeOut(() =>
            {
                audioSource.Stop();
                audioSource.volume = 0f;
                Debug.Log("[BGMManager] BGM stopped");
            });
        }
    }

    // ===== GETTER =====

    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }

    public bool IsInMenuScene()
    {
        return isMenuScene;
    }

    public float GetVolume()
    {
        return volume;
    }

    // ===== UNITY EDITOR =====

    #if UNITY_EDITOR
    [ContextMenu("Test Fade In")]
    private void TestFadeIn()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
            FadeIn();
        }
        else
        {
            Debug.LogWarning("[BGMManager] No clip assigned!");
        }
    }

    [ContextMenu("Test Fade Out")]
    private void TestFadeOut()
    {
        if (audioSource.isPlaying)
        {
            FadeOut();
        }
        else
        {
            Debug.LogWarning("[BGMManager] No music playing!");
        }
    }
    #endif
}