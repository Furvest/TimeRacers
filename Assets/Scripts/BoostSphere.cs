using UnityEngine;

public class BoostSphere : MonoBehaviour
{
    [SerializeField] private float boostDuration = 2f;

    void OnTriggerEnter2D(Collider2D other)
    {
        var car = other.GetComponent<Drive>();
        if (car != null)
        {
            car.ApplyBoost(boostDuration);
            Debug.Log("Буст!");
        }
    }
}