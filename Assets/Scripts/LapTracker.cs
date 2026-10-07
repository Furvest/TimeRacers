using UnityEngine;

public class LapTracker : MonoBehaviour
{
    [SerializeField] private int totalLaps = 3;

    private bool passedMid = false;      // была ли пройдена середина трассы
    private int currentLap = 0;
    private float lapStartTime;

    void Start()
    {
        lapStartTime = Time.time;
        Debug.Log($"[LapTracker] Старт. Кругов: {totalLaps}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Средний триггер: ставим флаг
        if (other.CompareTag("MidTrigger"))
        {
            if (!passedMid)
            {
                passedMid = true;
                Debug.Log("[LapTracker] Середина трассы пройдена.");
            }
            return;
        }

        //Финишная линия: считаем круг
        if (other.CompareTag("FinishLine"))
        {
            if (!passedMid)
            {
                Debug.Log("[LapTracker] Финиш проигнорирован — сначала проедь середину.");
                return;
            }

            currentLap++;
            float lapTime = Time.time - lapStartTime;
            lapStartTime = Time.time;
            passedMid = false;   // сбрасываем флаг — следующий круг надо начинать с середины

            Debug.Log($"[LapTracker] КРУГ {currentLap}/{totalLaps} — {lapTime:F2} сек");

            if (currentLap >= totalLaps)
            {
                Debug.Log("[LapTracker] ФИНИШ!");
                // здесь можно: остановить машину, показать UI, загрузить меню
            }
        }
    }
}