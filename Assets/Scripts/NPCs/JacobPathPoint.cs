using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════
//  ENUM  — dibagi di sini agar dipakai bersama JacobNPC
// ═════════════════════════════════════════════════════════════════════════════

public enum MovementAction
{
    /// <summary>Jacob berlari menuju posisi waypoint ini.</summary>
    Run,

    /// <summary>Jacob melompat saat tiba di waypoint ini, lalu lanjut ke berikutnya.</summary>
    Jump,

    /// <summary>Jacob berhenti sejenak di waypoint ini selama <see cref="JacobPathPoint.waitTime"/> detik.</summary>
    Wait,

    /// <summary>Jacob berhenti total. HARUS menjadi waypoint terakhir.</summary>
    IdleEnd
}

// ═════════════════════════════════════════════════════════════════════════════
//  JACOB PATH POINT
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Satu titik dalam path Jacob. Attach ke GameObject kosong, lalu drag ke
/// array <c>pathPoints</c> di JacobNPC sesuai urutan.
///
/// Gizmo warna:
///   🟢 Hijau  = Run   🔵 Biru   = Jump
///   🟡 Kuning = Wait  🔴 Merah  = IdleEnd
/// </summary>
public class JacobPathPoint : MonoBehaviour
{
    [Tooltip("Aksi yang dilakukan Jacob saat TIBA di waypoint ini.")]
    public MovementAction action = MovementAction.Run;

    [Tooltip("Gaya lompat (ForceMode2D.Impulse). Aktif bila action = Jump.")]
    public float arcHeight = 3f;

    [Tooltip("Durasi diam (detik). Aktif bila action = Wait.")]
    public float waitTime = 1f;

    [Tooltip("Arah hadap sprite saat bergerak MENUJU waypoint ini.")]
    public bool faceRight = true;

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private const float GizmoRadius = 0.22f;

    private void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor();
        Gizmos.DrawSphere(transform.position, GizmoRadius);

#if UNITY_EDITOR
        // Label teks di atas titik
        var style = new GUIStyle { fontStyle = FontStyle.Bold, fontSize = 11 };
        style.normal.textColor = GizmoColor();

        string lbl = action switch
        {
            MovementAction.Run     => "RUN →",
            MovementAction.Jump    => $"JUMP arc={arcHeight}m",
            MovementAction.Wait    => $"WAIT {waitTime}s",
            MovementAction.IdleEnd => "■ END",
            _                      => action.ToString()
        };

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (GizmoRadius + 0.12f),
            $"[{gameObject.name}]\n{lbl}", style);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        // Garis pendek menunjukkan arah hadap
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, (faceRight ? Vector3.right : Vector3.left) * 0.5f);
    }

    private Color GizmoColor() => action switch
    {
        MovementAction.Run     => new Color(0.20f, 0.80f, 0.20f, 0.85f),
        MovementAction.Jump    => new Color(0.20f, 0.60f, 1.00f, 0.85f),
        MovementAction.Wait    => new Color(1.00f, 0.85f, 0.10f, 0.85f),
        MovementAction.IdleEnd => new Color(1.00f, 0.25f, 0.25f, 0.85f),
        _                      => Color.white
    };
}