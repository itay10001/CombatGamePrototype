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
    public float dangerFallSpeed = 8f;
    public float recoveryWindow = 0.6f;

    [Header("Wall Running")]
    public float wallGravity = 20f;
    public float wallJumpForce = 12f;
    public float wallJumpUpForce = 12f;
    public float wallCheckDistance = 0.8f;
    public float wallOrientSpeed = 10f;

    private Rigidbody rb;
    private Transform cameraHolder;
    public float verticalLook;
    public float cameraRoll;
    private float targetCameraRoll = 0f;

    private bool isGrounded;
    private bool isAirborne;
    private float dashTimer;
    private float landingRecovery;
    private float recoveryDuration;
    private HUD hud;
    private bool accessibilityMode = false;

    private bool isWallRunning;
    private Vector3 wallNormal;
    private bool isOnWall;

    private bool recoveryAvailable = false;
    private bool recoveryUsed = false;
    private float recoveryTimer = 0f;
    private bool isStunned = false;
    private float stunTimer = 0f;
    private bool hasLanded = false;

    // Save score while falling so it's accurate at landing
    private float savedLandingScore = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraHolder = transform.Find("CameraHolder");
        hud = FindFirstObjectByType<HUD>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleJump();
        HandleDash();
        HandleRecovery();
        HandleStun();
        HandleRecoveryInput();
        CheckDangerousLanding();
        MouseLook();
        dashTimer -= Time.deltaTime;

        // Continuously save score while falling
        if (isAirborne && !isWallRunning)
            savedLandingScore = CalculateLandingScore();

        if (Input.GetKeyDown(KeyCode.T))
            accessibilityMode = !accessibilityMode;
    }

    void FixedUpdate()
    {
        HandleMovement();
        CheckGrounded();
        ApplyWallGravity();
    }

    void MouseLook()
    {
        cameraRoll = Mathf.Lerp(cameraRoll, targetCameraRoll, Time.deltaTime * wallOrientSpeed);

        if (landingRecovery > 0 || isStunned)
        {
            verticalLook = Mathf.Lerp(verticalLook, 0f, Time.deltaTime * 3f);
            cameraHolder.localRotation = Quaternion.Euler(verticalLook, 0, cameraRoll);
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0, mouseX, 0);
        verticalLook -= mouseY;

        if (!isAirborne || accessibilityMode)
            verticalLook = Mathf.Clamp(verticalLook, -80f, 80f);

        cameraHolder.localRotation = Quaternion.Euler(verticalLook, 0, cameraRoll);
    }

   void HandleMovement()
{
    if (landingRecovery > 0 || isStunned) return;

    float h = Input.GetAxisRaw("Horizontal");
    float v = Input.GetAxisRaw("Vertical");
    float speed = moveSpeed;
    if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

    if (isWallRunning)
    {
        Vector3 forward = Vector3.ProjectOnPlane(cameraHolder.forward, wallNormal).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraHolder.right, wallNormal).normalized;
        Vector3 dir = (right * h + forward * v).normalized;
        rb.linearVelocity = dir * speed;
    }
    else if (isAirborne)
    {
        // Air movement — fully camera relative so flipping changes directions
        Vector3 dir = (cameraHolder.right * h + cameraHolder.forward * v).normalized;
        rb.AddForce(dir * speed * 0.5f, ForceMode.Acceleration);
        // Cap air speed
        Vector3 flatVel = rb.linearVelocity;
        if (flatVel.magnitude > speed)
            rb.linearVelocity = flatVel.normalized * speed;
    }
    else
    {
        // Ground movement — yaw only
        Vector3 dir = (transform.right * h + transform.forward * v).normalized;
        Vector3 targetVelocity = dir * speed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }
}


    void ApplyWallGravity()
    {
        if (isWallRunning)
            rb.AddForce(-wallNormal * wallGravity, ForceMode.Acceleration);
    }

    void HandleJump()
    {
        if (isStunned) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                hasLanded = false;
                savedLandingScore = 0f;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isAirborne = true;
            }
            else if (isWallRunning || isOnWall)
            {
                hasLanded = false;
                savedLandingScore = 0f;
                Vector3 jumpDir = (wallNormal + Vector3.up).normalized;
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(jumpDir * wallJumpForce + Vector3.up * wallJumpUpForce, ForceMode.Impulse);
                ExitWallRun();
                isAirborne = true;
            }
        }
    }

    void HandleDash()
    {
        if (isStunned) return;

        if (Input.GetKeyDown(KeyCode.LeftControl) && dashTimer <= 0)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 dashDir = transform.forward;
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
            landingRecovery -= Time.deltaTime;

        if (recoveryAvailable)
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0)
            {
                recoveryAvailable = false;
                recoveryUsed = false;
                hud.HideRecovery();
            }
        }
    }

    void HandleStun()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0)
            {
                isStunned = false;
                hud.HideStun();
            }
        }
    }

    void HandleRecoveryInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && recoveryAvailable && !recoveryUsed)
        {
            recoveryUsed = true;
            recoveryAvailable = false;
            hud.HideRecovery();
            hud.ShowLanding("RECOVERY!");
        }
    }

    void CheckDangerousLanding()
    {
        if (!isAirborne || isWallRunning) return;

        float fallSpeed = -rb.linearVelocity.y;
        if (fallSpeed < dangerFallSpeed) return;

        float score = CalculateLandingScore();

        RaycastHit hit;
        float timeToGround = float.MaxValue;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f))
            timeToGround = hit.distance / fallSpeed;

        if (score > badLandingThreshold && timeToGround < recoveryWindow && !recoveryAvailable && !recoveryUsed)
        {
            recoveryAvailable = true;
            recoveryTimer = timeToGround;
            hud.ShowRecovery();
        }
    }

    void CheckGrounded()
    {
        if (isWallRunning)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        if (!isGrounded)
            isAirborne = true;
        else
            isAirborne = false;
    }

    void ExitWallRun()
    {
        isWallRunning = false;
        rb.useGravity = true;
        targetCameraRoll = 0f;
        isOnWall = false;
    }

    void OnCollisionStay(Collision col)
    {
        if (isGrounded) return;

        foreach (ContactPoint contact in col.contacts)
        {
            if (Mathf.Abs(Vector3.Dot(contact.normal, Vector3.up)) < 0.3f)
            {
                isOnWall = true;
                wallNormal = contact.normal;

                if (!isWallRunning)
                {
                    isWallRunning = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, wallNormal);
                    targetCameraRoll = Vector3.Dot(wallNormal, -transform.right) > 0 ? 90f : -90f;
                }
                return;
            }
        }
    }

    void OnCollisionExit(Collision col)
    {
        isOnWall = false;
        if (isWallRunning) ExitWallRun();
    }

    float CalculateLandingScore()
    {
        Vector3 velDir = rb.linearVelocity.normalized;
        float camVelAngle = Vector3.Angle(cameraHolder.forward, velDir) / 180f;
        float fallSpeed = Mathf.Clamp01(-rb.linearVelocity.y / (dangerFallSpeed * 2f));
        return Mathf.Clamp01((camVelAngle * 0.6f) + (fallSpeed * 0.4f));
    }

    void OnCollisionEnter(Collision col)
    {
        if (isStunned) return;
        if (hasLanded) return;

        if (col.relativeVelocity.y > 2f)
        {
            hasLanded = true;

            // Use saved score from while falling, not current velocity
            float score = savedLandingScore;

            if (recoveryUsed)
            {
                // Player successfully recovered
                recoveryUsed = false;
                recoveryAvailable = false;
                cameraRoll = 0f;
                targetCameraRoll = 0f;
                verticalLook = Mathf.Clamp(verticalLook, -80f, 80f);
                hud.ShowLanding("CLEAN LANDING");
                return;
            }

            if (score > veryBadLandingThreshold)
            {
                // Always stun on very bad landing — recovery was your chance to avoid it
                recoveryAvailable = false;
                isStunned = true;
                stunTimer = 2f;
                rb.linearVelocity = Vector3.zero;
                hud.ShowStun();
                hud.HideRecovery();
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
                cameraRoll = 0f;
                targetCameraRoll = 0f;
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
