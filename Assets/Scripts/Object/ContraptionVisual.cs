using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ContraptionVisual : MonoBehaviour
{
    private static ContraptionHandler handler;
    private Animator animator;
    private bool idling = true;

    private static event Action enterTraption;
    private static event Action exitTraption;


    private void OnEnable()
    {
        enterTraption += TraptionOn;
        exitTraption += TraptionOff;
    }

    private void OnDisable()
    {
        enterTraption -= TraptionOn;
        exitTraption -= TraptionOff;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        if (!handlin()) return;
        if (handler.LightCount <= 0 && !idling)
        {
            animator.CrossFadeInFixedTime("contraption_idle", 0.5f);
            idling = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (handler == null) handler = collision.gameObject.GetComponent<ContraptionHandler>();
        if (!handlin()) return;

        TraptionOn();
        enterTraption?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (!handlin()) return;

        TraptionOff();
        exitTraption?.Invoke();
    }

    private void TraptionOn()
    {
        animator.CrossFadeInFixedTime("contraption_enter", 0.2f);
        idling = false;
    }

    private void TraptionOff()
    {
        animator.CrossFadeInFixedTime("contraption_off", 0.25f);
    }

    private bool handlin()
    {
        if (handler == null || handler.enabled == false) return false;
        return true;
    }
}
