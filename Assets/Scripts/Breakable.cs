using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    public int health = 1;
    public string boomerangTag = "Goonerang";
    public ParticleSystem article;
    public GameObject fart;

    private int currentHealth;
    public bool IsDestroyed => currentHealth <= 0;

    private void Start()
    {
        currentHealth = health;

        if (TempData.GetValue(breakableID) != null)
        {
            currentHealth = (int)TempData.GetValue(breakableID);
        }

        if (currentHealth <= 0)
        {
            FUCK();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var bobm = collision.gameObject.GetComponent<Bomb>();

        var isBoomerang = collision.gameObject.CompareTag(boomerangTag);
        var theRang = collision.gameObject.GetComponent<Boomerang>();

        if ((theRang != null && theRang.isBurning && burnerang) || (isBoomerang && boomerang))
        {
            Hit(1);
        }
    }

    public void HitFromBomb(int damage)
    {
        if (!bomb) return;
        Hit(damage);
    }

    private void Hit(int damage)
    {
        currentHealth -= damage;

        if (!string.IsNullOrEmpty(breakableID))
        {
            TempData.SetValue(breakableID, currentHealth);
        }

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
