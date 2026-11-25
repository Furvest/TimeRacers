using System;
using UnityEngine;

public enum CarState { Stopped, Normal, Drifting, Boosting, Reverse, Paradox }

/// <summary>
/// Конечный автомат (FSM) для управления состояниями автомобиля.
/// </summary>
public class CarStateMachine
{
    public CarState CurrentState { get; private set; }
    
    // Событие для оповещения других систем (например, Drive.cs)
    public event Action<CarState, CarState> OnStateChanged; 

    public CarStateMachine(CarState startState = CarState.Stopped)
    {
        CurrentState = startState;
    }

    /// <summary>
    /// Попытка перехода в новое состояние.
    /// </summary>
    public void ChangeState(CarState newState)
    {
        if (CurrentState == newState) return;

        // Защита от некорректных переходов (например, из Буста сразу в Задний ход)
        if (!IsValidTransition(CurrentState, newState)) return;

        ExitState(CurrentState);  // Вызов логики выхода
        CarState oldState = CurrentState;
        CurrentState = newState;  // Смена состояния
        EnterState(newState);     // Вызов логики входа

        OnStateChanged?.Invoke(oldState, CurrentState);
    }

    /// <summary>
    /// Правила, какие переходы разрешены.
    /// </summary>
    private bool IsValidTransition(CarState from, CarState to)
    {
        // Парадокс — это тупиковое состояние
        if (from == CarState.Paradox) return false;

        // Из любого состояния можно перейти в Stopped
        if (to == CarState.Stopped) return true;

        switch (from)
        {
            case CarState.Stopped:
                // С места только вперед или назад
                return to == CarState.Normal || to == CarState.Reverse;
            case CarState.Reverse:
                // Из заднего хода только в стоп или вперед
                return to == CarState.Normal || to == CarState.Stopped;
            case CarState.Normal:
                // Из обычной езды можно дрифтовать или ускоряться
                return to == CarState.Drifting || to == CarState.Boosting || to == CarState.Reverse;
            case CarState.Drifting:
                // Из дрифта можно выйти в обычную езду или ускориться
                return to == CarState.Normal || to == CarState.Boosting;
            case CarState.Boosting:
                // Из буста можно в дрифт (если игрок зажал ручник) или обычную езду
                return to == CarState.Normal || to == CarState.Drifting;
            default:
                return false;
        }
    }

    /// <summary>
    /// Логика, которая выполняется при входе в состояние.
    /// </summary>
    private void EnterState(CarState state)
    {
        switch (state)
        {
            case CarState.Drifting:
                Debug.Log("[FSM] in drift");
                break;
            case CarState.Boosting:
                Debug.Log("[FSM] in boost");
                break;
            case CarState.Reverse:
                Debug.Log("[FSM] in reverse");
                break;
        }
    }

    /// <summary>
    /// Логика, которая выполняется при выходе из состояния.
    /// </summary>
    private void ExitState(CarState state)
    {
        switch (state)
        {
            case CarState.Drifting:
                Debug.Log("[FSM] out of drift");
                break;
            case CarState.Boosting:
                Debug.Log("[FSM] out of boost");
                break;
        }
    }
}