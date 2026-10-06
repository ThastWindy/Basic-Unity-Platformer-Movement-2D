using System.Collections;
using Unity.Tutorials.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;

public class PlayerMovement : MonoBehaviour
{

    Rigidbody2D _body;
    
    // Ground Check
    [Header("Ground Check")]
    [SerializeField] Transform GroundChecker;
    [SerializeField] LayerMask GroundLayer;
    [SerializeField] float GroundCheckRadius = 0.2f;
    bool _isGrounded;

    // Wall Check
    [Header("Wall Check")]
    [SerializeField] Transform WallChecker;
    [SerializeField] LayerMask WallLayer;
    [SerializeField] float WallCheckRadius = 0.2f;
    bool _isWalled;

    // Move
    [Header("Move")]
    [SerializeField] float MoveSpeed = 5f;
    float _moveDirection;

    // Flip
    bool _isFacingRight = true;

    // Jump
    [Header("Jump")]
    [SerializeField] float JumpHeight = 3f;
    [SerializeField] float GravityScale = 5f;
    [SerializeField] float FallGravityScale = 10f;
    
    // Wall Slide
    [Header("Wall Slide")]
    [SerializeField] float WallSlideSpeed = 0.2f;
    bool _isWallSliding;
    
    // Wall Jump
    [Header("Wall Jump")]
    [SerializeField] Vector2 WallJumpPower = new Vector2 (8f, 16f);
    [SerializeField] float WallJumpDuration = 0.4f;
    [SerializeField] float _wallJumpTime = 0.2f;
    bool _isWallJumping;
    float _wallJumpDirection;
    float _wallJumpCounter;

    // Dash
    [Header("Dashing")]
    [SerializeField] float DashingPower = 24f;
    [SerializeField] float DashingDuration = 0.2f;
    [SerializeField] float DashingCooldown = 1f;
    bool _canDash;
    bool _isDashing;

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        
    }

    void Update()
    {

        // Special
        if (_isDashing) return;

        // Move
        if (!_isWallJumping)
        _body.linearVelocityX = _moveDirection * MoveSpeed;

        // Flip (ref to _moveDirection)
        if (_isFacingRight && _moveDirection < 0f || !_isFacingRight && _moveDirection > 0f)
        {
            if (!_isWallJumping)
            Flip();
        }

        // Jump
        GroundCheck();

        if (_body.linearVelocity.y > 0)
        {
            _body.gravityScale = GravityScale;
        }
        else
        {
            _body.gravityScale = FallGravityScale;
        }

        // Wall
        WallCheck();

        // Wall Slide
        if (_isWalled && !_isGrounded && _moveDirection != 0)
        {
            _isWallSliding = true;
            _body.linearVelocityY = Mathf.Clamp(_body.linearVelocityY, -WallSlideSpeed, float.MaxValue);
        }
        else
        {
            _isWallSliding = false;
        }

        // Wall Jump
        if (_isWallSliding)
        {
            _isWallJumping = false;
            _wallJumpDirection = -transform.localScale.x;
            _wallJumpCounter = _wallJumpTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            _wallJumpCounter -= Time.deltaTime;
        }
    }

    // ==========

    void GroundCheck()
    {
        _isGrounded = Physics2D.OverlapCircle(GroundChecker.position, GroundCheckRadius, GroundLayer);
    }

    void WallCheck()
    {
        _isWalled = Physics2D.OverlapCircle(WallChecker.position, WallCheckRadius, WallLayer);
    }

    void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    void StopWallJumping()
    {
        _isWallJumping = false;
    }

    IEnumerator Dash()
    {
        _canDash = false;
        _isDashing = true;
        float originalGravity = _body.gravityScale;
        _body.gravityScale = 0f;
        _body.linearVelocity = new Vector2(transform.localScale.x * DashingPower, 0f);
        yield return new WaitForSeconds(DashingDuration);
        _body.gravityScale = originalGravity;
        _isDashing = false;
        yield return new WaitForSeconds(DashingCooldown);
        _canDash = true;
    }

    // ==========

    public void Move(InputAction.CallbackContext context)
    {
        _moveDirection = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {   
        if (context.performed)
        {
            // Ground Jump
            if (_isGrounded)
            {
                _body.gravityScale = GravityScale;
                float _jumpForce = Mathf.Sqrt(JumpHeight * ((Physics2D.gravity.y * _body.gravityScale) * -2f) * _body.mass);

                _body.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
            }

            // Wall Jump
            if (_wallJumpCounter > 0f)
            {
                _isWallJumping = true;
                _body.linearVelocity = new Vector2(_wallJumpDirection * WallJumpPower.x, WallJumpPower.y);
                _wallJumpCounter = 0f;

                if (transform.localScale.x != _wallJumpDirection)
                {
                    Flip();
                }

                Invoke(nameof(StopWallJumping), WallJumpDuration);
            }
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            StartCoroutine(Dash());
        }
    }
}
