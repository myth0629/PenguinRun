using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D bodyCollider;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Variable Jump (가변 점프)")]
    [Tooltip("버튼을 떼면 상승 속도가 이 비율로 감소 (0.5 = 절반으로 줄임)")]
    [SerializeField, Range(0.1f, 1f)] private float jumpCutMultiplier = 0.4f;
    
    [Tooltip("점프 컷이 적용되는 최소 상승 속도")]
    [SerializeField] private float minJumpCutVelocity = 0.1f;

    [Header("Coyote Time (코요테 타임)")]
    [Tooltip("플랫폼에서 떨어진 후에도 점프 가능한 시간 (초)")]
    [SerializeField] private float coyoteTime = 0.15f;
    
    [Header("Jump Buffering (점프 버퍼링)")]
    [Tooltip("착지 직전 점프 입력을 저장하는 시간 (초)")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Slide")]
    [SerializeField] private float slideColliderHeight = 0.5f;
    [SerializeField] private Vector2 slideColliderOffset = new Vector2(0f, -0.25f);
    
    [Header("Slide Buffering (슬라이드 버퍼링)")]
    [Tooltip("점프 중 슬라이드 입력을 저장하는 시간 (초)")]
    [SerializeField] private float slideBufferTime = 0.2f;

    [Header("Fall")]
    [SerializeField] private float fallVelocityThreshold = -0.1f;

    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference slideAction;

    private float defaultColliderHeight;
    private Vector2 defaultColliderOffset;
    private bool isGrounded;
    private bool isSliding;
    private bool isFalling;
    private bool isJumping;
    private bool jumpReleased;
    
    // 코요테 타임 & 점프 버퍼링 타이머
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float slideBufferCounter;
    private bool wasGroundedLastFrame;

    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int AnimIsSliding = Animator.StringToHash("isSliding");
    private static readonly int AnimIsFalling = Animator.StringToHash("isFalling");
    private static readonly int AnimIsJumping = Animator.StringToHash("isJumping");
    private static readonly int AnimJump = Animator.StringToHash("jump");
    private static readonly int AnimSlide = Animator.StringToHash("slide");

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        CacheDefaultCollider();
    }

    private void OnEnable()
    {
        jumpAction?.action.Enable();
        slideAction?.action.Enable();
    }

    private void OnDisable()
    {
        jumpAction?.action.Disable();
        slideAction?.action.Disable();
    }

    private void Update()
    {
        bool jumpPressed = jumpAction != null && jumpAction.action.WasPerformedThisFrame();
        bool jumpHeld = jumpAction != null && jumpAction.action.IsPressed();
        bool slidePressed = slideAction != null && slideAction.action.WasPerformedThisFrame();
        bool slideHeld = slideAction != null && slideAction.action.IsPressed();

        UpdateGrounded();
        UpdateCoyoteTime();
        UpdateJumpBuffer(jumpPressed);
        UpdateSlideBuffer(slidePressed);
        HandleJumpInput(jumpPressed, jumpHeld);
        HandleSlideInput(slidePressed, slideHeld);
        UpdateFalling();
        UpdateAnimator();
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        wasGroundedLastFrame = isGrounded;
    }

    private void UpdateJumpBuffer(bool jumpPressed)
    {
        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void UpdateSlideBuffer(bool slidePressed)
    {
        if (slidePressed)
        {
            slideBufferCounter = slideBufferTime;
        }
        else
        {
            slideBufferCounter -= Time.deltaTime;
        }
    }

    private void HandleJumpInput(bool jumpPressed, bool jumpHeld)
    {
        // 슬라이드 중 점프 입력 -> 슬라이드 종료 후 점프 (선입력)
        if (isSliding)
        {
            if (jumpBufferCounter > 0f)
            {
                EndSlide();
                // 점프 바로 실행
            }
            else
            {
                return;
            }
        }

        // 점프 시작 (코요테 타임 + 점프 버퍼링 적용)
        bool canJump = coyoteTimeCounter > 0f && !isJumping;
        bool wantsJump = jumpBufferCounter > 0f;
        
        if (canJump && wantsJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetTrigger(AnimJump);
            isJumping = true;
            jumpReleased = false;
            
            // 타이머 초기화 (더블 점프 방지)
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
        }

        // 점프 중 버튼을 떼면 상승 속도 감소 (가변 점프)
        if (isJumping && !jumpHeld && !jumpReleased)
        {
            jumpReleased = true;
            
            // 아직 상승 중이면 속도 감소
            if (rb.linearVelocity.y > minJumpCutVelocity)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        // 착지하면 점프 상태 리셋
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            isJumping = false;
            jumpReleased = false;
        }
    }

    private void HandleSlideInput(bool slidePressed, bool slideHeld)
    {
        // 슬라이드 중인데 키를 뗐으면 즉시 종료 (최우선)
        if (isSliding && !slideHeld)
        {
            EndSlide();
            slideBufferCounter = 0f; // 버퍼도 초기화
            return;
        }

        // 공중에서는 버퍼만 저장하고 리턴
        if (!isGrounded)
        {
            return;
        }

        // 슬라이드 시작 조건: 버퍼에 입력이 있거나 지금 눌렀고, 키를 누르고 있어야 함
        bool wantsSlide = (slideBufferCounter > 0f || slidePressed) && slideHeld;
        
        if (!isSliding && wantsSlide && !isJumping)
        {
            StartSlide();
            slideBufferCounter = 0f; // 버퍼 소모
        }
    }

    private void StartSlide()
    {
        isSliding = true;
        ApplySlideCollider();
        animator.SetTrigger(AnimSlide);
    }

    private void EndSlide()
    {
        isSliding = false;
        RestoreDefaultCollider();
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = bodyCollider != null && bodyCollider.IsTouchingLayers(groundLayer);
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(AnimIsGrounded, isGrounded);
        animator.SetBool(AnimIsSliding, isSliding);
        animator.SetBool(AnimIsFalling, isFalling);
        animator.SetBool(AnimIsJumping, isJumping);
    }

    private void UpdateFalling()
    {
        if (rb == null)
        {
            isFalling = false;
            return;
        }

        isFalling = !isGrounded && rb.linearVelocity.y < fallVelocityThreshold;
    }

    private void CacheDefaultCollider()
    {
        if (bodyCollider is CapsuleCollider2D capsule)
        {
            defaultColliderHeight = capsule.size.y;
            defaultColliderOffset = capsule.offset;
        }
        else if (bodyCollider is BoxCollider2D box)
        {
            defaultColliderHeight = box.size.y;
            defaultColliderOffset = box.offset;
        }
    }

    private void ApplySlideCollider()
    {
        if (bodyCollider is CapsuleCollider2D capsule)
        {
            capsule.size = new Vector2(capsule.size.x, slideColliderHeight);
            capsule.offset = slideColliderOffset;
        }
    }

    private void RestoreDefaultCollider()
    {
        if (bodyCollider is CapsuleCollider2D capsule)
        {
            capsule.size = new Vector2(capsule.size.x, defaultColliderHeight);
            capsule.offset = defaultColliderOffset;
        }
        else if (bodyCollider is BoxCollider2D box)
        {
            box.size = new Vector2(box.size.x, defaultColliderHeight);
            box.offset = defaultColliderOffset;
        }
    }
}
