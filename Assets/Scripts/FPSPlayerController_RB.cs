using System.Collections;
using UnityEngine;

public class FPSPlayerController_RB : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Rigidbody component attached to the player.")]
    public Rigidbody rb;

    [Tooltip("Transform representing the root of the camera.")]
    public Transform cameraRootTr;

    [Tooltip("Transform from which bullets spawn.")]
    public Transform bulletSpawnPoint;

    [Tooltip("Prefab to instantiate for bullets.")]
    public GameObject bulletPrefab;

    [Tooltip("Component holding player stats like move speed, ammo, etc.")]
    public PlayerStats stats; // Reference to PlayerStats component

    [Header("Movement Settings")]
    [Tooltip("Force applied when the player jumps.")]
    public float jumpForce = 5f;

    [Tooltip("Multiplier applied to move speed during dashes.")]
    public float dashMultiplier = 15f; // Renamed from dashSpeed

    [Tooltip("Duration of the dash in seconds.")]
    public float dashDuration = 0.2f;

    [Tooltip("Cooldown time between dashes.")]
    public float dashCooldown = 1f;

    [Header("Look Settings")]
    [Tooltip("Sensitivity for camera look around.")]
    public float lookSensitivity = 2f;

    [Tooltip("Minimum vertical angle for camera rotation.")]
    public float minPitch = -80f;

    [Tooltip("Maximum vertical angle for camera rotation.")]
    public float maxPitch = 80f;

    [Header("Shooting Settings")]
    [Tooltip("Time interval between consecutive fires.")]
    public float fireRate = 0.2f;

    [Header("Ground Check Settings")]
    [Tooltip("Vertical offset for the grounded check sphere.")]
    public float GroundedOffset = -0.14f;

    [Tooltip("Radius of the sphere used for the grounded check.")]
    public float GroundedRadius = 0.5f;

    [Tooltip("Layer mask defining what is considered ground.")]
    public LayerMask GroundLayers;

    // Internal state
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float yaw = 0f;
    private float pitch = 0f;
    public bool isDashing = false;
    public bool canDash = true;
    private float lastFireTime = 0f;
    public bool isGrounded = false;

    // New input state variables
    private bool requestJump;
    private bool fireInput;
    private bool dashInput;

    // Events for reactive upgrades.
    public event System.Action OnShootEvent;
    public event System.Action OnEmptyAmmoEvent;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (stats == null)
            stats = GetComponent<PlayerStats>();
        // if (cameraRootTr == null)
        // cameraRootTr = transform.Find("cameraRoot")?.transform;
        // if (bulletSpawnPoint == null)
        //     cameraRootTr = transform.Find("bulletSpawn")?.transform;

        // Lock the cursor for FPS gameplay.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate() { }

    void Update()
    {
        // CheckGrounded();
        // Use  look input for camera rotation
        yaw += lookInput.x * lookSensitivity * Time.deltaTime;
        pitch += lookInput.y * lookSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraRootTr != null)
        {
            cameraRootTr.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            Debug.Log("Camera rotation updated: " + cameraRootTr.localRotation.eulerAngles);
        }
        UpdateMovement();

        if (requestJump)
        {
            Jump();
            requestJump = false; // reset jump request after processing
        }
        if (fireInput)
        {
            Fire();
        }
        if (dashInput && canDash && !isDashing)
        {
            StartCoroutine(Dash());
            dashInput = false;
        }
    }

    private void UpdateMovement()
    {
        Vector3 desiredVelocity =
            (transform.right * moveInput.x + transform.forward * moveInput.y) * stats.moveSpeed;
        Vector3 currentVelocity = rb.linearVelocity;
        currentVelocity.x = desiredVelocity.x;
        currentVelocity.z = desiredVelocity.z;
        rb.linearVelocity = currentVelocity;
    }

    private void CheckGrounded()
    {
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y - GroundedOffset,
            transform.position.z
        );
        isGrounded = Physics.CheckSphere(
            spherePosition,
            GroundedRadius,
            GroundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    // External methods to set input (called by the input handler).
    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetLookInput(Vector2 input)
    {
        lookInput = input;
    }

    // Updated input setters: now just store state.
    public void RequestJump()
    {
        requestJump = true;
    }

    public void SetFireInput(bool newFireState)
    {
        fireInput = newFireState;
    }

    public void SetDashInput(bool newDashState)
    {
        dashInput = newDashState;
    }

    public void Jump()
    {
        if (isGrounded)
        {
            Vector3 velocity = rb.linearVelocity;
            if (velocity.y < 0)
                velocity.y = 0;
            rb.linearVelocity = velocity;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void Fire()
    {
        if (Time.time - lastFireTime < fireRate)
            return;

        if (stats.currentAmmo < stats.bulletCount)
        {
            OnEmptyAmmoEvent?.Invoke();
            return;
        }
        stats.currentAmmo -= stats.bulletCount;
        lastFireTime = Time.time;

        Vector3 baseDirection = cameraRootTr.forward;

        for (int i = 0; i < stats.bulletCount; i++)
        {
            Vector3 spreadDirection = GetSpreadDirection(baseDirection, stats.bulletSpread);
            SpawnBullet(spreadDirection);
        }

        OnShootEvent?.Invoke();
    }

    // Returns a direction vector with random spread applied.
    private Vector3 GetSpreadDirection(Vector3 baseDir, float spreadAngle)
    {
        float randomYaw = Random.Range(-spreadAngle, spreadAngle);
        float randomPitch = Random.Range(-spreadAngle, spreadAngle);
        Quaternion rotation = Quaternion.Euler(randomPitch, randomYaw, 0);
        Vector3 spreadDir = rotation * baseDir;
        return spreadDir.normalized;
    }

    private void SpawnBullet(Vector3 direction)
    {
        if (bulletPrefab == null || bulletSpawnPoint == null)
            return;

        GameObject bullet = Instantiate(
            bulletPrefab,
            bulletSpawnPoint.position,
            Quaternion.LookRotation(direction)
        );
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = direction * stats.bulletSpeed;
        }
    }

    public void StartDash()
    {
        if (canDash && !isDashing)
            StartCoroutine(Dash());
    }

    // Replace previous Dash coroutine implementation with a speed multiplier dash.
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalSpeed = stats.moveSpeed;
        stats.moveSpeed *= dashMultiplier; // using dashMultiplier as the multiplier
        yield return new WaitForSeconds(dashDuration);
        stats.moveSpeed = originalSpeed;
        isDashing = false;
        yield return new WaitForSeconds(Mathf.Max(0, dashCooldown - dashDuration)); // wait for cooldown before allowing next dash
        canDash = true;
    }
}
