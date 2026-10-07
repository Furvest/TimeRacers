using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Drive : MonoBehaviour
{
    [Header("Скорость")]
    public float moveSpeed = 5f;
    public float acceleration = 12f;
    public float brakeDeceleration = 20f;
    public float drag = 2f;

    [Header("Поворот")]
    public float turnSpeed = 120f;
    public float driftTurnBoost = 1.5f;

    [Header("Сцепление")]
    public float normalGrip = 12f;
    public float driftGrip = 1.5f;
    public float driftDragMultiplier = 0.3f;

    private CarControls controls;
    private Rigidbody2D rb;
    private Vector2 velocity;

    void Awake()
    {
        controls = new CarControls();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;    // запрет физике вращать тело
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // чтобы проверять столкновение со стенами
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void OnEnable()  => controls.Car.Enable();
    void OnDisable() => controls.Car.Disable();

    void FixedUpdate()
    {
        // --- Ввод ---
        float move  = controls.Car.Move.ReadValue<float>();   // W=+1, S=-1
        float steer = controls.Car.Steer.ReadValue<float>();  // D=+1, A=-1
        bool  drift = controls.Car.Drift.IsPressed();

        // --- Поворот через физику ---
        float turnMul = drift ? driftTurnBoost : 1f;
        float deltaAngle = -steer * turnSpeed * turnMul * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation + deltaAngle);

        // --- Оси машины в 2D ---
        Vector2 forward = rb.transform.up;     // «нос» — ось Y спрайта
        Vector2 right   = rb.transform.right;  // «право» — ось X спрайта

        float forwardVel = Vector2.Dot(velocity, forward);
        float lateralVel = Vector2.Dot(velocity, right);

        // --- Продольная составляющая ---
        if (move > 0f)
        {
            forwardVel += acceleration * Time.fixedDeltaTime;
            forwardVel = Mathf.Min(forwardVel, moveSpeed);
        }
        else if (move < 0f)
        {
            forwardVel = Mathf.MoveTowards(forwardVel, 0f,
                                           brakeDeceleration * Time.fixedDeltaTime);
        }
        else
        {
            float dragNow = drift ? drag * driftDragMultiplier : drag;
            forwardVel = Mathf.MoveTowards(forwardVel, 0f,
                                           dragNow * Time.fixedDeltaTime);
        }

        // --- Поперечная (сцепление шин) ---
        float grip = drift ? driftGrip : normalGrip;
        lateralVel *= Mathf.Exp(-grip * Time.fixedDeltaTime);

        // --- Собираем вектор и двигаем через физику ---
        velocity = forward * forwardVel + right * lateralVel;
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    // При ударе о стену — гасим скорость, чтобы не отскакивало и не улетало
    void OnCollisionEnter2D(Collision2D col)
    {
        velocity = Vector2.zero;
    }
}