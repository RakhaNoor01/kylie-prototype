using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Slopburger : MonoBehaviour
{
    private void Update()
    {
        Shader.SetGlobalVector("_playerPos", gameObject.transform.position);
    }
}
