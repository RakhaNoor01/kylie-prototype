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
    public bool hasTimer = false;
    public float timer = 6;
    public int damage = 1;
    public float respawnDelay = 3;
    public string bombID;
    public bool nuke = false;

    [Header("Visual")]
    public float minDistance = 1.5f;
    public float followSpeed = 20;
    public float effectDuration = 1;
    public float finalPulseDuration = 0.25f;
    public GameObject splosion;
    public GameObject fuse;
    public Animator pulse;
    public string splodeAnimName = "splode";

    public static GameObject theBobm;
    private GameObject player;
    private PlayerHealth hi;
    public bool detonated = false;
    private bool ignited = false;
    private Collider2D col;
    private ParticleSystem farticle;
    private Vector2 spawnPos;

    private float realTimer = 0;

    private void Start()
    {
        spawnPos = transform.position;

        if (TempData.HasKey(bombID))
        {
            Destroy(gameObject);
        }

        splosion.SetActive(false);
        col = GetComponent<Collider2D>();
        fuse.SetActive(false);
        farticle = fuse.GetComponent<ParticleSystem>();

        if (nuke && TempData.HasKey("nuke_collect"))
        {
            var ok = Slopburger.instance.gameObject.transform.position;
            gameObject.transform.position = ok;
        }
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

        if (hasTimer)
        {
            ignited = true;
            fuse.SetActive(true);
            farticle.Play();
            pulse.Play("bomb_pulse");
        }
        else
        {
            pulse.Play("bomb_pickup");
        }

        if (nuke)
        {
            TempData.SetValue("nuke_collect", true);
        }
    }

    private void Update()
    {
        if (hasTimer && ignited)
        {
            handleBobmTimer();
        }
        handleBobmFollow();
    }

    private void handleBobmTimer()
    {
        var tim = timer + finalPulseDuration;

        if (realTimer >= tim)
        {
            Detonate(false);
        }

        if (realTimer >= timer)
        {
            pulse.Play("bomb_finalpulse");
        }

        realTimer += Time.deltaTime;
    }

    private void handleBobmFollow()
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

    public bool Detonate(bool detonateThing)
    {
        if (nuke && !detonateThing) return false;

        if (player == null || detonated == true) return false;
        detonated = true;
        ignited = false;

        hi.AddIframes(iframes);

        transform.position = player.transform.position;

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SetFrameVelocity(new Vector2(0, power));
            pc.CancelDash();
        }

        bool hitBreakable = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, splosionRadius);
        RaycastHit2D[] rayHits = new RaycastHit2D[1];
        foreach (var hit in hits)
        {
            var breakable = hit.GetComponent<Breakable>();
            if (breakable == null)
                continue;

            Vector2 origin = transform.position;
            Vector2 target = hit.bounds.center;

            Vector2 direction = (target - origin).normalized;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.useTriggers = false;
            filter.SetLayerMask(~LayerMask.GetMask("Player", "Goonerang"));

            Physics2D.Raycast(
                origin,
                direction,
                filter,
                rayHits,
                splosionRadius
            );

            if (rayHits[0].collider == hit)
            {
                hitBreakable = true;
                breakable.HitFromBomb(damage);

                if (!string.IsNullOrEmpty(bombID))
                {
                    TempData.SetValue(bombID, "hello vro");
                }
            }
        }

        theBobm = null;
        hi = null;
        pulse.gameObject.SetActive(false);
        fuse.SetActive(false);

        StartCoroutine(Whoa(hitBreakable));

        return true;
    }

    private IEnumerator Whoa(bool hitBreakable)
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;

        splosion.SetActive(true);
        splosion.transform.parent = null;
        splosion.transform.position = player.transform.position;

        var anim = splosion.GetComponent<Animator>();
        if (anim != null)
        {
            anim.Play(splodeAnimName);
        }

        yield return new WaitForSeconds(effectDuration);

        if (hitBreakable)
        {
            Destroy(gameObject);
            Destroy(splosion);
            yield break;
        }

        yield return new WaitForSeconds(respawnDelay);

        // reset bomb
        transform.position = spawnPos;
        sr.enabled = true;

        detonated = false;
        realTimer = 0f;
        player = null;
        hi = null;

        col.enabled = true;
        splosion.SetActive(false);
        pulse.gameObject.SetActive(true);
        pulse.Play("bomb_respawn");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, splosionRadius);
    }
}
