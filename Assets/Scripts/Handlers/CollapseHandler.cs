using System.Collections;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.Rendering;

public class CollapseHandler : MonoBehaviour
{
    public Breakable doomPillar;

    [Header("Shake")]
    public float duration;
    public float mag;

    [Header("Collapse")]
    public float startDelay;

    public GameObject collapsePillar;
    public Transform startPos;
    public Transform pillarSpawn;

    public Animator indicator;
    public float indicatorLength;
    public string indicatorAnimName;

    public float spawnInterval;
    public ExtraTags playerEndCollapse;
    public ExtraTags collapseForward;
    public GameObject lastPillar;

    private bool started = false;
    public bool isCollapse = false;
    private bool fastForward = false;
    private static bool ended = false;

    private Button gumbo;
    private Vector2 spawnOffset;
    private float totalXOffset = 0f;

    public static CollapseHandler instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (!ended)
        {
           gumbo = GetComponent<Button>();
           spawnOffset = pillarSpawn.position - startPos.position;
        }
        else
        {
            lastPillar.SetActive(true);
        }

        collapsePillar.SetActive(false);
        indicator.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (doomPillar.IsDestroyed && !started)
        {
            started = true;
            isCollapse = true;
            Collapse();
        }

        if (!started) return;
        CollapseEndPlayer();
        CollapseFastForward();
    }

    private void CollapseEndPlayer()
    {
        var col = playerEndCollapse.colInfo;
        if (col == null) return;

        if (col.gameObject.CompareTag("Player"))
        {
            ended = true;

            Vector3 pos = startPos.position;
            pos.x = lastPillar.transform.position.x;
            startPos.position = pos;

            totalXOffset = 0;
        }
    }

    private void CollapseFastForward()
    {
        if (fastForward) return;

        var col = collapseForward.colInfo;
        if (col == null) return;

        if (col.gameObject.CompareTag("Player"))
        {
            fastForward = true;

            Vector3 pos = startPos.position;
            pos.x = collapseForward.transform.position.x;
            startPos.position = pos;

            totalXOffset = 0f;
        }
    }

    private void Collapse()
    {
        gumbo.TriggerButton();
        CameraShake.Instance.Shake(duration, mag, -1);
        StartCoroutine(SpawnCollapsePillars());
    }

    private IEnumerator SpawnCollapsePillars()
    {
        yield return new WaitForSeconds(startDelay);

        totalXOffset = 0;

        while (isCollapse)
        {
            Vector3 spawnPos = startPos.position +
                               new Vector3(totalXOffset, spawnOffset.y, 0);
            totalXOffset += spawnOffset.x;

            var indicator = Instantiate(this.indicator, spawnPos - new Vector3(0, spawnOffset.y), Quaternion.identity);
            indicator.gameObject.SetActive(true);

            indicator.Play(indicatorAnimName);

            yield return new WaitForSeconds(indicatorLength);

            var pillar = Instantiate(collapsePillar, spawnPos, Quaternion.identity);
            pillar.SetActive(true);

            yield return new WaitForSeconds(spawnInterval);

            Destroy(indicator);
        }
    }
}
