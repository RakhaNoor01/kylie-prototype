using DG.Tweening;
using UnityEngine;

public class ButtonMover : ButtonTarget
{
    public Transform target;
    public float moveDur = 1;
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

        if (state)
        {
            transform.DOMove(target.position, moveDur).SetEase(onEasing);
            transform.DORotate(target.rotation.eulerAngles, moveDur).SetEase(onEasing);
        } 
        else if (!state)
        {
            transform.DOMove(ogPos, moveDur).SetEase(offEasing);
            transform.DORotate(ogRot.eulerAngles, moveDur).SetEase(offEasing);
        }
    }
}
