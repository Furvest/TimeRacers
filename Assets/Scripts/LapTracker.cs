using System.Collections.Generic;
using UnityEngine;

public class LapTracker : MonoBehaviour
{
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private string[] checkpointOrder = { "Mid1", "Mid2", "Mid3" };
    [SerializeField] private string finishTag = "FinishLine";

    public int CurrentLap { get; private set; } = 0;
    public int TotalLaps => totalLaps;
    public float RaceTime => raceTimer;
    public float CurrentLapTime => currentLapTime;
    public IReadOnlyList<float> LapTimes => lapTimes;

    public event System.Action<int, int> OnLapChanged;
    public event System.Action<int, float> OnLapFinished;

    private readonly List<float> lapTimes = new();
    private int nextCheckpoint = 0;
    private float raceTimer = 0f;
    private float currentLapTime = 0f;
    private bool raceFinished = false;

    void Start() => OnLapChanged?.Invoke(CurrentLap, totalLaps);

    void Update()
    {
        if (raceFinished) return;
        raceTimer += Time.deltaTime;
        currentLapTime += Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (raceFinished) return;

        // Следующий ожидаемый чекпоинт
        if (nextCheckpoint < checkpointOrder.Length &&
            other.CompareTag(checkpointOrder[nextCheckpoint]))
        {
            Debug.Log($"[LapTracker] Чекпоинт {checkpointOrder[nextCheckpoint]} пройден.");
            nextCheckpoint++;
            return;
        }

        // Финиш
        if (other.CompareTag(finishTag))
        {
            if (nextCheckpoint < checkpointOrder.Length)
            {
                Debug.Log("[LapTracker] Финиш не засчитан — не все чекпоинты пройдены.");
                return;
            }

            CurrentLap++;
            lapTimes.Add(currentLapTime);
            if (lapTimes.Count > 3) lapTimes.RemoveAt(0);
            OnLapFinished?.Invoke(CurrentLap, currentLapTime);
            currentLapTime = 0f;
            nextCheckpoint = 0;

            OnLapChanged?.Invoke(CurrentLap, totalLaps);
            Debug.Log($"[LapTracker] КРУГ {CurrentLap}/{totalLaps} — {lapTimes[^1]:F2} сек");

            if (CurrentLap >= totalLaps)
            {
                raceFinished = true;
                Debug.Log($"[LapTracker] 🏁 ФИНИШ! Общее время: {raceTimer:F2} сек");
            }
        }
    }
}