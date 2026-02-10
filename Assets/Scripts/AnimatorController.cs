using UnityEngine;
using TarodevController;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _anim;
    private PlayerController _controller;

    [SerializeField] Transform _visuals;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        HandleAnimations();
    }

    private void HandleAnimations()
    {
        Vector2 velocity = _controller.Velocity;
        bool grounded = _controller.Grounded;

        _anim.SetFloat("Speed", Mathf.Abs(velocity.x));
        _anim.SetFloat("VerticalSpeed", velocity.y);
        _anim.SetBool("Grounded", grounded);

        HandleFlip(velocity.x);
    }

    private void HandleFlip(float xVelocity)
    {
        if (Mathf.Abs(xVelocity) < 0.01f) return;

        _visuals.localScale = new Vector3(
            Mathf.Sign(xVelocity),
            1,
            1
        );
    }

}
