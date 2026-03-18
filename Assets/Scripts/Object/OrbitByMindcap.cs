using DG.Tweening;
using TarodevController;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class OrbitByMindcap : MonoBehaviour
{
    public float power = 6.7f;
    public float cooldown = 1.5f;

    private PlayerController controller;
    private Collider2D col;
    private Collider2D playercol;
    private SpriteRenderer sr;
    private ParticleSystem particle;
    private Vector3 ogSize;
    private float cdTimer;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        ogSize = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        particle = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (playercol != null && !col.IsTouching(playercol))
        {
            controller = null;
            playercol = null;
        }

        if (Input.GetButtonDown("Jump") && controller != null && cdTimer <= 0)
        {
            UseAnim();
            controller.ApplyBounce(power);
            cdTimer = cooldown;
        }
        else if (cdTimer > 0)
        {
            cdTimer -= Time.deltaTime;
            if (cdTimer <= 0)
            {
                OffCDAnim();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc != null)
        {
            controller = pc;
            playercol = collision;
        }
    }

    private void UseAnim()
    {
        particle.Stop();
        transform.DOKill();
        sr.DOKill();
        transform.DOScale(ogSize*1.4f, 0.1f).SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                transform.DOScale(ogSize * 0.5f, 0.3f).SetEase(Ease.InOutCubic);
                sr.DOFade(0.5f, 0.4f);
            });
    }

    private void OffCDAnim()
    {
        particle.Play();
        transform.DOKill();
        sr.DOKill();
        transform.DOScale(ogSize, 0.4f).SetEase(Ease.InOutCubic);
        sr.DOFade(1f, 0.4f);
    }
}