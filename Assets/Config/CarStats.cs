using UnityEngine;

/// <summary>
/// ScriptableObject, содержащий все настройки баланса и физики автомобиля.
/// Позволяет менять параметры без перекомпиляции кода.
/// </summary>
[CreateAssetMenu(fileName = "NewCarStats", menuName = "TimeRacers/Car Stats")]
public class CarStats : ScriptableObject
{
    [Header("Скорость и Ускорение")]
    [Tooltip("Максимальная скорость движения вперед")]
    [Range(1f, 50f)] public float moveSpeed = 5f;
    
    [Tooltip("Максимальная скорость заднего хода")]
    [Range(1f, 10f)] public float reverseSpeed = 2.5f;
    
    [Tooltip("Как быстро машина набирает скорость вперед")]
    [Range(1f, 50f)] public float acceleration = 12f;
    
    [Tooltip("Как быстро машина набирает скорость назад")]
    [Range(1f, 50f)] public float reverseAcceleration = 8f;
    
    [Tooltip("Сила торможения при нажатии S на ходу")]
    [Range(1f, 50f)] public float brakeDeceleration = 20f;
    
    [Tooltip("Естественное замедление при отпускании газа")]
    [Range(0f, 10f)] public float drag = 2f;

    [Header("Управление и Повороты")]
    [Tooltip("Скорость поворота машины (градусы в секунду)")]
    [Range(50f, 200f)] public float turnSpeed = 120f;
    
    [Tooltip("Множитель поворота во время дрифта")]
    [Range(1f, 3f)] public float driftTurnBoost = 1.5f;
    
    [Tooltip("Минимальная скорость, при которой начинает работать поворот")]
    [Range(0.1f, 5f)] public float minTurnSpeed = 0.3f;
    
    [Tooltip("Эффект заднего привода (занос задней оси при повороте)")]
    [Range(0f, 100f)] public float rwdOversteer = 60f;

    [Header("Сцепление с дорогой (Физика)")]
    [Tooltip("Чем выше, тем быстрее машина останавливает боковое скольжение (едет по рельсам)")]
    [Range(1f, 30f)] public float normalGrip = 12f;
    
    [Tooltip("Сцепление во время дрифта (зажатый Shift). Чем ниже, тем сильнее занос")]
    [Range(0.5f, 10f)] public float driftGrip = 1.5f;
    
    [Tooltip("Множитель сопротивления во время дрифта")]
    [Range(0f, 1f)] public float driftDragMultiplier = 0.3f;

    [Header("Буст (Ускорение)")]
    [Tooltip("Длительность буста по умолчанию")]
    [Range(0.5f, 5f)] public float boostDuration = 2f;
    
    [Tooltip("Во сколько раз увеличивается макс. скорость при бусте")]
    [Range(1f, 3f)] public float boostSpeedMultiplier = 1.2f;
    
    [Tooltip("Во сколько раз увеличивается ускорение при бусте")]
    [Range(1f, 5f)] public float boostAccelerationMultiplier = 1.5f;
    
    [Tooltip("Мгновенный рывок (импульс) в начале буста")]
    [Range(0f, 10f)] public float boostImpulse = 2f;

    [Header("Отображение состояния (UI)")]
    public bool showStateOnScreen = true;
    public Vector2 stateLabelPosition = new Vector2(20, 20);
    [Range(10, 72)] public int stateLabelFontSize = 28;

    [Header("Логи")]
    [Tooltip("Выводить ли в консоль смену состояний машины")]
    public bool logStateChanges = true;
}