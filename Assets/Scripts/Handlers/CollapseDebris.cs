using System.Collections;
using TarodevController;
using UnityEngine;

public class CollapseDebris : MonoBehaviour
{
    public float fallSpeed = 10;
    public float fallDelay = 0.5f;
    public float lifetime = 5;
    public string indicatorAnimName;

    private bool instantiated = false;
    private float yOffset;
    private Transform camr;
    private Transform playe;
    private Rigidbody2D rb;
    private bool falling = false;
    private Animator indicator;
    private Collider2D ough;

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

        instantiated = true;
        StartCoroutine(how());
    }

    private void FixedUpdate()
    {
        if (!instantiated) return;
        indicator.transform.position = new Vector2(transform.position.x, camr.position.y);

        if (falling) return;
        transform.position = new Vector2(transform.position.x, playe.position.y + yOffset);
    }

    private IEnumerator how()
    {
        indicator.gameObject.SetActive(true);
        indicator.Play(indicatorAnimName);

        yield return new WaitForSeconds(fallDelay);
        falling = true;
        ough.enabled = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = new Vector2(0, -fallSpeed);

        yield return new WaitForSeconds(lifetime);
        Destroy(indicator);
        Destroy(gameObject);
    }
}
