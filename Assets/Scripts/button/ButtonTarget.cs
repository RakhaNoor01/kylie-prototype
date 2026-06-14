using UnityEngine;

public class ButtonTarget : MonoBehaviour
{
    public Button button;
    protected bool state;
    public bool inverted;

    public virtual void Awake()
    {
        if (button != null)
            button.RegisterTarget(this);
    }

    public virtual void SetState(bool state)
    {
        this.state = inverted ? !state : state;
    }
}