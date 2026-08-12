using TarodevController;
using UnityEngine;

public class DashEnabler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private bool triggered;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered || !collision.CompareTag("Player"))
            return;

        triggered = true;

        PlayerController.Instance.SetDashEnabled(true);
    }
}
