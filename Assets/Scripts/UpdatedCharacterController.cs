using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public class UpdatedCharacterController : MonoBehaviour
{
    private bool isDashing = false;
    private bool canDash = true;
    private bool Grounded; // set by GroundedCheck

    // Cinemachine
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout delta
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif

    private CharacterController _controller;
    private InputHandler _input;

    [Header("References")]
    [Tooltip("Where the camera will be.")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("The gun object to be used for firing bullets.")]
    public GameObject gun;

    private GameObject _mainCamera;

    [Header("Firing Settings")]
    [Tooltip("Prefab of the bullet to be instantiated when firing.")]
    public GameObject bulletPrefab;

    [Tooltip("Transform representing the spawn point of the bullets.")]
    public Transform bulletSpawnPoint;

    private const float _threshold = 0.01f;

    // PlayerStats reference
    private PlayerStats _playerStats;

    // New firing events
    public event System.Action OnShootEvent;
    public event System.Action OnEmptyAmmoEvent;

    private float _lastFireTime = 0f;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<InputHandler>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
        Debug.LogError(
            "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it"
        );
#endif

        // Retrieve PlayerStats (now holding movement parameters)
        _playerStats = GetComponent<PlayerStats>();

        // Initialize timeouts from PlayerStats now
        _jumpTimeoutDelta = _playerStats.jumpTimeout;
        _fallTimeoutDelta = _playerStats.fallTimeout;
    }

    private void Update()
    {
        if (_input.dash && canDash && !isDashing)
        {
            StartCoroutine(Dash());
            _input.dash = false;
        }
        if (_input.fire)
        {
            Fire();
        }
        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y + _playerStats.groundedOffset,
            transform.position.z
        );
        Grounded = Physics.CheckSphere(
            spherePosition,
            _playerStats.groundedRadius,
            _playerStats.groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold)
        {
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetPitch +=
                _input.look.y * _playerStats.rotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _input.look.x * _playerStats.rotationSpeed * deltaTimeMultiplier;

            _cinemachineTargetPitch = ClampAngle(
                _cinemachineTargetPitch,
                _playerStats.bottomClamp,
                _playerStats.topClamp
            );

            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(
                _cinemachineTargetPitch,
                0.0f,
                0.0f
            );

            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    private void Move()
    {
        float targetSpeed = _playerStats.moveSpeed;
        if (_input.move == Vector2.zero)
            targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(
            _controller.velocity.x,
            0.0f,
            _controller.velocity.z
        ).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        if (
            currentHorizontalSpeed < targetSpeed - speedOffset
            || currentHorizontalSpeed > targetSpeed + speedOffset
        )
        {
            _speed = Mathf.Lerp(
                currentHorizontalSpeed,
                targetSpeed * inputMagnitude,
                Time.deltaTime * _playerStats.speedChangeRate
            );
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
        if (_input.move != Vector2.zero)
        {
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }
        _controller.Move(
            inputDirection.normalized * (_speed * Time.deltaTime)
                + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
        );
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalSpeed = _playerStats.moveSpeed;
        _playerStats.moveSpeed *= _playerStats.dashMultiplier;
        yield return new WaitForSeconds(_playerStats.dashDuration);
        _playerStats.moveSpeed = originalSpeed;
        isDashing = false;
        yield return new WaitForSeconds(
            Mathf.Max(0, _playerStats.dashCooldown - _playerStats.dashDuration)
        );
        canDash = true;
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = _playerStats.fallTimeout;
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(
                    _playerStats.jumpHeight * -2f * _playerStats.gravity
                );
                _input.jump = false;
            }
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = _playerStats.jumpTimeout;
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            _input.jump = false;
        }
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += _playerStats.gravity * Time.deltaTime;
        }
        if (CeilingCheck() && _verticalVelocity > 0.0f)
        {
            _verticalVelocity = 0.0f;
        }
    }

    private bool CeilingCheck()
    {
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y + _playerStats.ceilingOffset,
            transform.position.z
        );
        return Physics.CheckSphere(
            spherePosition,
            _playerStats.ceilingRadius,
            _playerStats.ceilingLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f)
            lfAngle += 360f;
        if (lfAngle > 360f)
            lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void Fire()
    {
        if (Time.time - _lastFireTime < _playerStats.fireRate)
            return;
        if (_playerStats.currentAmmo < _playerStats.bulletCount)
        {
            OnEmptyAmmoEvent?.Invoke();
            return;
        }
        _playerStats.currentAmmo -= _playerStats.bulletCount;
        _lastFireTime = Time.time;
        Vector3 baseDirection = CinemachineCameraTarget.transform.forward;
        for (int i = 0; i < _playerStats.bulletCount; i++)
        {
            Vector3 spreadDirection = GetSpreadDirection(baseDirection, _playerStats.bulletSpread);
            SpawnBullet(spreadDirection);
        }
        OnShootEvent?.Invoke();
    }

    // NEW: Returns a direction vector with random spread applied.
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
            bulletRb.linearVelocity = direction * _playerStats.bulletSpeed;
        }
        Destroy(bullet, _playerStats.bulletLifeSpan); // Destroy bullet after its lifespan
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded)
            Gizmos.color = transparentGreen;
        else
            Gizmos.color = transparentRed;

        Gizmos.DrawSphere(
            new Vector3(
                transform.position.x,
                transform.position.y + _playerStats.groundedOffset,
                transform.position.z
            ),
            _playerStats.groundedRadius
        );

        if (CeilingCheck())
            Gizmos.color = transparentRed;
        else
            Gizmos.color = transparentGreen;
        Gizmos.DrawSphere(
            new Vector3(
                transform.position.x,
                transform.position.y + _playerStats.ceilingOffset,
                transform.position.z
            ),
            _playerStats.ceilingRadius
        );
    }
#endif
}
