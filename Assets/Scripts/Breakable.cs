using UnityEngine;

public class Breakable : MonoBehaviour
{
    public int health = 1;
    public string boomerangTag = "Goonerang";
    public ParticleSystem article;

    [Header("Damage Source")]
    public bool boomerang = true;
    public bool burnerang = false;
    public bool bomb = false;

    private int currentHealth;

    private void Start()
    {
        currentHealth = health;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var bobm = collision.gameObject.GetComponent<Bomb>();

        var isBoomerang = collision.gameObject.CompareTag(boomerangTag);
        var theRang = collision.gameObject.GetComponent<Boomerang>();

        if ((theRang != null && theRang.isBurning && burnerang) || (isBoomerang && boomerang))
        {
            Hit();
        }
    }

    public void HitFromBomb()
    {
        if (!bomb) return;
        Hit();
    }

    private void Hit()
    {
        currentHealth -= 1;
        if (article != null)
        {
            article.Play();
        }
        if (currentHealth <= 0)
        {
            FUCK();
        }
    }

    private void FUCK()
    {
        gameObject.GetComponent<Collider2D>().enabled = false;
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
    }
}
