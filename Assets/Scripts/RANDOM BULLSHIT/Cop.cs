using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Cop : MonoBehaviour
{
    public Light2D imGonnaCopyYou;
    private Light2D ts;

    private void Start()
    {
        ts = GetComponent<Light2D>();
    }

    private void Update()
    {
        EditorUtility.CopySerialized(imGonnaCopyYou, ts);
    }
}
