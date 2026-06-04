using UnityEngine;

public class DashEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particles;
    private TrailRenderer trail;
    public float trailDurr = 0.2f;

    private void Start()
    {
        trail = GetComponent<TrailRenderer>();
        trail.emitting = false;
    }

    public void OnDash(Vector2 direction)
    {
        float angle = 0f;
        // Rotate vfxContainer to face dash direction
        if (direction != Vector2.zero)
        {
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Play each particle system
        foreach (ParticleSystem particle in _particles)
        {
            var main = particle.main;
            main.startRotation = -angle * Mathf.Deg2Rad; // MainModule uses radians

            particle.Play();
        }

        trail.emitting = true;
        Invoke(nameof(TheKiller), trailDurr);
    }

    private void TheKiller()
    {
        trail.emitting = false;
    }
}