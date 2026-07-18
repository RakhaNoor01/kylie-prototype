#if UNITY_EDITOR
using UnityEditor;
#endif

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
#if UNITY_EDITOR
        EditorUtility.CopySerialized(imGonnaCopyYou, ts);
#endif
    }
}