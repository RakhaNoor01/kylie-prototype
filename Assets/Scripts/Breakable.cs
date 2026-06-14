using UnityEngine;

public class Breakable : MonoBehaviour
{
    public int health = 1;
    public string boomerangTag = "Goonerang";
    public ParticleSystem article;
    public GameObject fart;

    private int currentHealth;

    private void Start()
    {
        currentHealth = health;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(boomerangTag))
        {
            Hit();
        }
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
        fart.SetActive(false);
    }
}
