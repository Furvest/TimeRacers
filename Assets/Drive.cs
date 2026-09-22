using UnityEngine;
using UnityEngine.InputSystem;
// настройки управления машиной бибиб
public class drive : MonoBehaviour
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
    private Vector2 velocity;

    void Awake()
    {
        controls = new CarControls();
    }

    void OnEnable()  => controls.Car.Enable();
    void OnDisable() => controls.Car.Disable();

    void Update()
    {
        //Ввод
        float move  = controls.Car.Move.ReadValue<float>();   // W=+1, S=-1
        float steer = controls.Car.Steer.ReadValue<float>();  // D=+1, A=-1
        bool  drift = controls.Car.Drift.IsPressed();

        //Поворот (знак минус — чтобы D крутил вправо)
        float turnMul = drift ? driftTurnBoost : 1f;
        transform.Rotate(0f, 0f, -steer * turnSpeed * turnMul * Time.deltaTime);

        //Раскладываем вектор скорости на оси машины
        Vector2 forward = transform.up;
        Vector2 right   = transform.right;
        float forwardVel = Vector2.Dot(velocity, forward);
        float lateralVel = Vector2.Dot(velocity, right);

        // Движение вперед/назад
        if (move > 0f)
        {
            forwardVel += acceleration * Time.deltaTime;
            forwardVel = Mathf.Min(forwardVel, moveSpeed);
        }
        else if (move < 0f)
        {
            forwardVel = Mathf.MoveTowards(forwardVel, 0f, brakeDeceleration * Time.deltaTime);
        }
        else
        {
            float dragNow = drift ? drag * driftDragMultiplier : drag;
            forwardVel = Mathf.MoveTowards(forwardVel, 0f,
                                           dragNow * Time.deltaTime);
        }

        // Дрифт-составляющая (чем меньше сцепление, тем сильнее скольжение)
        float grip = drift ? driftGrip : normalGrip;
        lateralVel *= Mathf.Exp(-grip * Time.deltaTime);

        // Объединяем вектор скорости и перемещаем машину
        velocity = forward * forwardVel + right * lateralVel;
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }
}