using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FadingThing : MonoBehaviour
{
    public float fadeSeconds = 0.5f;
    public float fadeOpacity = 0;
    private TilemapRenderer tilemapRenderer;

    private void Start()
    {
        tilemapRenderer = GetComponent<TilemapRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            tilemapRenderer.material.DOFade(fadeOpacity, fadeSeconds);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            tilemapRenderer.material.DOFade(1, fadeSeconds);
        }
    }
}