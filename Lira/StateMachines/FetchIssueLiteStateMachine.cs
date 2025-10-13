using Lira.Objects;

namespace Lira.StateMachines;

public class FetchIssueLiteStateMachine(LiraClient client) : FetchIssueMachineImpl<IssueLite>(client) { }

