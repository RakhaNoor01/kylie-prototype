using UnityEngine;

public class MovingPlatformTarget : MonoBehaviour
{
    // lowk just a data container lol

    public Transform target => transform;
    public float waitTime = 0f;
    public float speed = 0f;
    public bool instant = false;
    public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
