using System.Collections;
using UnityEngine;

/// <summary>
/// Taruh di GameObject KOSONG di scene gameplay (Forest/Mountain ATAU di scene
/// Persistent, terserah mana yang lebih nyaman) — TIDAK menyentuh RoomManager.cs
/// atau SoloLeveling.cs sama sekali.
///
/// Tugasnya cuma satu: begitu scene ini aktif, tunggu 1 frame supaya
/// RoomManager.InitializeStartRoom() (dipanggil dari SoloLeveling) sempat
/// selesai duluan, lalu sembunyikan LoadingScreenController.
///
/// Kalau mau lebih presisi (nunggu room benar2 siap, bukan cuma 1 frame),
/// bisa expose flag/event dari RoomManager nanti — tapi untuk sekarang ini
/// sudah cukup karena InitializeStartRoom() dipanggil di .completed callback
/// yang artinya scene sudah fully loaded saat itu.
/// </summary>
public class GameplayLoadingHook : MonoBehaviour
{
    [Tooltip("Delay tambahan sebelum loading screen disembunyikan (detik). " +
             "Beri sedikit waktu kalau ada inisialisasi lain yang perlu selesai.")]
    public float extraDelay = 0.15f;

    private void Start()
    {
        StartCoroutine(Co_HideLoadingScreen());
    }

    private IEnumerator Co_HideLoadingScreen()
    {
        // Tunggu beberapa frame supaya semua Awake/Start/InitializeStartRoom
        // di scene ini selesai duluan
        yield return null;
        yield return null;

        if (extraDelay > 0f)
            yield return new WaitForSeconds(extraDelay);

        if (LoadingScreenController.Instance != null)
            yield return LoadingScreenController.Instance.Hide();
    }
}