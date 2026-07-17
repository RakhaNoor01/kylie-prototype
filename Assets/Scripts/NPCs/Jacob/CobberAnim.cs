using UnityEngine;

public class CobberAnim : MonoBehaviour
{
    [SerializeField] private Cobber controller;
    [SerializeField] private Transform visuals;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        Vector2 velocity = controller.Velocity;

        anim.SetFloat("Speed", Mathf.Abs(velocity.x));
        anim.SetInteger("State", (int)controller.State);

        HandleFlip(velocity.x);

        Debug.Log(controller.State);
    }

    private void HandleFlip(float x)
    {
        if (Mathf.Abs(x) < 0.01f)
            return;

        visuals.localScale = new Vector3(-Mathf.Sign(x), 1, 1);
    }
}