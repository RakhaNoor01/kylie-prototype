using UnityEngine;

public class ButtonTarget : MonoBehaviour
{
    public Button button;
    protected bool state;

    private void Start()
    {
        if (button != null)
            button.RegisterTarget(this);
    }

    public virtual void SetState(bool setTo)
    {
        state = setTo;
    }
}