using UnityEngine;

/// <summary>
/// ScriptableObject для хранения пресета настроек автомобиля.
/// </summary>
[CreateAssetMenu(fileName = "CarDebugConfig", menuName = "TimeRacers/Car Debug Config")]
public class CarDebugConfig : ScriptableObject
{
    public float moveSpeed = 5f;
    public float acceleration = 10f;
    public float turnSpeed = 120f;
    public float normalGrip = 0.95f;
    public float driftGrip = 0.2f;
}