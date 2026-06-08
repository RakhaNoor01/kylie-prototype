using UnityEngine;

public class CollapseHandler : MonoBehaviour
{
    public Breakable doomPillar;

    [Header("Shake")]
    public float duration;
    public float mag;

    private bool isCollapsing = false;

    private void Update()
    {
        if (doomPillar.IsDestroyed && !isCollapsing)
        {
            isCollapsing = true;
            Collapse();
        }
    }

    private void Collapse()
    {
        CameraShake.Instance.Shake(duration, mag, -1);
    }
}
