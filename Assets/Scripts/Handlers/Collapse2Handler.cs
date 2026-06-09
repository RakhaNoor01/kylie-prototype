using System.Collections;
using UnityEngine;

public class Collapse2Handler : MonoBehaviour
{
    public CollapseDebris debris;
    public Animator indicator;
    public float interval;
    public float minRange = -12;
    public float maxRange = 12;
    public float ySpawn = 12;
    [Range(0f, 1f)]
    public float playerTargetChance = 0.25f;
    public ExtraTags stoppa;

    private Transform camera;
    private bool started = false;
    private bool isCollapse = false;
    private Transform player;

    private void Start()
    {
        camera = Camera.main.gameObject.transform;
        debris.gameObject.SetActive(false);
        indicator.gameObject.SetActive(false);
        player = Slopburger.instance.gameObject.transform;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !started)
        {
            started = true;
            isCollapse = true;
            StartCoroutine(Collapse2());
        }
    }

    private void Update()
    {
        var col = stoppa.colInfo;
        if (col == null) return;

        if (col.gameObject.CompareTag("Player"))
        {
            isCollapse = false;
        }
    }

    private IEnumerator Collapse2()
    {
        CameraShake.Instance.Shake(0.75f, 0.5f, -1);

        while (isCollapse)
        {
            float xPos;

            if (player != null && Random.value < playerTargetChance)
            {
                xPos = player.position.x;
            }
            else
            {
                xPos = camera.position.x + Random.Range(minRange, maxRange);
            }

            var startpos = new Vector3(
                xPos,
                camera.position.y + ySpawn,
                0
            );

            var thing = Instantiate(debris, startpos, Quaternion.identity);
            thing.gameObject.SetActive(true);

            var indicator = Instantiate(
                this.indicator,
                startpos - new Vector3(0, ySpawn, 0),
                Quaternion.identity
            );

            thing.Yummy(camera, ySpawn, indicator);

            yield return new WaitForSeconds(interval);
        }
    }
}
