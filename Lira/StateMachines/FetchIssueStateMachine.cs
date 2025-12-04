using Lira.Objects;

namespace Lira.StateMachines;

/// <summary>State machine specialized for fetching full Issue objects.</summary>
public class FetchIssueStateMachine(LiraClient client) : FetchIssueMachineImpl<Issue>(client) { }
