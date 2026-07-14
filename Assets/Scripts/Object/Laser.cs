using UnityEngine;

public class Laser : MonoBehaviour
{
    public GameObject endThing;
    private LineRenderer line;
    private ContactFilter2D filter;
    private RaycastHit2D[] hits = new RaycastHit2D[1];

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;

        filter.useTriggers = false;
        filter.SetLayerMask(LayerMask.GetMask("Ground", "Player"));
        filter.useLayerMask = true;
    }

    private void FixedUpdate()
    {
        int hitCount = Physics2D.Raycast(transform.position, transform.right, filter, hits, 32);
        line.SetPosition(0, transform.position);

        if (hitCount > 0)
        {
            line.SetPosition(1, hits[0].point);
            if (endThing != null)
            {
                endThing.SetActive(true);
                endThing.transform.position = hits[0].point;
            }
            var ob = hits[0].collider.gameObject;
            if (ob.CompareTag("Player"))
            {
                ob.GetComponent<PlayerHealth>().Die();
            }
        }
        else
        {
            endThing.SetActive(false);
            line.SetPosition(1, transform.position + transform.right * 32);
        }
    }
}
