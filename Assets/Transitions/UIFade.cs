using UnityEngine;

public class UIFadeManager : MonoBehaviour
{
    public static UIFadeManager Instance { get; private set; }

    private Animator _anim;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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