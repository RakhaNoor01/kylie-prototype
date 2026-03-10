using UnityEngine;

public class UIFadeManager : MonoBehaviour
{
    private Animator _anim;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    public void PlayFadeOut()
    {
        _anim.SetTrigger("FadeOut");
    }

    public void PlayFadeIn()
    {
        _anim.SetTrigger("FadeIn");
    }
}
