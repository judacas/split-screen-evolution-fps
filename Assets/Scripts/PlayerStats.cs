using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Movement")]
    public float moveSpeed = 5f;

    // New movement advanced parameters (moved from UpdatedCharacterController)
    [Header("Movement - Advanced")]
    public float dashMultiplier = 2.5f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float rotationSpeed = 1.0f;
    public float speedChangeRate = 10.0f;
    public float jumpHeight = 1.2f;
    public float gravity = -15f;
    public float jumpTimeout = 0.1f;
    public float fallTimeout = 0.15f;
    public float groundedOffset = -0.14f;
    public float groundedRadius = 0.5f;
    public LayerMask groundLayers;
    public float ceilingOffset = 0.5f;
    public float ceilingRadius = 0.5f;
    public LayerMask ceilingLayers;
    public float topClamp = 90.0f;
    public float bottomClamp = -90.0f;

    [Header("Shooting")]
    public float bulletDamage = 10f;
    public float bulletSpeed = 10f;
    public float bulletLifeSpan = 1f;
    public float fireRate = 0.2f; // Time between shots

    [Tooltip("In degrees; used to spread multiple bullets.")]
    public float bulletSpread = 5f;

    [Tooltip("Number of bullets fired per shot.")]
    public int bulletCount = 1;

    [Header("Ammo")]
    public int maxAmmo = 30;
    public int currentAmmo = 30;

    public void Start()
    {
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
    }
}
