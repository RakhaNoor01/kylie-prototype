
using UnityEngine;
using TarodevController;

public class PlayerAnimator : MonoBehaviour
{
    private Animator _anim;
    private bool _isDead;

    [SerializeField] Transform _visuals;

    public PlayerController _controller;

    public GameObject glider;

    private GameObject gliderOff;
    private GameObject gliderOn;
    private Animator gloffAnim;

    // Glider state tracking
    private bool _gliderEquipped = false;
    private bool _wasGliding = false;
    private bool _isUnequipping = false;

    private void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
        //_controller = GetComponent<PlayerController>();

        gliderOff = glider.transform.Find("off")?.gameObject;
        gliderOn = glider.transform.Find("on")?.gameObject;
        gloffAnim = gliderOff.GetComponent<Animator>();

        gliderOff.GetComponent<SpriteRenderer>().enabled = false;
        gliderOn.GetComponent<SpriteRenderer>().enabled = false;
    }
    private void OnEnable()
    {
        // Make sure controller reference is valid after respawn
        if (_controller == null)
            _controller = GetComponent<PlayerController>();
    }
    public void SetController(PlayerController controller)
    {
        _controller = controller;
    }
    private void Update()
    {
        if (_controller == null) return;
        if (_isDead) return; // dead = skip animations

        HandleAnimations(); // this reads velocity and sets animator
        HandleGliderAnims();
    }
    public void SetGlide(bool isGliding)
    {
        if (_anim != null)
            _anim.SetBool("IsGliding", isGliding);
    }
    public void ResetDeath()
    {
        _isDead = false;
        _anim.ResetTrigger("Dead");
        _anim.SetBool("Respawn", true); // NEW parameter for Animator transitions
        _anim.Play("Idle", 0, 0f);       // force Idle at time 0
        _anim.Update(0f);                // flush Animator state immediately
    }

    public void SetCling(bool clinging)
    {
        if (_anim != null)
            _anim.SetBool("IsClinging", clinging);
    }


    public void SetWallSlide(bool sliding)
    {
        if (_anim != null)
            _anim.SetBool("WallSlide", sliding);
    }


    public void PlayDeath()
    {
        Debug.Log("Death animation triggered!");
        _isDead = true;
        _anim.SetTrigger("Dead");
        _anim.SetBool("Respawn", false);
    }

    public void HandleAnimations()
    {
        Vector2 velocity = _controller.Velocity;
        bool grounded = _controller.Grounded;
        bool isDashing = _controller.IsDashing;
        bool isClinging = _controller.IsClinging;
        bool isWallSliding = _controller.IsWallSliding;
        bool isGliding = _controller.IsGliding;

        _anim.SetFloat("Speed", Mathf.Abs(velocity.x));
        _anim.SetFloat("VerticalSpeed", velocity.y);
        _anim.SetBool("Grounded", grounded);
        _anim.SetBool("Dashing", isDashing);
        _anim.SetBool("DashAvailable", _controller.DashAvailable);
        _anim.SetBool("IsClinging", isClinging);
        _anim.SetBool("IsGliding", isGliding);

        // Set dash direction for 8-way animations
        if (isDashing)
        {
            float dashDirection = GetDashDirectionParameter(velocity);
            _anim.SetFloat("DashDirection", dashDirection);
        }
        else
        {
            _anim.SetFloat("DashDirection", -1);
        }

        HandleFlip(velocity.x);
    }

    private float GetDashDirectionParameter(Vector2 velocity)
    {
        // Returns 0-7 for 8 directions: 0=Right, 1=UpRight, 2=Up, 3=UpLeft, 4=Left, 5=DownLeft, 6=Down, 7=DownRight
        if (velocity.magnitude < 0.01f) return 0; // Return right if no velocity
        
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;

        // Convert angle to 0-7 range
        float direction = Mathf.Round(angle / 45f) % 8;
        return direction;
    }

    private void HandleFlip(float xVelocity)
    {
        if (_controller.IsClinging || _controller.IsWallSliding)
        {
            if (_controller.TouchingLeftWall)
                _visuals.localScale = new Vector3(-1, 1, 1); // face left
            else if (_controller.TouchingRightWall)
                _visuals.localScale = new Vector3(1, 1, 1);  // face right
            return;
        }

        if (Mathf.Abs(xVelocity) > 0.01f)
        {
            _visuals.localScale = new Vector3(Mathf.Sign(xVelocity), 1, 1);
        }
    }

    // glider animation
    private void HandleGliderAnims()
    {
        bool controllerGlider = _controller.Glider;
        bool isGliding = _controller.IsGliding;

        // Equip: Glider just became true
        if (controllerGlider && !_gliderEquipped && !_isUnequipping)
        {
            _gliderEquipped = true;

            glider.SetActive(true);
            gliderOff.GetComponent<SpriteRenderer>().enabled = true;
            gliderOn.GetComponent<SpriteRenderer>().enabled = false;

            gloffAnim.Play("gliderEquip", 0, 0f);
        }

        // Gliding started
        if (isGliding && !_wasGliding && _gliderEquipped)
        {
            gliderOff.GetComponent<SpriteRenderer>().enabled = false;
            gliderOn.GetComponent<SpriteRenderer>().enabled = true;
        }

        // Gliding stopped (but still equipped)
        if (!isGliding && _wasGliding && _gliderEquipped)
        {
            gliderOff.GetComponent<SpriteRenderer>().enabled = true;
            gliderOn.GetComponent<SpriteRenderer>().enabled = false;
        }

        // Unequip: Glider just became false
        if (!controllerGlider && _gliderEquipped && !_isUnequipping)
        {
            _gliderEquipped = false;
            _isUnequipping = true;

            // Show "off" sprite for the unequip anim, hide "on"
            gliderOff.GetComponent<SpriteRenderer>().enabled = true;
            gliderOn.GetComponent<SpriteRenderer>().enabled = false;

            // Unparent so the glider can fly off independently
            glider.transform.SetParent(null);

            gloffAnim.Play("gliderUneq", 0, 0f);

            // Wait for the clip to finish, then reparent and disable
            float clipLength = GetAnimClipLength(gloffAnim, "gliderUneq");
            StartCoroutine(ReparentAfterDelay(clipLength));
        }

        _wasGliding = isGliding;
    }

    private System.Collections.IEnumerator ReparentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        glider.transform.SetParent(gameObject.transform);
        glider.transform.localPosition = Vector2.zero;
        glider.transform.localScale = Vector2.one;
        glider.SetActive(false);

        _isUnequipping = false;
    }

    private float GetAnimClipLength(Animator animator, string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 1f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        Debug.LogWarning($"Clip '{clipName}' not found on {animator.name}, defaulting to 1s");
        return 1f;
    }

}
