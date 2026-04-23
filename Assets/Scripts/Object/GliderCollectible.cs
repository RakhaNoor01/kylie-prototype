using TarodevController;
using UnityEngine;

public class GliderCollectible : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var troller = collision.gameObject.GetComponent<PlayerController>();

        if (troller != null && troller.Glider != true)
        {
            troller.Glider = true;
            gameObject.SetActive(false);
        }
    }
}
