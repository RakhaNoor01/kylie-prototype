using UnityEngine;

public class ButtonScreenshake : ButtonTarget
{
    public float duration;
    public float magnitude;
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
