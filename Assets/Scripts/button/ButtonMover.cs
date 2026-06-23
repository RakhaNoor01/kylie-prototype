using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class ButtonMover : ButtonTarget
{
    public Transform target;
    public float moveDur = 1;

    [Header("Speed")]
    public bool useSpeed = false;
    public float moveSpeed = 1;

    public Ease onEasing = Ease.Linear;
    public Ease offEasing = Ease.Linear;

    public bool useCustomEase = false;
    public AnimationCurve customEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 ogPos;
    private Quaternion ogRot;
    private float distance;
    private bool init = false;

    public override void Awake()
    {
        base.Awake();
        ogPos = transform.position;
        ogRot = transform.rotation;
        distance = Vector3.Distance(transform.position, target.transform.position);
    }

    public override void SetState(bool setTo)
    {
        base.SetState(setTo);

        transform.DOKill();

        Vector3 destinationPos = state ? target.position : ogPos;
        Quaternion destinationRot = state ? target.rotation : ogRot;

        if (!init)
        {
            init = true;
            transform.SetPositionAndRotation(destinationPos, destinationRot);
            return;
        }

        float duration = moveDur;

        if (useSpeed)
        {
            duration = moveSpeed > 0f ? distance / moveSpeed : 0f;
        }

        Tween moveTween = transform.DOMove(destinationPos, duration);
        Tween rotateTween = transform.DORotate(destinationRot.eulerAngles, duration);

        if (useCustomEase)
        {
            moveTween.SetEase(customEase);
            rotateTween.SetEase(customEase);
        }
        else
        {
            Ease easing = state ? onEasing : offEasing;
            moveTween.SetEase(easing);
            rotateTween.SetEase(easing);
        }
    }
}
