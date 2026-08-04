// CircleWipeController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Kontrol animasi iris wipe lingkaran.
/// </summary>
[RequireComponent(typeof(Image))]
public class CircleWipeController : MonoBehaviour
{
    [Header("Wipe Settings")]
    public float wipeInDuration = 0.65f;
    public float wipeOutDuration = 0.55f;
    public Color wipeColor = Color.black;
    [Range(0f, 0.1f)]
    public float softness = 0.02f;

    private Material _mat;
    private Tween _tween;
    private static readonly int PropRadius = Shader.PropertyToID("_Radius");
    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropSoftness = Shader.PropertyToID("_Softness");
    private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");

    private void Awake()
    {
        _mat = Instantiate(GetComponent<Image>().material);
        GetComponent<Image>().material = _mat;

        _mat.SetColor(PropColor, wipeColor);
        _mat.SetFloat(PropSoftness, softness);
        UpdateAspectRatio();

        _mat.SetFloat(PropRadius, 1.5f);
        gameObject.SetActive(false);
    }

    private void UpdateAspectRatio()
    {
        float aspect = (float)Screen.width / Screen.height;
        _mat.SetFloat(PropAspectRatio, aspect);
    }

    public IEnumerator WipeIn()
    {
        gameObject.SetActive(true);
        UpdateAspectRatio();
        _tween?.Kill();
        _tween = DOVirtual.Float(1.5f, 0f, wipeInDuration, v => _mat.SetFloat(PropRadius, v))
            .SetEase(Ease.InOutQuad);
        yield return _tween.WaitForCompletion();
    }

    public IEnumerator WipeOut()
    {
        gameObject.SetActive(true);
        UpdateAspectRatio();
        _tween?.Kill();
        _tween = DOVirtual.Float(0f, 1.5f, wipeOutDuration, v => _mat.SetFloat(PropRadius, v))
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => gameObject.SetActive(false));
        yield return _tween.WaitForCompletion();
    }
}