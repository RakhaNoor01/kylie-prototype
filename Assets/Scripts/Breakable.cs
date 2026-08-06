using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Breakable : MonoBehaviour
{
    public int health = 1;
    public string boomerangTag = "Goonerang";
    public ParticleSystem article;
    public string breakableID;

    [Header("Damage Source")]
    public bool boomerang = true;
    public bool burnerang = false;
    public bool bomb = false;

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
        var the = gameObject.GetComponent<SpriteRenderer>();
        if (the == null)
        {
            var the2 = gameObject.GetComponent<TilemapRenderer>();
            the2.enabled = false;
        }
        else
        {
            the.enabled = false;
        }
        
        if (fart != null) fart.SetActive(false);
    }
}
