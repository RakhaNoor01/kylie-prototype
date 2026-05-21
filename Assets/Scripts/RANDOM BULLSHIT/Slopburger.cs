using UnityEngine;

public class Slopburger : MonoBehaviour
{
    private void Update()
    {
        Shader.SetGlobalVector("_playerPos", gameObject.transform.position);
    }
}
