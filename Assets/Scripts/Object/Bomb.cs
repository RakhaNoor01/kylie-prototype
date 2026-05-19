using System.Collections;
using TarodevController;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Bomb : MonoBehaviour
{
    public float power = 15;
    public float push = 1;
    public float iframes = 0.15f;

    [Header("Visual")]
    public float minDistance = 1.5f;
    public float followSpeed = 20;
    public float effectDuration = 1;
    public GameObject splosion;

    public static GameObject theBobm;
    private GameObject player;
    private PlayerHealth hi;
    private bool detonated = false;

    private void Start()
    {
        splosion.SetActive(false);
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
            // Launch upward + nudge away from the death zone wall
            float xNudge = deathZoneDirection.x * push;
            pc.SetFrameVelocity(new Vector2(xNudge, power));
            pc.CancelDash();
        }

        theBobm = null;
        hi = null;

        StartCoroutine(Whoa());
    }

    private IEnumerator Whoa()
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

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
}
