using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kontrol animasi iris wipe lingkaran.
///
/// SETUP:
/// 1. Buat Material baru, assign shader "UI/CircleWipe"
/// 2. Di Canvas, buat Image baru:
///      Name:         CircleWipeImage
///      Anchor:       stretch-stretch (full screen)
///      Left/Right/Top/Bottom: 0
///      Material:     material yang dibuat di step 1
///      Color:        bebas (tidak dipakai, shader pakai _Color)
///      RaycastTarget: OFF
/// 3. Pasang script CircleWipeController.cs di Image tersebut
/// 4. Di MainMenuManager, hapus slot circleWipe lama, drag CircleWipeImage ke slot baru
///
/// Hierarchy akhir (pastikan CircleWipeImage PALING ATAS di Canvas, last child):
/// [Canvas]
///   ├── BG
///   ├── Group_MainMenu
///   ├── Group_LevelSelect
///   ├── Group_ControlsGuide
///   ├── Panel_Fade
///   └── CircleWipeImage   ← paling atas / last child
/// </summary>
[RequireComponent(typeof(Image))]
public class CircleWipeController : MonoBehaviour
{
    [Header("Wipe Settings")]
    [Tooltip("Durasi wipe masuk (mengecil, menu menghilang)")]
    public float wipeInDuration  = 0.65f;

    [Tooltip("Durasi wipe keluar (membesar, setelah scene baru load)")]
    public float wipeOutDuration = 0.55f;

    [Tooltip("Warna layar di luar lingkaran")]
    public Color wipeColor = Color.black;

    [Tooltip("Softness tepi lingkaran (0 = hard, 0.05 = soft)")]
    [Range(0f, 0.1f)]
    public float softness = 0.02f;

    // ── Private ───────────────────────────────────────────────────────────────

    private Material _mat;
    private static readonly int PropRadius      = Shader.PropertyToID("_Radius");
    private static readonly int PropColor       = Shader.PropertyToID("_Color");
    private static readonly int PropSoftness    = Shader.PropertyToID("_Softness");
    private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");

    private void Awake()
    {
        // Instance material supaya tidak shared
        _mat = Instantiate(GetComponent<Image>().material);
        GetComponent<Image>().material = _mat;

        _mat.SetColor(PropColor, wipeColor);
        _mat.SetFloat(PropSoftness, softness);
        UpdateAspectRatio();

        // Mulai fully open (lingkaran besar = semua keliatan... eh wait:
        // Radius besar = semua transparan (lingkaran besar nutupin area hitam lebih kecil)
        // Radius = 0 = full hitam
        // Radius = 1+ = full transparan
        // Start: tidak aktif, radius besar (tidak keliatan efeknya)
        _mat.SetFloat(PropRadius, 1.5f);
        gameObject.SetActive(false);
    }

    private void UpdateAspectRatio()
    {
        float aspect = (float)Screen.width / Screen.height;
        _mat.SetFloat(PropAspectRatio, aspect);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Wipe IN: lingkaran mengecil → layar jadi hitam.
    /// Panggil sebelum load scene.
    /// </summary>
    public IEnumerator WipeIn()
    {
        gameObject.SetActive(true);
        UpdateAspectRatio();
        yield return StartCoroutine(AnimateRadius(1.5f, 0f, wipeInDuration));
    }

    /// <summary>
    /// Wipe OUT: lingkaran membesar → scene baru terungkap.
    /// Panggil di awal scene baru (dari script lain yang ada di scene itu).
    /// </summary>
    public IEnumerator WipeOut()
    {
        gameObject.SetActive(true);
        UpdateAspectRatio();
        yield return StartCoroutine(AnimateRadius(0f, 1.5f, wipeOutDuration));
        gameObject.SetActive(false);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private IEnumerator AnimateRadius(float from, float to, float duration)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = EaseInOut(Mathf.Clamp01(e / duration));
            _mat.SetFloat(PropRadius, Mathf.Lerp(from, to, t));
            yield return null;
        }
        _mat.SetFloat(PropRadius, to);
    }

    private float EaseInOut(float t) => t < 0.5f ? 2*t*t : 1f - Mathf.Pow(-2*t+2, 2f)/2f;
}
