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
        teleportEffect.SetActive(true);
        teleportEffect.GetComponent<Animator>().Play("tpeffect");
        teleportEffect.transform.parent = null;

        StartCoroutine(dude());
    }

    IEnumerator dude()
    {
        yield return new WaitForSeconds(effectDuration);

        teleportEffect.SetActive(false);
        teleportEffect.transform.position = player.transform.position;
        teleportEffect.transform.parent = player.transform;
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