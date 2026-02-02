using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Auto Run")]
    [SerializeField] private float runSpeed = 6.5f;
    [SerializeField] private bool movePlayerForward = false;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Slash")]
    [SerializeField] private float slashRange = 1.2f;
    [SerializeField] private float slashRadius = 0.6f;
    [SerializeField] private LayerMask slashTargets;

    [Header("Parry")]
    [SerializeField] private float parryRange = 1.1f;
    [SerializeField] private float parryRadius = 0.7f;
    [SerializeField] private LayerMask parryTargets;
    [SerializeField] private float hitStopSeconds = 0.1f;
    [SerializeField] private float reflectSpeed = 12f;
    [SerializeField] private float perfectShockwaveRadius = 6f;
    [SerializeField] private LayerMask shockwaveTargets;

    [Header("Rhythm")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float beatWindowSeconds = 0.12f;
    [SerializeField] private float perfectWindowSeconds = 0.05f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float comboZoomPerStack = 0.03f;
    [SerializeField] private float minOrthoSize = 4.5f;

    [Header("Input")]
    [SerializeField] private KeyCode slashKey = KeyCode.J;
    [SerializeField] private KeyCode parryKey = KeyCode.K;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Score")]
    [SerializeField] private int scorePerSlash = 100;
    [SerializeField] private int scorePerParry = 250;
    [SerializeField] private int scorePerPerfectParry = 600;

    public float RunSpeed => GameSpeedController.Speed > 0f ? GameSpeedController.Speed : runSpeed;

    private Rigidbody2D rb;
    private Animator anim;
    private float songStartTime;
    private float baseOrthoSize;
    private int combo;
    private int score;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        if (targetCamera != null && targetCamera.orthographic)
        {
            baseOrthoSize = targetCamera.orthographicSize;
        }
        else
        {
            baseOrthoSize = 5f;
        }
    }

    private void Start()
    {
        songStartTime = Time.time;
    }

    private void Update()
    {
        UpdateGrounded();
        HandleInput();
        UpdateCameraZoom();
    }

    private void FixedUpdate()
    {
        float targetX = movePlayerForward ? RunSpeed : 0f;
        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(jumpKey) || Input.GetButtonDown("Jump"))
        {
            TryJump();
        }

        if (Input.GetKeyDown(slashKey) || Input.GetButtonDown("Fire1"))
        {
            TrySlash();
        }

        if (Input.GetKeyDown(parryKey) || Input.GetButtonDown("Fire2"))
        {
            TryParry();
        }
    }

    private void TryJump()
    {
        if (!isGrounded)
        {
            return;
        }
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        anim.SetTrigger("Jump");
        Debug.Log("Jump!");
    }

    private void TrySlash()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(GetSlashCenter(), slashRadius, slashTargets);
        if (hits.Length == 0)
        {
            ResetCombo();
            return;
        }

        bool hitSomething = false;
        foreach (Collider2D hit in hits)
        {
            hitSomething = true;
            hit.SendMessage("OnSlashHit", SendMessageOptions.DontRequireReceiver);
        }

        if (hitSomething)
        {
            combo++;
            score += scorePerSlash;
        }
    }

    private void TryParry()
    {
        float beatInterval = 60f / Mathf.Max(1f, bpm);
        float phase = (Time.time - songStartTime) % beatInterval;
        float distanceToBeat = Mathf.Min(phase, beatInterval - phase);
        bool onBeat = distanceToBeat <= beatWindowSeconds;
        bool perfect = distanceToBeat <= perfectWindowSeconds;

        Collider2D[] hits = Physics2D.OverlapCircleAll(GetParryCenter(), parryRadius, parryTargets);
        if (hits.Length == 0)
        {
            ResetCombo();
            return;
        }

        if (!onBeat)
        {
            ResetCombo();
            return;
        }

        foreach (Collider2D hit in hits)
        {
            hit.SendMessage("OnParry", SendMessageOptions.DontRequireReceiver);
            ReflectProjectile(hit);
        }

        combo++;
        score += perfect ? scorePerPerfectParry : scorePerParry;
        StartCoroutine(HitStop());

        if (perfect)
        {
            Shockwave();
        }
    }

    private void ReflectProjectile(Collider2D hit)
    {
        Rigidbody2D hitRb = hit.attachedRigidbody;
        if (hitRb == null)
        {
            return;
        }

        Vector2 reflectDir = Vector2.right;
        hitRb.linearVelocity = reflectDir * reflectSpeed;
    }

    private IEnumerator HitStop()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopSeconds);
        Time.timeScale = originalTimeScale;
    }

    private void Shockwave()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, perfectShockwaveRadius, shockwaveTargets);
        foreach (Collider2D hit in hits)
        {
            hit.SendMessage("OnPerfectParryShockwave", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void ResetCombo()
    {
        combo = 0;
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = rb.linearVelocity.y == 0f;
            UpdateAnimatorGrounded();
            return;
        }
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer) != null;
        UpdateAnimatorGrounded();
    }

    private void UpdateAnimatorGrounded()
    {
        if (anim == null)
        {
            return;
        }

        anim.SetBool("IsGrounded", isGrounded);
    }

    private void UpdateCameraZoom()
    {
        if (targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        float targetSize = Mathf.Max(minOrthoSize, baseOrthoSize - combo * comboZoomPerStack);
        targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, targetSize, Time.deltaTime * 6f);
    }

    private Vector2 GetSlashCenter()
    {
        return (Vector2)transform.position + Vector2.right * slashRange;
    }

    private Vector2 GetParryCenter()
    {
        return (Vector2)transform.position + Vector2.right * parryRange;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetSlashCenter(), slashRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(GetParryCenter(), parryRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perfectShockwaveRadius);
    }
}
