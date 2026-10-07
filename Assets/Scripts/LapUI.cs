using System.Text;
using UnityEngine;
using TMPro;

public class LapUI : MonoBehaviour
{
    [SerializeField] private LapTracker tracker;

    [Header("Тексты")]
    [SerializeField] private TMP_Text totalTimeLabel;   // большое, сверху
    [SerializeField] private TMP_Text currentLapLabel;  // поменьше, под ним
    [SerializeField] private TMP_Text lapCounterLabel;  // «Круг 1 / 3»
    [SerializeField] private TMP_Text lapListLabel;     // список времён

    private readonly StringBuilder sb = new();

    void OnEnable()
    {
        if (tracker == null) return;
        tracker.OnLapChanged  += HandleLapChanged;
        tracker.OnLapFinished += HandleLapFinished;
    }

    void OnDisable()
    {
        if (tracker == null) return;
        tracker.OnLapChanged  -= HandleLapChanged;
        tracker.OnLapFinished -= HandleLapFinished;
    }

    void Start()
    {
        HandleLapChanged(tracker.CurrentLap, tracker.TotalLaps);
        RefreshLapList();
    }

    void Update()
    {
        // Постоянно обновляем общее время и текущий круг
        totalTimeLabel.text  = $"Время: {FormatTime(tracker.RaceTime)}";
        currentLapLabel.text = $"Круг:  {FormatTime(tracker.CurrentLapTime)}";
    }

    void HandleLapChanged(int current, int total)
    {
        lapCounterLabel.text = $"Круг {current} / {total}";
    }

    void HandleLapFinished(int lapNumber, float time)
    {
        RefreshLapList();
    }

    void RefreshLapList()
    {
        sb.Clear();
        var times = tracker.LapTimes;

        for (int i = 0; i < times.Count; i++)
        {
            int lapNum = tracker.CurrentLap - times.Count + i + 1;
            sb.AppendLine($"Круг {lapNum}: {FormatTime(times[i])}");
        }

        lapListLabel.text = sb.ToString().TrimEnd();
    }

    string FormatTime(float t)
    {
        int min = Mathf.FloorToInt(t / 60f);
        float sec = t - min * 60f;
        return min > 0 ? $"{min}:{sec:00.00}" : $"{sec:00.00}";
    }
}