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
    [SerializeField] private float rwdOversteer = 60f; // угол, на который задняя ось может "сносить" машину при повороте

    [Header("Сцепление")]
    [SerializeField] private float normalGrip = 12f;
    [SerializeField] private float driftGrip = 1.5f;
    [SerializeField] private float driftDragMultiplier = 0.3f;

    [Header("Буст")]
    [SerializeField] private float boostDuration = 2f;
    [SerializeField] private float boostSpeedMultiplier = 1.2f;        // +20% к скорости
    [SerializeField] private float boostAccelerationMultiplier = 1.5f; // +50% к ускорению
    [SerializeField] private float boostImpulse = 2f;                  // мгновенный рывок

    [Header("State UI")]
    [SerializeField] private bool showStateOnScreen = true;
    [SerializeField] private Vector2 stateLabelPosition = new Vector2(20, 20);
    [SerializeField] private int stateLabelFontSize = 28;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;
    
    public bool InputEnabled { get; set; } = true;
    public CarState State { get; private set; } = CarState.Stopped;
    public event System.Action<CarState, CarState> OnStateChanged;

    private CarControls controls;
    private Rigidbody2D rb;
    private Vector2 velocity;
    private float boostTimer;
    private float speedLimit, grip, turnBoost;
    private bool isTouchingWall = false;
    private Vector2 wallNormal;

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
        //блокировка ввода до старта
        if (!InputEnabled)
        {
            velocity = Vector2.zero; 
            SetState(CarState.Stopped);
            return;
        }

        // чтение ввода
        float move  = controls.Car.Move.ReadValue<float>();
        float steer = controls.Car.Steer.ReadValue<float>();
        bool  drift = controls.Car.Drift.IsPressed();

        if (boostTimer > 0f) boostTimer -= Time.fixedDeltaTime;

        SetState(DetermineState(move, drift));
        ApplyParams();

        //оси машины
        Vector2 forward = rb.transform.up;
        Vector2 right   = rb.transform.right;
        float fwd = Vector2.Dot(velocity, forward);
        float lat = Vector2.Dot(velocity, right);

        //поворот только если машина движется да
        float speedFactor = Mathf.Clamp01(Mathf.Abs(fwd) / minTurnSpeed);
        float turnAmount  = -steer * turnSpeed * turnBoost * speedFactor * Time.fixedDeltaTime;

        //эффекты заднего привода
        if (move > 0f && Mathf.Abs(steer) > 0.1f && Mathf.Abs(fwd) > 1f)
        {
            float speedRatio = Mathf.Clamp01(Mathf.Abs(fwd) / speedLimit);
            turnAmount += -steer * rwdOversteer * speedRatio * Time.fixedDeltaTime;
        }
        rb.MoveRotation(rb.rotation + turnAmount);

        //продольная составляющая (разгон/тормоз/задний ход)
        float currentAccel = (State == CarState.Boosting)
            ? acceleration * boostAccelerationMultiplier
            : acceleration;

        if (move > 0f)
        {
            fwd += currentAccel * Time.fixedDeltaTime;
            fwd = Mathf.Min(fwd, speedLimit);
        }
        else if (move < 0f)
        {
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

        //поперечная составляющая (сцепление шин)
        lat *= Mathf.Exp(-grip * Time.fixedDeltaTime);

        //собираем вектор скорости
        velocity = forward * fwd + right * lat;

        //касания со стенами
        if (isTouchingWall)
        {
            float normalVelocity = Vector2.Dot(velocity, wallNormal);
            
            if (normalVelocity < 0f)
            {
                velocity -= normalVelocity * wallNormal;
                velocity *= 0.98f; 
            }
        }
        //движение
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
                speedLimit = moveSpeed * boostSpeedMultiplier;
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
    {
        boostTimer = (duration > 0f) ? duration : boostDuration;
        velocity += (Vector2)rb.transform.up * boostImpulse;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        //при первом ударе теряем 40% скорости (глухой удар)
        velocity *= 0.6f;
    }
    void OnCollisionStay2D(Collision2D col)
    {
        if (col.contactCount == 0) return;
        
        //запоминаем нормаль стены и ставим флаг, что мы её касаемся
        isTouchingWall = true;
        wallNormal = col.contacts[0].normal;
    }
    void OnCollisionExit2D(Collision2D col)
    {
        isTouchingWall = false;
    }

    void OnGUI()
    {
        if (!showStateOnScreen) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = stateLabelFontSize,
            fontStyle = FontStyle.Bold,
        };
        GUI.Label(
            new Rect(stateLabelPosition.x, stateLabelPosition.y, 300, 50),
            State.ToString(),
            style);
    }
}