using System.Collections;
using System.Collections.Specialized;
using TarodevController;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

    public bool started = false;
    public bool isCollapse = false;
    public bool fastForward = false;
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
        if (ended)
        {
            lastPillar.SetActive(true);
        }

        gumbo = GetComponent<Button>();
        spawnOffset = pillarSpawn.position - startPos.position;

        collapsePillar.SetActive(false);
        indicator.gameObject.SetActive(false);
    }

    private void Update()
    {
        bool normalStart = doomPillar.IsDestroyed;

        bool ffStart =
            TempData.HasKey("fastforward") &&
            PlayerController.Instance.FirstInput;

        if ((normalStart || ffStart) && !started)
        {
            started = true;
            isCollapse = true;

            if (TempData.HasKey("fastforward"))
            {
                CameraShake.Instance.Shake(0.5f, 0.4f, -1);
                Invoke(nameof(Collapse), spawnInterval);
            }
            else
            {
                Collapse();
            }
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

            TempData.SetValue("fastforward", true);
        }
    }

    private void Collapse()
    {
        if (!TempData.HasKey("fastforward"))
        {
            CameraShake.Instance.Shake(duration, mag, -1);
        }
        gumbo.TriggerButton();
        StartCoroutine(SpawnCollapsePillars());
    }

    private IEnumerator SpawnCollapsePillars()
    {
        float endTime = Time.time + startDelay;

        yield return new WaitUntil(() =>
            fastForward || Time.time >= endTime
        );

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
