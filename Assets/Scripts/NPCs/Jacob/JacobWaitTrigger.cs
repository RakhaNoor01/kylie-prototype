using UnityEngine;

public class JacobWaitTrigger : MonoBehaviour
{
    [SerializeField] private Cobber cob;

    private bool used;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (used)
            return;

        if (!collision.CompareTag("Player"))
            return;

        used = true;

        cob.ContinuePath();
    }
}
