using System.Collections.Generic;
using Stateless.Reflection;

namespace Stateless.Graph
{
    /// <summary>
    ///     Used to keep track of a state that has substates
    /// </summary>
    public class SuperState : State
    {
        internal SuperState(StateInfo stateInfo)
            : base(stateInfo)
        {
        }

        /// <summary>
        ///     List of states that are a substate of this state
        /// </summary>
        public List<State> SubStates { get; } = new List<State>();
    }
}