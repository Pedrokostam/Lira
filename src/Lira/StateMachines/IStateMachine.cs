using System.Threading.Tasks;

namespace Lira.StateMachines;
public interface IStateMachine<TState> where TState : IState
{
    /// <summary>
    ///     Runs the next step of the machine based on the given state.
    ///     If the state indicates the work is already finished it shall be returned without changes.
    /// </summary>
    /// <param name="state"></param>
    /// <returns>
    ///     New instance of <typeparamref name="TState"/>, which should be passed to the next invocation of this method.
    ///     <para/>
    ///     If the input state was for an already finished workflow, return the input state without any additional work.
    /// </returns>
    Task<TState> Process(TState state);
}