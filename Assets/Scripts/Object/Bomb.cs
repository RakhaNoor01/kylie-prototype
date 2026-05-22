using System.Collections;
using TarodevController;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Bomb : MonoBehaviour
{
    public float power = 15;
    public float push = 1;
    public float iframes = 0.15f;
    public float splosionRadius = 2f;

    [Header("Visual")]
    public float minDistance = 1.5f;
    public float followSpeed = 20;
    public float effectDuration = 1;
    public GameObject splosion;

    public static GameObject theBobm;
    private GameObject player;
    private PlayerHealth hi;
    public bool detonated = false;
    private CircleCollider2D col;

    private void Start()
    {
        splosion.SetActive(false);
        col = GetComponent<CircleCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && theBobm == null)
        {
            player = collision.gameObject;
            Collect();
        }
    }

    private void Collect()
    {
        // parent to player then unparent
        // so that it can freely move in the world and stays in the persistent scene
        gameObject.transform.parent = player.transform;
        gameObject.transform.parent = null;
        theBobm = gameObject;

        col.enabled = false;
        hi = player.gameObject.GetComponent<PlayerHealth>();

    }

    private void Update()
    {
        if (theBobm == gameObject)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist > minDistance)
            {
                Vector2 dir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                Vector2 targetPos = (Vector2)player.transform.position + dir * minDistance;
                transform.position = Vector2.Lerp(transform.position, targetPos, followSpeed * Time.fixedDeltaTime);
            }
        }
    }

    public void Detonate(Vector2 deathZoneDirection)
    {
        if (player == null || detonated == true) return;
        detonated = true;

        hi.AddIframes(iframes);

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            float xNudge = deathZoneDirection.x * push;
            pc.SetFrameVelocity(new Vector2(xNudge, power));
            pc.CancelDash();
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, splosionRadius);
        foreach (var hit in hits)
        {
            var breakable = hit.GetComponent<Breakable>();
            if (breakable != null)
            {
                breakable.HitFromBomb();
            }
        }

        theBobm = null;
        hi = null;

        StartCoroutine(Whoa());
    }

    private IEnumerator Whoa()
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;

        splosion.SetActive(true);
        splosion.transform.parent = null;
        splosion.transform.position = player.transform.position;

        var anim = splosion.GetComponent<Animator>();
        if (anim != null)
        {
            anim.Play("splode");
        }

        yield return new WaitForSeconds(effectDuration);

        Destroy(splosion);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, splosionRadius);
    }
}
