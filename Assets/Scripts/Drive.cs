using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Drive : MonoBehaviour
{
    [Header("Конфигурация")]
    [Tooltip("Ссылка на ассет с настройками машины")]
    [SerializeField] private CarStats stats; 
    public CarStats Stats => stats;
    
    public bool InputEnabled { get; set; } = true;

    [Header("Отладочная информация (только чтение)")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float currentLateralSpeed;
    [SerializeField] private bool isDrifting;

    // Публичные свойства для чтения из окна редактора
    public float CurrentSpeed => currentSpeed;
    public float CurrentLateralSpeed => currentLateralSpeed;
    public bool IsDrifting => isDrifting;

    private CarControls controls;
    private Rigidbody2D rb;
    private Vector2 velocity;
    
    // Переменные состояния
    private float boostTimer;
    private float speedLimit, grip, turnBoost;
    private bool isTouchingWall = false;
    private Vector2 wallNormal;
    
    // Наш FSM
    private CarStateMachine fsm;

    void Awake()
    {
        if (stats == null)
        {
            Debug.LogError($"[{gameObject.name}] Не назначен CarStats! Машина не будет двигаться.");
            return;
        }

        controls = new CarControls();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Инициализируем текущие параметры из конфига
        speedLimit = stats.moveSpeed; 
        grip = stats.normalGrip; 
        turnBoost = 1f;
        
        // Инициализируем FSM
        fsm = new CarStateMachine();
        fsm.OnStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(CarState oldState, CarState newState)
    {
        if (stats.logStateChanges) 
            Debug.Log($"[Drive] {oldState} → {newState}");
    }

    void OnEnable()  { if (controls != null) controls.Car.Enable(); }
    void OnDisable() { if (controls != null) controls.Car.Disable(); }

    void FixedUpdate()
    {
        if (stats == null) return;

        //блокировка ввода до старта
        if (!InputEnabled)
        {
            velocity = Vector2.zero; 
            fsm.ChangeState(CarState.Stopped); // ИСПРАВЛЕНО ТУТ
            return;
        }

        // чтение ввода
        float move  = controls.Car.Move.ReadValue<float>();
        float steer = controls.Car.Steer.ReadValue<float>();
        bool  drift = controls.Car.Drift.IsPressed();

        if (boostTimer > 0f) boostTimer -= Time.fixedDeltaTime;

        // Работа с FSM
        CarState desiredState = DetermineDesiredState(move, drift);
        fsm.ChangeState(desiredState);
        ApplyParams();

        //оси машины
        Vector2 forward = rb.transform.up;
        Vector2 right   = rb.transform.right;
        float fwd = Vector2.Dot(velocity, forward);
        float lat = Vector2.Dot(velocity, right);

        //поворот только если машина движется
        float speedFactor = Mathf.Clamp01(Mathf.Abs(fwd) / stats.minTurnSpeed);
        float turnAmount  = -steer * stats.turnSpeed * turnBoost * speedFactor * Time.fixedDeltaTime;

        //эффекты заднего привода
        if (move > 0f && Mathf.Abs(steer) > 0.1f && Mathf.Abs(fwd) > 1f)
        {
            float speedRatio = Mathf.Clamp01(Mathf.Abs(fwd) / speedLimit);
            turnAmount += -steer * stats.rwdOversteer * speedRatio * Time.fixedDeltaTime;
        }
        rb.MoveRotation(rb.rotation + turnAmount);

        //продольная составляющая (разгон/тормоз/задний ход)
        float currentAccel = (fsm.CurrentState == CarState.Boosting)
            ? stats.acceleration * stats.boostAccelerationMultiplier
            : stats.acceleration;

        if (move > 0f)
        {
            fwd += currentAccel * Time.fixedDeltaTime;
            fwd = Mathf.Min(fwd, speedLimit);
        }
        else if (move < 0f)
        {
            if (fwd > 0.05f)
                fwd = Mathf.MoveTowards(fwd, 0f, stats.brakeDeceleration * Time.fixedDeltaTime);
            else
            {
                fwd -= stats.reverseAcceleration * Time.fixedDeltaTime;
                fwd = Mathf.Max(fwd, -stats.reverseSpeed);
            }
        }
        else
        {
            float d = (fsm.CurrentState == CarState.Drifting) ? stats.drag * stats.driftDragMultiplier : stats.drag;
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

        // --- данные для отладки ---
        currentSpeed = fwd;
        currentLateralSpeed = lat;
        isDrifting = drift;
    }

    private CarState DetermineDesiredState(float move, bool drift)
    {
        if (fsm.CurrentState == CarState.Paradox) return CarState.Paradox;
        if (boostTimer > 0f) return CarState.Boosting;
        if (drift && velocity.sqrMagnitude > 0.5f) return CarState.Drifting;
        if (velocity.sqrMagnitude < 0.01f && Mathf.Abs(move) < 0.1f) return CarState.Stopped;
        if (move < 0f && Vector2.Dot(velocity, rb.transform.up) < -0.1f) return CarState.Reverse;
        return CarState.Normal;
    }

    void ApplyParams()
    {
        switch (fsm.CurrentState)
        {
            case CarState.Boosting:
                speedLimit = stats.moveSpeed * stats.boostSpeedMultiplier;
                grip = stats.normalGrip; turnBoost = 1f; break;
            case CarState.Drifting:
                speedLimit = stats.moveSpeed;
                grip = stats.driftGrip; turnBoost = stats.driftTurnBoost; break;
            case CarState.Reverse:
                speedLimit = stats.reverseSpeed;
                grip = stats.normalGrip; turnBoost = 1f; break;
            default:
                speedLimit = stats.moveSpeed;
                grip = stats.normalGrip; turnBoost = 1f; break;
        }
    }

    public void ApplyBoost(float duration = -1f)
    {
        if (stats == null) return;
        boostTimer = (duration > 0f) ? duration : stats.boostDuration;
        velocity += (Vector2)rb.transform.up * stats.boostImpulse;
        
        // Мгновенно переключаем FSM, чтобы не ждать следующего кадра
        fsm.ChangeState(CarState.Boosting);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        velocity *= 0.6f;
    }
    void OnCollisionStay2D(Collision2D col)
    {
        if (col.contactCount == 0) return;
        isTouchingWall = true;
        wallNormal = col.contacts[0].normal;
    }
    void OnCollisionExit2D(Collision2D col)
    {
        isTouchingWall = false;
    }

    void OnGUI()
    {
        if (stats == null || !stats.showStateOnScreen) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = stats.stateLabelFontSize,
            fontStyle = FontStyle.Bold,
        };
        GUI.Label(
            new Rect(stats.stateLabelPosition.x, stats.stateLabelPosition.y, 300, 50),
            fsm.CurrentState.ToString(),
            style);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Вектор скорости (синий)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, (Vector3)velocity);

        // Вектор направления "носа" (зелёный)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 2f);

        // Визуализация бокового скольжения (красный)
        Gizmos.color = Color.red;
        Vector2 lateral = transform.right * currentLateralSpeed;
        Gizmos.DrawRay(transform.position, (Vector3)lateral);
    }
}