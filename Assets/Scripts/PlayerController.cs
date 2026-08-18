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
    public float fireRate = 0.2f;
    public float bulletSpeed = 12f;

    private Rigidbody2D rb;
    private Camera cam;
    private Vector2 moveInput;
    private bool isFiring;
    private float nextFireTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
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
        Vector2 move = moveInput.normalized * moveSpeed;
        Vector2 newPos = rb.position + move * Time.fixedDeltaTime;

        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        newPos.x = Mathf.Clamp(newPos.x, min.x + screenPadding.x, max.x - screenPadding.x);
        newPos.y = Mathf.Clamp(newPos.y, min.y + screenPadding.y, max.y - screenPadding.y);

        rb.MovePosition(newPos);
    }

    // Auto-called by PlayerInput ("Send Messages" behavior) whenever
    // the "Move" action changes value (WASD, arrows, or gamepad stick).
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Auto-called by PlayerInput whenever the "Fire" action is pressed/released.
    public void OnFire(InputValue value)
    {
        isFiring = value.isPressed;

        // Fire immediately on press, then Update() handles repeat-fire while held
        if (isFiring && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Fire();
        }
    }

    void Fire()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        b.Init(Vector2.up, bulletSpeed, "Player");
    }

}
