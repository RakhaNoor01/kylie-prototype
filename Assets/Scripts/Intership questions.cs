using UnityEngine;
using UnityEngine.Events;

public class AwesomeSingleton : MonoBehaviour
{
    public static AwesomeSingleton instance;

    private void Start()
    {
        if (instance == null)
        {
            instance = this; // set self as instance
            DontDestroyOnLoad(gameObject); // persist accross scenes
        }
        else
        {
            Destroy(gameObject); // if instance already exists, destroy
        }

    }

    public void DoThing()
    {
        Debug.Log("thing");
    }
}

// Decoupling
public interface IDoStuff
{
    void DoThing();
}


using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Ensure only one GameManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void HelloWorld()
    {
        Debug.Log("Hello World");
    }
}

public interface IHelloWorld
{
    void HelloWorld();
}