using Lira.Objects;

namespace Lira.StateMachines;

/// <summary>State machine specialized for fetching lightweight IssueLite objects.</summary>
public class FetchIssueLiteStateMachine(LiraClient client) : FetchIssueMachineImpl<IssueLite>(client) { }
