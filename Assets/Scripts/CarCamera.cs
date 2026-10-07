using UnityEngine;

public class CarCamera : MonoBehaviour
{
    [Header("цель")]
    public Transform target;                 // CameraTarget внутри машины

    [Header("Смещение")]
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Z всегда -10

    [Header("Плавность")]
    [Tooltip("3-4 — плавно и кинематографично, 8-10 — резко")]
    public float smooth = 5f;

    [Header("Забегание вперёд")]
    public bool lookAhead = true;
    public float lookAheadDistance = 1.5f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 lastTargetPos;

    void Start()
    {
        if (target != null)
            lastTargetPos = target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Направление движения машины (по разнице позиций)
        Vector3 moveDir = (target.position - lastTargetPos).normalized;
        lastTargetPos = target.position;

        Vector3 desiredPos = target.position + offset;

        // Забегаем чуть вперёд по ходу движения
        if (lookAhead && moveDir.sqrMagnitude > 0.0001f)
            desiredPos += moveDir * lookAheadDistance;

        // Плавно двигаем камеру
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref velocity,
            1f / smooth
        );

        // Камера всегда смотрит ровно (без поворота) — для top-down это правильно
        transform.rotation = Quaternion.identity;
    }
}