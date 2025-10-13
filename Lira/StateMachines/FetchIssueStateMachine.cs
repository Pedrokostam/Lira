using Lira.Objects;

namespace Lira.StateMachines;

public class FetchIssueStateMachine(LiraClient client) : FetchIssueMachineImpl<Issue>(client) { }

