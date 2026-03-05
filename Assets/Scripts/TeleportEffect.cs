using System.Collections;
using UnityEngine;
using DG.Tweening;

public class TeleportEffect : MonoBehaviour
{
    public float effectDuration = 0.3f;
    public float trailDuration = 0.1f;
    public GameObject trailEffect;
    public GameObject teleportEffect;
    public GameObject player;

    public void teleport()
    {
        var tpEffect = Instantiate(teleportEffect, transform);

        tpEffect.SetActive(true);
        tpEffect.GetComponent<Animator>().Play("tpeffect");
        tpEffect.transform.parent = null;

        StartCoroutine(dude(tpEffect));
    }

    IEnumerator dude(GameObject test)
    {
        yield return new WaitForSeconds(effectDuration);

        Destroy(test);
    }

    public void ToggleTrail(bool state)
    {
        if (trailEffect == null) return;

        TrailRenderer[] trails = trailEffect.GetComponentsInChildren<TrailRenderer>();

        foreach (TrailRenderer trail in trails)
        {
            trail.emitting = state;
        }
    }
}