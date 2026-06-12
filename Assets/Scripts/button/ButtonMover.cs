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

    private Vector3 ogPos;
    private Quaternion ogRot;

    private void Awake()
    {
        ogPos = transform.position;
        ogRot = transform.rotation;
    }

    public override void SetState(bool setTo)
    {
        base.SetState(setTo);

        transform.DOKill();

        Vector3 destinationPos = state ? target.position : ogPos;
        Quaternion destinationRot = state ? target.rotation : ogRot;

        float duration = moveDur;

        if (useSpeed)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            duration = moveSpeed > 0f ? distance / moveSpeed : 0f;
        }

        Ease easing = state ? onEasing : offEasing;

        transform.DOMove(destinationPos, duration).SetEase(easing);
        transform.DORotate(destinationRot.eulerAngles, duration).SetEase(easing);
    }
}
