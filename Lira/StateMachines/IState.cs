using System;

namespace Lira.StateMachines;

/// <summary>
/// The type implementing this interface should have a parameterless constructor that screates a valid state for the first step of its associated machine.
/// </summary>
public interface IState
{
    bool IsFinished { get; }
    bool ShouldContinue { get; }
}

public interface IState<TStep> : IState where TStep : Enum
{
    TStep FinishedStep { get; init; }
    TStep NextStep { get; }
}
