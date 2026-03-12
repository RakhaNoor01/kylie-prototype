using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class ButtonMover : ButtonTarget
{
    public Transform target;
    public float moveDur;

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
            transform.DOMove(target.position, moveDur);
            transform.DORotate(target.rotation.eulerAngles, moveDur);
        } 
        else if (!state)
        {
            transform.DOMove(ogPos, moveDur);
            transform.DORotate(ogRot.eulerAngles, moveDur);
        }
    }
}
