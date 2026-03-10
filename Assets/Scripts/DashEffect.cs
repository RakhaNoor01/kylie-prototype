using UnityEngine;

public class DashEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particles;

    public void OnDash(Vector2 direction)
    {
        // Rotate vfxContainer to face dash direction
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Play each particle system
        foreach (ParticleSystem particle in _particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play();
        }
    }
}