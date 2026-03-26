//using UnityEngine;
//using TarodevController;

//[RequireComponent(typeof(Animator))]
//public class PlayerAnimator : MonoBehaviour
//{
//    private Animator _anim;
//    public PlayerController _controller;

//    [SerializeField] Transform _visuals;

//    private void Awake()
//    {
//        _anim = GetComponent<Animator>();
//    }

//    private void Update()
//    {
//        HandleAnimations();
//    }

//    private void HandleAnimations()
//    {
//        Vector2 velocity = _controller.Velocity;
//        bool grounded = _controller.Grounded;
//        bool isDashing = _controller.IsDashing;

//        _anim.SetFloat("Speed", Mathf.Abs(velocity.x));
//        _anim.SetFloat("VerticalSpeed", velocity.y);
//        _anim.SetBool("Grounded", grounded);
//        _anim.SetBool("Dashing", isDashing);
//        _anim.SetBool("DashAvailable", _controller.DashAvailable);

//        // Set dash direction for 8-way animations
//        if (isDashing)
//        {
//            float dashDirection = GetDashDirectionParameter(velocity);
//            _anim.SetFloat("DashDirection", dashDirection);
//        }
//        else
//        {
//            _anim.SetFloat("DashDirection", -1);
//        }

//        HandleFlip(velocity.x);
//    }

//    private float GetDashDirectionParameter(Vector2 velocity)
//    {
//        // Returns 0-7 for 8 directions: 0=Right, 1=UpRight, 2=Up, 3=UpLeft, 4=Left, 5=DownLeft, 6=Down, 7=DownRight
//        if (velocity.magnitude < 0.01f) return 0; // Return right if no velocity
        
//        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
//        if (angle < 0) angle += 360;

//        // Convert angle to 0-7 range
//        float direction = Mathf.Round(angle / 45f) % 8;
//        return direction;
//    }

//    private void HandleFlip(float xVelocity)
//    {
//        if (Mathf.Abs(xVelocity) < 0.01f) return;

//        _visuals.localScale = new Vector3(
//            Mathf.Sign(xVelocity),
//            1,
//            1
//        );
//    }

//}
