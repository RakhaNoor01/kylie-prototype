using UnityEngine;

public class TreeReaction : MonoBehaviour
{
    private Animator animator;
    private float playerEnterX;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            playerEnterX = collision.transform.position.x;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            TriggerBlow(collision.transform.position.x);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerEnterX = other.transform.position.x;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            TriggerBlow(other.transform.position.x);
    }

    void TriggerBlow(float exitX)
    {
        if (exitX > playerEnterX)
            animator.SetTrigger("BlowLeft");
        else
            animator.SetTrigger("BlowRight");
    }
}