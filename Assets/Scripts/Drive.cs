using UnityEngine;
using UnityEngine.InputSystem;

public enum CarState { Stopped, Normal, Drifting, Boosting, Reverse, Paradox }

[RequireComponent(typeof(Rigidbody2D))]
public class Drive : MonoBehaviour
{
    [Header("Скорость")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float reverseSpeed = 2.5f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float reverseAcceleration = 8f;
    [SerializeField] private float brakeDeceleration = 20f;
    [SerializeField] private float drag = 2f;

    [Header("Поворот")]
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float driftTurnBoost = 1.5f;
    [SerializeField] private float minTurnSpeed = 0.3f;   // скорость, с которой начинается поворот

    [Header("Сцепление")]
    [SerializeField] private float normalGrip = 12f;
    [SerializeField] private float driftGrip = 1.5f;
    [SerializeField] private float driftDragMultiplier = 0.3f;

    [Header("Буст")]
    [SerializeField] private float boostMultiplier = 1.8f;
    [SerializeField] private float boostDuration = 2f;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    public CarState State { get; private set; } = CarState.Stopped;
    public event System.Action<CarState, CarState> OnStateChanged;

    private CarControls controls;
    private Rigidbody2D rb;
    private Vector2 velocity;
    private float boostTimer;
    private float speedLimit, grip, turnBoost;

    void Awake()
    {
        controls = new CarControls();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        speedLimit = moveSpeed; grip = normalGrip; turnBoost = 1f;
    }

    void OnEnable()  => controls.Car.Enable();
    void OnDisable() => controls.Car.Disable();

    void FixedUpdate()
    {
        float move  = controls.Car.Move.ReadValue<float>();
        float steer = controls.Car.Steer.ReadValue<float>();
        bool  drift = controls.Car.Drift.IsPressed();

        if (boostTimer > 0f) boostTimer -= Time.fixedDeltaTime;

        SetState(DetermineState(move, drift));
        ApplyParams();

        Vector2 forward = rb.transform.up;
        Vector2 right   = rb.transform.right;
        float fwd = Vector2.Dot(velocity, forward);
        float lat = Vector2.Dot(velocity, right);

        // === Поворот: только если машина движется ===
        float speedFactor = Mathf.Clamp01(Mathf.Abs(fwd) / minTurnSpeed);
        float turnAmount  = -steer * turnSpeed * turnBoost * speedFactor * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation + turnAmount);

        // === Продольная составляющая ===
        if (move > 0f)
        {
            fwd += acceleration * Time.fixedDeltaTime;
            fwd = Mathf.Min(fwd, speedLimit);
        }
        else if (move < 0f)
        {
            // S: сначала тормоз до нуля, потом задний ход
            if (fwd > 0.05f)
                fwd = Mathf.MoveTowards(fwd, 0f, brakeDeceleration * Time.fixedDeltaTime);
            else
            {
                fwd -= reverseAcceleration * Time.fixedDeltaTime;
                fwd = Mathf.Max(fwd, -reverseSpeed);
            }
        }
        else
        {
            float d = (State == CarState.Drifting) ? drag * driftDragMultiplier : drag;
            fwd = Mathf.MoveTowards(fwd, 0f, d * Time.fixedDeltaTime);
        }

        // Поперечная
        lat *= Mathf.Exp(-grip * Time.fixedDeltaTime);

        velocity = forward * fwd + right * lat;
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    CarState DetermineState(float move, bool drift)
    {
        if (State == CarState.Paradox) return CarState.Paradox;
        if (boostTimer > 0f) return CarState.Boosting;
        if (drift && velocity.sqrMagnitude > 0.5f) return CarState.Drifting;
        if (velocity.sqrMagnitude < 0.01f && Mathf.Abs(move) < 0.1f) return CarState.Stopped;
        if (move < 0f && Vector2.Dot(velocity, rb.transform.up) < -0.1f) return CarState.Reverse;
        return CarState.Normal;
    }

    void SetState(CarState next)
    {
        if (next == State) return;
        CarState prev = State;
        State = next;
        OnStateChanged?.Invoke(prev, next);
        if (logStateChanges) Debug.Log($"[drive] {prev} → {next}");
    }

    void ApplyParams()
    {
        switch (State)
        {
            case CarState.Boosting:
                speedLimit = moveSpeed * boostMultiplier;
                grip = normalGrip; turnBoost = 1f; break;
            case CarState.Drifting:
                speedLimit = moveSpeed;
                grip = driftGrip; turnBoost = driftTurnBoost; break;
            case CarState.Reverse:
                speedLimit = reverseSpeed;
                grip = normalGrip; turnBoost = 1f; break;
            default:
                speedLimit = moveSpeed;
                grip = normalGrip; turnBoost = 1f; break;
        }
    }

    public void ApplyBoost(float duration = -1f)
        => boostTimer = (duration > 0f) ? duration : boostDuration;

    void OnCollisionEnter2D(Collision2D col) => velocity = Vector2.zero;
}