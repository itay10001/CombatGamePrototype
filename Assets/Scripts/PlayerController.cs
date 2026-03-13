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
    public float badLandingThreshold = 0.33f;
    public float veryBadLandingThreshold = 0.66f;

    [Header("Wall Running")]
    public float wallRunTime = 1.5f;
    public float wallGravity = 15f;
    public float wallJumpForce = 12f;
    public float wallJumpUpForce = 12f;
    public float wallCheckDistance = 0.8f;
    public float wallOrientSpeed = 8f;

    private Rigidbody rb;
    private Transform cameraHolder;
    private float verticalLook;
    private bool isGrounded;
    private bool isAirborne;
    private float dashTimer;
    private float landingRecovery;
    private float recoveryDuration;
    private HUD hud;
    private bool accessibilityMode = false;

    // Wall running
    private bool isWallRunning;
    private float wallRunTimer;
    private Vector3 wallNormal;
    private bool isOnWall;
    private Quaternion targetPlayerRotation;
    private Quaternion normalPlayerRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraHolder = transform.Find("CameraHolder");
        hud = FindObjectOfType<HUD>();
        Cursor.lockState = CursorLockMode.Locked;
        normalPlayerRotation = transform.rotation;
    }

    void Update()
    {
        MouseLook();
        HandleJump();
        HandleDash();
        HandleRecovery();
        HandleWallRun();
        dashTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.T))
            accessibilityMode = !accessibilityMode;
    }

    void FixedUpdate()
    {
        HandleMovement();
        CheckGrounded();
        CheckWall();
    }

    void MouseLook()
    {
        if (landingRecovery > 0) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (isWallRunning)
        {
            // On wall: yaw rotates along wall surface, pitch is normal
            transform.Rotate(0, mouseX, 0);
            verticalLook -= mouseY;
            verticalLook = Mathf.Clamp(verticalLook, -80f, 80f);
            cameraHolder.localRotation = Quaternion.Euler(verticalLook, 0, 0);
        }
        else if (!isAirborne || accessibilityMode)
        {
            transform.Rotate(0, mouseX, 0);
            verticalLook -= mouseY;
            verticalLook = Mathf.Clamp(verticalLook, -80f, 80f);
            cameraHolder.localRotation = Quaternion.Euler(verticalLook, 0, 0);
        }
        else
        {
            transform.Rotate(0, mouseX, 0);
            cameraHolder.localRotation *= Quaternion.Euler(-mouseY, 0, 0);
            verticalLook = cameraHolder.localEulerAngles.x;
            if (verticalLook > 180f) verticalLook -= 360f;
        }
    }

    void HandleMovement()
    {
        if (landingRecovery > 0) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        if (isWallRunning)
        {
            // Move along the wall as if it's ground
            Vector3 wallUp = -wallNormal; // into the wall = down on wall
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up).normalized;
            Vector3 wallRight = Vector3.Cross(wallForward, -wallNormal).normalized;

            Vector3 dir = (wallRight * h + wallForward * v).normalized;
            rb.linearVelocity = dir * speed;

            // Apply gravity toward the wall
            rb.AddForce(wallNormal * -wallGravity, ForceMode.Acceleration);
        }
        else
        {
            Vector3 dir = (transform.right * h + transform.forward * v).normalized;
            Vector3 targetVelocity = dir * speed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isAirborne = true;
            }
            else if (isWallRunning || isOnWall)
            {
                // Launch away from wall + momentum + up
                Vector3 jumpDir = wallNormal;
                jumpDir += rb.linearVelocity.normalized * 0.5f;
                jumpDir.y = 0;
                jumpDir.Normalize();

                rb.linearVelocity = Vector3.zero;
                rb.AddForce(jumpDir * wallJumpForce + Vector3.up * wallJumpUpForce, ForceMode.Impulse);

                // Exit wall run
                ExitWallRun();
                isAirborne = true;
            }
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

    void HandleRecovery()
    {
        if (landingRecovery > 0)
        {
            landingRecovery -= Time.deltaTime;
            verticalLook = Mathf.Lerp(verticalLook, 0f, Time.deltaTime * (1f / recoveryDuration) * 3f);
            cameraHolder.localRotation = Quaternion.Euler(verticalLook, 0, 0);
        }
    }

    void HandleWallRun()
    {
        if (!isWallRunning) return;

        wallRunTimer -= Time.deltaTime;

        // Smoothly rotate player to treat wall as floor
        Quaternion wallOrientation = Quaternion.LookRotation(
            Vector3.Cross(wallNormal, Vector3.up),
            -wallNormal
        );
        transform.rotation = Quaternion.Slerp(transform.rotation, wallOrientation, Time.deltaTime * wallOrientSpeed);

        if (wallRunTimer <= 0)
            ExitWallRun();
    }

    void ExitWallRun()
    {
        isWallRunning = false;
        rb.useGravity = true;

        // Smoothly snap player rotation back to upright
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    }

    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        if (!isGrounded)
            isAirborne = true;
        else
        {
            if (isWallRunning) ExitWallRun();
            isAirborne = false;
        }
    }

    void CheckWall()
    {
        if (isGrounded) return;

        RaycastHit hit;
        Vector3[] directions = { transform.right, -transform.right, transform.forward };

        isOnWall = false;

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out hit, wallCheckDistance))
            {
                isOnWall = true;
                wallNormal = hit.normal;

                if (!isWallRunning && rb.linearVelocity.magnitude > 2f)
                {
                    isWallRunning = true;
                    wallRunTimer = wallRunTime;
                    rb.useGravity = false;
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                }
                return;
            }
        }

        isOnWall = false;
        if (isWallRunning) ExitWallRun();
    }

    float CalculateLandingScore()
    {
        float verticalAngle = Vector3.Angle(cameraHolder.forward, Vector3.down);
        float verticalScore = verticalAngle / 180f;

        Vector3 moveDir = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).normalized;
        Vector3 facingDir = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        float horizontalAngle = Vector3.Angle(facingDir, moveDir);
        float horizontalScore = 1f - (horizontalAngle / 180f);

        float combined = (verticalScore * 0.7f) + (horizontalScore * 0.3f);

        if (verticalScore < 0.33f && horizontalScore > 0.8f)
            return Mathf.Max(combined, 0.34f);

        return combined;
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.relativeVelocity.y > 2f)
        {
            float score = CalculateLandingScore();

            if (score > veryBadLandingThreshold)
            {
                recoveryDuration = 1.2f;
                landingRecovery = recoveryDuration;
                rb.linearVelocity *= 0.2f;
                hud.ShowLanding("VERY BAD LANDING");
            }
            else if (score > badLandingThreshold)
            {
                recoveryDuration = 0.5f;
                landingRecovery = recoveryDuration;
                rb.linearVelocity *= 0.5f;
                hud.ShowLanding("BAD LANDING");
            }
            else
            {
                verticalLook = Mathf.Clamp(verticalLook, -80f, 80f);
                cameraHolder.localRotation = Quaternion.Euler(verticalLook, 0, 0);
                hud.ShowLanding("CLEAN LANDING");
            }
        }
    }

    public Vector3 GetVelocity() => rb.linearVelocity;
    public float GetLandingScore() => CalculateLandingScore();
    public Vector3 GetCameraForward() => cameraHolder.forward;
    public bool IsAirborne() => isAirborne;
    public bool IsWallRunning() => isWallRunning;
}
