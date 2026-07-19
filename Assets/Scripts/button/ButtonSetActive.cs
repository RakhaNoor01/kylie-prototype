using UnityEngine;

public class ButtonSetActive : ButtonTarget
{
    public GameObject thing;

    public override void SetState(bool setTo)
    {
        base.SetState(setTo);
        thing.SetActive(state);
    }
}
