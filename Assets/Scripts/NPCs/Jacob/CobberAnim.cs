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

    // Update is called once per frame
    private void Update()
    {
        Vector2 velocity = controller.Velocity;

        anim.SetFloat("Speed", Mathf.Abs(velocity.x));
        if (controller.State == Cobber.JacobState.Jumping)
        {
            anim.SetTrigger("Jump");
        }

        HandleFLip(velocity.x);
    }

    private void HandleFLip(float x)
    {
        if (Mathf.Abs(x) < 0.01f)
        {
            return;
        }

        visuals.localScale = new Vector3(Mathf.Sign(-x), 1, 1);
    }
}
