using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ContraptionVisual : MonoBehaviour
{
    private static ContraptionHandler handler;
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        handler = ContraptionHandler.Instance;
    }

    private void LateUpdate()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float progress = handler.LightCount / handler.maxLightCount;
        animator.SetFloat("Light", progress);
    }
}
