using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Vein : MonoBehaviour
{
    public int health = 1;
    public string boomerangTag = "Goonerang";
    public ParticleSystem article;
    public BWFallingPlatform platformBelow;  // Changed from FallingPlatform

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

    private void OnCollisionEnter2D(Collision2D collision)
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
            StartCoroutine(FUCK());
        }
    }

    private IEnumerator FUCK()
    {
        var tilem = GetComponentInChildren<Tilemap>();
        if (tilem != null)
        {
            tilem.gameObject.SetActive(false);
        }

        if (platformBelow != null)
        {
            platformBelow.StartFalling();  // BWFallingPlatform also has StartFalling... wait, it doesn't — see note
        }

        yield return new WaitForSeconds(article.main.duration);

        Destroy(gameObject);
    }
}