using System;
using UnityEngine;

public class Torch : MonoBehaviour
{
    public bool spawnTorch = false;
    private Slopburger the;
    private static GameObject gongobject;
    public static event Action getSpawnTorch;

    private void Start()
    {
        gongobject = null;
        the = Slopburger.instance;
        getSpawnTorch += AlreadyTorch;
    }

    private void OnDestroy()
    {
        getSpawnTorch -= AlreadyTorch;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (the.TheOrch == false)
        {
            gongobject = this.gameObject;
            the.TsFunction(true);
            gameObject.SetActive(false);
            getSpawnTorch?.Invoke();
            Debug.Log($"{gameObject.name}");
        }
    }

    private void AlreadyTorch()
    {
        if (gongobject != this.gameObject && spawnTorch && the.TheOrch)
        {
            gameObject.SetActive(false);
        }
    }
}
