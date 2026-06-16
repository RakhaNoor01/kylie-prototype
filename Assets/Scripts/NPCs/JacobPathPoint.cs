using UnityEngine;

public enum JacobAction { Run, Jump, Wait, IdleEnd }

/// <summary>
/// Satu titik path Jacob. Attach ke GameObject kosong, drag ke pathPoints di JacobNPC.
///
/// Jump setup:
///   • Waypoint ini = titik TOLAK
///   • landingPoint  = Transform titik PENDARATAN (drag GO lain)
///   • arcHeight     = tinggi puncak lompatan
/// </summary>
public class JacobPathPoint : MonoBehaviour
{
    [Tooltip("Aksi saat Jacob TIBA di waypoint ini.")]
    public JacobAction action = JacobAction.Run;

    [Header("Jump")]
    [Tooltip("Titik pendaratan. Wajib diisi bila action = Jump.")]
    public Transform landingPoint;
    [Tooltip("Tinggi puncak arc lompatan (meter).")]
    public float arcHeight = 3f;

    [Header("Wait")]
    [Tooltip("Durasi diam (detik). Dipakai bila action = Wait.")]
    public float waitTime = 1f;

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private const float R = 0.22f;

    private void OnDrawGizmos()
    {
        Gizmos.color = GetColor();
        Gizmos.DrawSphere(transform.position, R);

        if (action == JacobAction.Jump && landingPoint != null)
        {
            // Titik landing
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(landingPoint.position, R * 0.75f);

            // Preview arc parabola (sama dengan DogController/JumpWaypoint)
            Gizmos.color = Color.green;
            Vector3 s = transform.position, e = landingPoint.position;
            Vector3 prev = s;
            for (int i = 1; i <= 24; i++)
            {
                float t = i / 24f;
                float x = Mathf.Lerp(s.x, e.x, t);
                float y = Mathf.Lerp(s.y, e.y, t) + arcHeight * 4f * t * (1f - t);
                Vector3 curr = new Vector3(x, y, 0f);
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }
        }

#if UNITY_EDITOR
        var style = new GUIStyle { fontStyle = FontStyle.Bold, fontSize = 10 };
        style.normal.textColor = GetColor();
        string lbl = action switch
        {
            JacobAction.Run     => "RUN",
            JacobAction.Jump    => $"JUMP arc={arcHeight}",
            JacobAction.Wait    => $"WAIT {waitTime}s",
            JacobAction.IdleEnd => "END",
            _                   => action.ToString()
        };
        UnityEditor.Handles.Label(transform.position + Vector3.up * (R + 0.1f),
            $"[{gameObject.name}] {lbl}", style);
#endif
    }

    private Color GetColor() => action switch
    {
        JacobAction.Run     => new Color(0.2f, 0.8f, 0.2f),
        JacobAction.Jump    => new Color(0.2f, 0.6f, 1.0f),
        JacobAction.Wait    => new Color(1.0f, 0.85f, 0.1f),
        JacobAction.IdleEnd => new Color(1.0f, 0.25f, 0.25f),
        _                   => Color.white
    };
}