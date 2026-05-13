using UnityEngine;

public class LookThisWay : MonoBehaviour
{
    public static Transform Cam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void Awake()
    {
        Cam = GetComponent<Camera>().transform;
    }
}
