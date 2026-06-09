using UnityEngine;

public class CollapsePillar : MonoBehaviour
{
    public string floorTag;
    public string lavaTag;
    public GameObject deathZone;
    public ParticleSystem landing;
    public ParticleSystem landingLava;

    [Header("Shake")]
    public float duration = 0.5f;
    public float mag = 0.75f;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        deathZone.SetActive(false);
    }

    private void Land()
    {
        CameraShake.Instance.Shake(duration, mag, -1);
        deathZone.SetActive(true);
        Destroy(rb);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(floorTag))
        {
            landing.Play();
            Land();
        }

        if (collision.gameObject.CompareTag(lavaTag))
        {
            landingLava.Play();
            Land();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var xtra = collision.gameObject.GetComponent<ExtraTags>();
        if (xtra == null) return;

        if (xtra.extraTag == ExtraTags.ExtraTag.stopCollapse)
        {
            CollapseHandler.instance.isCollapse = false;
        }
    }
}
