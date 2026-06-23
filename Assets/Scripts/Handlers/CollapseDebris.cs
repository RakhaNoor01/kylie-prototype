using System.Collections;
using TarodevController;
using UnityEngine;

public class CollapseDebris : MonoBehaviour
{
    public float minSpeed = 10;
    public float maxSpeed = 15;
    public float minSize = 0.7f;
    public float maxSize = 1.3f;
    public float fallDelay = 0.5f;
    public float lifetime = 5;

    public string indicatorAnimName;
    public ParticleSystem imBrok;
    public ParticleSystem lavar;

    private bool instantiated = false;
    private float yOffset;
    private Transform camr;
    private Transform playe;
    private Rigidbody2D rb;
    private bool falling = false;
    private Animator indicator;
    private Collider2D ough;
    private bool h = false;
    private float speed;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ough = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Static;
        ough.enabled = false;
        playe = Slopburger.instance.gameObject.transform;
    }

    public void Yummy(Transform camera, float yOffset, Animator indicator)
    {
        camr = camera;
        this.yOffset = yOffset;
        this.indicator = indicator;

        var hi = Random.value;
        speed = Mathf.Lerp(minSpeed, maxSpeed, hi);
        var size = Mathf.Lerp(maxSize, minSize, hi);
        
        gameObject.transform.localScale *= size;
        this.indicator.gameObject.transform.localScale *= size;

        instantiated = true;
        StartCoroutine(how());
    }

    private void FixedUpdate()
    {
        if (h == true) return;
        if (!instantiated) return;
        if (indicator != null) indicator.transform.position = new Vector2(transform.position.x, camr.position.y);

        if (falling) return;
        transform.position = new Vector2(transform.position.x, playe.position.y + yOffset);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Campfire"))
        {
            StartCoroutine(Debrid(true));
        }
    }

    private IEnumerator how()
    {
        indicator.gameObject.SetActive(true);
        indicator.Play(indicatorAnimName);

        yield return new WaitForSeconds(fallDelay);
        falling = true;
        ough.enabled = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = new Vector2(0, -speed);

        yield return new WaitForSeconds(lifetime);

        StartCoroutine(Debrid(false));
    }

    private IEnumerator Debrid(bool laval)
    {
        if (h) yield break;
        h = true;
        if (!laval)
        {
            imBrok.gameObject.transform.SetParent(null);
            imBrok.Play();
            yield return new WaitUntil(() => !imBrok.isEmitting);
            Destroy(imBrok.gameObject);
        }
        else
        {
            lavar.gameObject.transform.SetParent(null);
            lavar.Play();
            yield return new WaitUntil(() => !lavar.isEmitting);
            Destroy(lavar.gameObject);
        }

        Destroy(indicator.gameObject);

        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }
}
