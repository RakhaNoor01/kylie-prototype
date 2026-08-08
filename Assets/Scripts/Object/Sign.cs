using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public class SignPopup : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private Transform visual;

    [Header("Filter Trigger")]
    [SerializeField] private string playerTag = "Player";

    [Header("Animasi Muncul / Hilang")]
    [SerializeField] private Vector3 targetScale = Vector3.one;
    [SerializeField] private float growDuration = 0.35f;
    [SerializeField] private float shrinkDuration = 0.25f;
    [SerializeField] private AnimationCurve growCurve =
        new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.7f, 1.08f), new Keyframe(1, 1));
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Efek Ngambang (saat sudah full muncul)")]
    [SerializeField] private bool enableFloat = true;
    [SerializeField] private float floatAmplitude = 0.08f;
    [SerializeField] private float floatSpeed = 2f;

    private Coroutine currentRoutine;
    private Vector3 basePosition;
    private bool isPlayerInside;

    private void Awake()
    {
        if (visual == null)
        {
            Debug.LogWarning($"[SignPopup] '{name}' belum punya referensi 'Visual'. Drag child sprite-nya ke field itu.", this);
            return;
        }

        basePosition = visual.localPosition;
        visual.localScale = Vector3.zero;
        visual.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        isPlayerInside = true;
        visual.gameObject.SetActive(true);

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimateScale(growCurve, growDuration, onComplete: () =>
        {
            if (enableFloat) currentRoutine = StartCoroutine(FloatLoop());
        }));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        isPlayerInside = false;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        visual.localPosition = basePosition;
        currentRoutine = StartCoroutine(AnimateScale(shrinkCurve, shrinkDuration, onComplete: () =>
        {
            visual.gameObject.SetActive(false);
        }));
    }

    private IEnumerator AnimateScale(AnimationCurve curve, float duration, System.Action onComplete = null)
    {
        float t = 0f;
        Vector3 startScale = visual.localScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = curve.Evaluate(Mathf.Clamp01(t / duration));
            visual.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, progress);
            yield return null;
        }

        visual.localScale = curve.keys[curve.length - 1].value >= 1f ? targetScale : Vector3.zero;
        onComplete?.Invoke();
    }

    private IEnumerator FloatLoop()
    {
        float t = 0f;
        while (isPlayerInside)
        {
            t += Time.deltaTime * floatSpeed;
            visual.localPosition = basePosition + new Vector3(0f, Mathf.Sin(t) * floatAmplitude, 0f);
            yield return null;
        }
    }
}