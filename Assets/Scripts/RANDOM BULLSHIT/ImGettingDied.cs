using UnityEngine;

public class ImGettingDied : MonoBehaviour
{
    private void OnDestroy()
    {
        Debug.Log($"{gameObject.name} is died");
    }
}
