using Unity.VisualScripting;
using UnityEngine;

public class Starter : MonoBehaviour
{
    [SerializeField] private Cobber jacob;
    [SerializeField] private JacobNode firstNode;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Begin();
        }
    }
    public void Begin()
    {
        jacob.transform.position = firstNode.transform.position;
        jacob.StartRun(firstNode);
    }

    public void ResetSequence()
    {
        jacob.transform.position = firstNode.transform.position;
        jacob.StartRun(firstNode);
    }
}
