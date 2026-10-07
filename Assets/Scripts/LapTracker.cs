using System.Collections.Generic;
using UnityEngine;

public class LapTracker : MonoBehaviour
{
    [SerializeField] private int totalLaps = 3;

    public int CurrentLap { get; private set; } = 0;
    public int TotalLaps => totalLaps;
    public float RaceTime => raceTimer;
    public float CurrentLapTime => currentLapTime;
    public IReadOnlyList<float> LapTimes => lapTimes;

    // События для UI
    public event System.Action<int, int> OnLapChanged;   // (текущий, всего)
    public event System.Action<int, float> OnLapFinished; // (номер круга, время)

    private readonly List<float> lapTimes = new();  // времена последних кругов
    private bool passedMid = false;
    private float raceTimer = 0f;
    private float currentLapTime = 0f;
    private bool raceFinished = false;

    void Start()
    {
        OnLapChanged?.Invoke(CurrentLap, totalLaps);
    }

    void Update()
    {
        if (raceFinished) return;
        raceTimer += Time.deltaTime;
        currentLapTime += Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("MidTrigger"))
        {
            passedMid = true;
            return;
        }

        if (other.CompareTag("FinishLine"))
        {
            if (!passedMid) return;

            CurrentLap++;
            passedMid = false;

            // Сохраняем время круга, держим максимум 3
            lapTimes.Add(currentLapTime);
            if (lapTimes.Count > 3) lapTimes.RemoveAt(0);
            OnLapFinished?.Invoke(CurrentLap, currentLapTime);
            currentLapTime = 0f;

            OnLapChanged?.Invoke(CurrentLap, totalLaps);
            Debug.Log($"[LapTracker] КРУГ {CurrentLap}/{totalLaps} — {lapTimes[^1]:F2} сек");

            if (CurrentLap >= totalLaps)
            {
                raceFinished = true;
                Debug.Log($"[LapTracker] ФИНИШ! Общее время: {raceTimer:F2} сек");
            }
        }
    }
}