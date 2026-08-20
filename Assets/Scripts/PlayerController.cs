using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public Vector2 screenPadding = new Vector2(0.5f, 0.5f);

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.35f;
    public float bulletSpeed = 12f;

    [Header("Recoil")]
    public float recoilDamping = 8f;

    private Rigidbody2D rb;
    private Camera cam;
    private Vector2 moveInput;
    private bool isFiring;
    private float nextFireTime;
    private Vector2 recoilVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        PlayerRoleComponent roleComponent = GetComponent<PlayerRoleComponent>();
        if (roleComponent != null)
        {
            moveSpeed *= roleComponent.Stats.moveSpeedMultiplier;
            fireRate *= roleComponent.Stats.fireRateMultiplier;
        }
    }

    void Update()
    {
        // Handles held-down fire (auto-fire while button is held)
        if (isFiring && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Fire();
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        recoilVelocity = Vector2.Lerp(recoilVelocity, Vector2.zero, recoilDamping * Time.fixedDeltaTime);

        Vector2 move = moveInput.normalized * moveSpeed + recoilVelocity;
        Vector2 newPos = rb.position + move * Time.fixedDeltaTime;

        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        newPos.x = Mathf.Clamp(newPos.x, min.x + screenPadding.x, max.x - screenPadding.x);
        newPos.y = Mathf.Clamp(newPos.y, min.y + screenPadding.y, max.y - screenPadding.y);

        rb.MovePosition(newPos);
    }

    // Auto-called by PlayerInput ("Send Messages" behavior) whenever
    // the "Move" action changes value (WASD, arrows, or gamepad stick).
// Auto-called by PlayerInput ("Send Messages" behavior) whenever
    // the "Move" action changes value (WASD, arrows, or gamepad stick).
    public void OnMove(InputValue value)
    {
        SetMoveDirection(value.Get<Vector2>());
    }

    // Non-input entry point so AIController can drive movement directly.
    public void SetMoveDirection(Vector2 direction)
    {
        moveInput = direction;
    }

    // Auto-called by PlayerInput whenever the "Fire" action is pressed/released.
// Auto-called by PlayerInput whenever the "Fire" action is pressed/released.
    public void OnFire(InputValue value)
    {
        SetFiring(value.isPressed);
    }

    // Non-input entry point so AIController can drive firing directly.
    public void SetFiring(bool firing)
    {
        isFiring = firing;

        // Fire immediately on press, then Update() handles repeat-fire while held
        if (isFiring && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Fire();
        }
    }

    public void AddRecoil(Vector2 impulse)
    {
        recoilVelocity += impulse;
    }

    void Fire()
    {
        SpawnBullet(1f, 1);
    }

    public void FireBigShot(float widthMultiplier, int damageAmount)
    {
        SpawnBullet(widthMultiplier, damageAmount);
    }

void SpawnBullet(float widthMultiplier, int damageAmount)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        if (widthMultiplier != 1f)
        {
            Vector3 scale = bulletObj.transform.localScale;
            scale.x *= widthMultiplier;
            bulletObj.transform.localScale = scale;
        }
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = damageAmount;
        b.Init(Vector2.up, bulletSpeed, "Player", gameObject);
    }

}
