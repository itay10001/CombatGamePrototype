using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float sprintMultiplier = 1.6f;
    public float jumpForce = 7f;
    public float dashForce = 15f;
    public float dashCooldown = 0.8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;

    [Header("Landing")]
    public float badLandingThreshold = 45f;
    public float veryBadLandingThreshold = 90f;

    private Rigidbody rb;
    private Transform cameraHolder;
    private float verticalLook;
    private bool isGrounded;
    private float dashTimer;
    private float landingRecovery;
    private HUD hud;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraHolder = transform.Find("CameraHolder");
        hud = FindObjectOfType<HUD>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        MouseLook();
        HandleJump();
        HandleDash();
        dashTimer -= Time.deltaTime;
        landingRecovery -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        HandleMovement();
        CheckGrounded();
    }

    void MouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0, mouseX, 0);

        verticalLook -= mouseY;
        verticalLook = Mathf.Clamp(verticalLook, -80f, 80f);
        cameraHolder.localEulerAngles = new Vector3(verticalLook, 0, 0);
    }

    void HandleMovement()
    {
        if (landingRecovery > 0) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        Vector3 dir = (transform.right * h + transform.forward * v).normalized;
        Vector3 targetVelocity = dir * speed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && dashTimer <= 0)
        {
            Vector3 dashDir = transform.forward;
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (h != 0 || v != 0)
                dashDir = (transform.right * h + transform.forward * v).normalized;

            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            rb.AddForce(dashDir * dashForce, ForceMode.Impulse);
            dashTimer = dashCooldown;
        }
    }

    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    float CalculateLandingScore()
    {
        // Vertical: looking down = good (0), horizontal = bad (0.5), looking up = worst (1)
        float verticalAngle = Vector3.Angle(cameraHolder.forward, Vector3.down);
        float verticalScore = verticalAngle / 180f;

        // Horizontal: facing opposite to movement = good (0), same direction = bad (1)
        Vector3 moveDir = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).normalized;
        Vector3 facingDir = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        float horizontalAngle = Vector3.Angle(facingDir, moveDir);
        float horizontalScore = 1f - (horizontalAngle / 180f);

        // Pitch is primary, yaw can still tip you into bad territory
        float combined = (verticalScore * 0.7f) + (horizontalScore * 0.3f);

        // If pitch is good but yaw is terrible, cap at BAD not CLEAN
        if (verticalScore < 0.33f && horizontalScore > 0.8f)
            return Mathf.Max(combined, 0.34f);

        return combined;
    }


    void OnCollisionEnter(Collision col)
    {
        if (col.relativeVelocity.y > 2f)
        {
            float score = CalculateLandingScore();

            if (score > 0.66f)
            {
                landingRecovery = 1.2f;
                rb.linearVelocity *= 0.2f;
                hud.ShowLanding("VERY BAD LANDING");
            }
            else if (score > 0.33f)
            {
                landingRecovery = 0.5f;
                rb.linearVelocity *= 0.5f;
                hud.ShowLanding("BAD LANDING");
            }
            else
            {
                hud.ShowLanding("CLEAN LANDING");
            }
        }
    }

    public Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }

    public float GetLandingScore()
    {
        return CalculateLandingScore();
    }

    public Vector3 GetCameraForward()
    {
        return cameraHolder.forward;
    }
}
