using System;
using UnityEngine;

public enum ZombieState
{
    Idle,
    Pursue,
    Attack,
    Stunned,
    Dead
}

[DisallowMultipleComponent]
public class ZombieStateMachine : MonoBehaviour
{
    public ZombieState CurrentState { get; private set; } = ZombieState.Idle;

    public event Action<ZombieState> OnStateChanged;

    public void SetState(ZombieState newState)
    {
        if (newState == CurrentState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);
    }
}
