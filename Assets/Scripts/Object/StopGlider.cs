using TarodevController;
using UnityEngine;

public class StopGlider : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var troller = collision.gameObject.GetComponent<PlayerController>();

        if (troller != null)
        {
            troller.Glider = false;
            troller.OhTheMisery();
        }
    }
}
