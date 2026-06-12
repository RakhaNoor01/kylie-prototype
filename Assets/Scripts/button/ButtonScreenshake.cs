using UnityEngine;

public class ButtonScreenshake : ButtonTarget
{
    public float duration = 0.5f;
    public float magnitude = 0.5f;
    public float magDecay = -1;

    public override void SetState(bool setTo)
    {
        base.SetState(setTo);

        if (state)
        {
            CameraShake.Instance.Shake(duration, magnitude, magDecay);
        }
    }
}
