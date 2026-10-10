using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RaceStart : MonoBehaviour
{
    [SerializeField] private Drive car;
    [SerializeField] private TMP_Text countdownLabel;

    [SerializeField] private float countStepTime = 1f;   // время на каждую цифру
    [SerializeField] private float perfectWindow = 0.3f; // окно PERFECT после GO
    [SerializeField] private float goodWindow = 0.8f;    // окно GOOD после GO
    [SerializeField] private float perfectBoost = 2.5f;
    [SerializeField] private float goodBoost = 1f;

    private bool canPress = false;   // открыто ли окно для нажатия
    private float goTime;

    void Start()
    {
        car.InputEnabled = false;
        countdownLabel.text = "";
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 3; i >= 1; i--)
        {
            countdownLabel.text = i.ToString();
            yield return new WaitForSeconds(countStepTime);
        }

        countdownLabel.text = "GO!";
        goTime = Time.time;
        car.InputEnabled = true;
        canPress = true;

        yield return new WaitForSeconds(goodWindow);
        canPress = false;

        yield return new WaitForSeconds(0.6f);
        countdownLabel.text = "";
    }

    void Update()
    {
        if (!canPress) return;
        if (!Keyboard.current.wKey.wasPressedThisFrame) return;

        canPress = false;
        float delta = Time.time - goTime;

        if (delta <= perfectWindow)
        {
            car.ApplyBoost(perfectBoost);
            countdownLabel.text = "PERFECT!";
        }
        else
        {
            car.ApplyBoost(goodBoost);
            countdownLabel.text = "GOOD!";
        }
    }
}