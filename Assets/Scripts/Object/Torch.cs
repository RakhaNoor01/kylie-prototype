using UnityEngine;

public class Torch : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var hi = collision.gameObject.GetComponent<Slopburger>();
        if (hi != null && hi.TheOrch == false)
        {
            hi.TsFunction(true);
            gameObject.SetActive(false);
        }
    }
}
