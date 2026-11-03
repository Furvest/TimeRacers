using UnityEngine;

public class StickRotation : MonoBehaviour
{
    public float speed = 100f;

    void Update()
    {
        // Вращаем палку вокруг оси Z
        transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}