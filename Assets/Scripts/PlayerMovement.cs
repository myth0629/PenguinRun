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

    [Header("Slide")]
    [SerializeField] private float slideColliderHeight = 0.5f;
    [SerializeField] private Vector2 slideColliderOffset = new Vector2(0f, -0.25f);

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

    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int AnimIsSliding = Animator.StringToHash("isSliding");
    private static readonly int AnimIsFalling = Animator.StringToHash("isFalling");
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
        bool slidePressed = slideAction != null && slideAction.action.WasPerformedThisFrame();
        bool slideHeld = slideAction != null && slideAction.action.IsPressed();

        UpdateGrounded();
        HandleJumpInput(jumpPressed);
        HandleSlideInput(slidePressed, slideHeld);
        UpdateFalling();
        UpdateAnimator();
    }

    private void HandleJumpInput(bool jumpPressed)
    {
        if (isSliding)
        {
            return;
        }

        if (isGrounded && jumpPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetTrigger(AnimJump);
        }
    }

    private void HandleSlideInput(bool slidePressed, bool slideHeld)
    {
        if (!isGrounded)
        {
            return;
        }

        if (!isSliding && slidePressed)
        {
            StartSlide();
        }

        if (isSliding && !slideHeld)
        {
            EndSlide();
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
