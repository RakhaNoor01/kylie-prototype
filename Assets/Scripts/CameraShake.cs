using UnityEngine;
using System.Collections;
using Unity.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Settings")]
    public float defaultDuration = 0.2f;
    public float defaultMagnitude = 0.1f;
    public float defaultMagFalloff = 0f;

    private Transform cameraTransform;
    private Vector3 originalPosition;
    private CameraController controller;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            cameraTransform = Camera.main.transform;
            controller = Camera.main.GetComponent<CameraController>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// what
    /// </summary>
    /// <param name="magFalloff">
    /// how much the magnitude decays per second
    /// if value is set as -1, magnitude decays linearly over duration
    /// </param>
    public void Shake(float duration, float magnitude, float? magFalloff)
    {
        StopAllCoroutines();
        var mfo = magFalloff ?? defaultMagFalloff;
        StartCoroutine(ShakeCoroutine(duration, magnitude, mfo));
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultMagnitude, defaultMagFalloff);
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude, float magFalloff)
    {
        float elapsed = 0f;
        originalPosition = cameraTransform.localPosition;

        var imGonnaMag = magnitude;
        var magDecay = magFalloff == -1 ? magnitude / duration : magFalloff;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * imGonnaMag;
            float y = Random.Range(-1f, 1f) * imGonnaMag;

            controller.ShakeOffset = new Vector3(x, y, 0);

            imGonnaMag -= magDecay * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        controller.ShakeOffset = Vector3.zero;
    }
}