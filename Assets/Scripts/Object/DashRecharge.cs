using System;
using UnityEngine;
using TarodevController;

[RequireComponent(typeof(Collider2D))]
public class DashRecharge : MonoBehaviour
{
    // optional event that other systems can listen for (particles, sound, etc.)
    public event Action<DashRecharge> Collected;
    public float respawnTime = 3f;

    private float respawnTimer;
    private SpriteRenderer sprite;
    private Collider2D col;
    private ParticleSystem ticle;
    private bool yummers;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        sprite = GetComponent<SpriteRenderer>();
        ticle = GetComponent<ParticleSystem>();
    }

    private void FixedUpdate()
    {
        if (respawnTimer > 0)
        {
            respawnTimer -= Time.deltaTime;
        } 
        else
        {
            yummers = true;
            Toggle(yummers);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // try interface first for flexibility
        var player = other.GetComponent<IPlayerController>();
        if (player == null)
        {
            // fallback to concrete type
            player = other.GetComponent<PlayerController>();
        }

        if (player != null && yummers)
        {
            player.RechargeDash();
            Collected?.Invoke(this);
            respawnTimer = respawnTime;
            yummers = false;
            Toggle(yummers);
        }
    }

    private void Toggle(bool toggle)
    {
        sprite.color = toggle ? new Color(1, 1, 1, 1) : new Color(0, 0, 0, 0.5f);
        col.enabled = toggle;
        if (!toggle)
        {
            ticle.Play();
        }
    }
}