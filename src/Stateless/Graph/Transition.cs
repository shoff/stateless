using System.Collections.Generic;
using Stateless.Reflection;

namespace Stateless.Graph
{
    /// <summary>
    ///     Used to keep track of transitions between states
    /// </summary>
    public class Transition
    {
        /// <summary>
        ///     List of actions to be performed by the destination state (the one being entered)
        /// </summary>
        public List<ActionInfo> destinationEntryActions = new List<ActionInfo>();

        /// <summary>
        ///     Base class of transitions
        /// </summary>
        /// <param name="sourceState"></param>
        /// <param name="trigger"></param>
        public Transition(State sourceState, TriggerInfo trigger)
        {
            this.SourceState = sourceState;
            this.Trigger = trigger;
        }

        /// <summary>
        ///     The trigger that causes this transition
        /// </summary>
        public TriggerInfo Trigger { get; }

        /// <summary>
        ///     Should the entry and exit actions be executed when this transition takes place
        /// </summary>
        public bool ExecuteEntryExitActions { get; protected set; } = true;

        /// <summary>
        ///     The state where this transition starts
        /// </summary>
        public State SourceState { get; }
    }

    internal class FixedTransition : Transition
    {
        public FixedTransition(State sourceState, State destinationState, TriggerInfo trigger,
            IEnumerable<InvocationInfo> guards)
            : base(sourceState, trigger)
        {
            this.DestinationState = destinationState;
            this.Guards = guards;
        }

        /// <summary>
        ///     The state where this transition finishes
        /// </summary>
        public State DestinationState { get; }

        /// <summary>
        ///     Guard functions for this transition (null if none)
        /// </summary>
        public IEnumerable<InvocationInfo> Guards { get; }
    }

    internal class DynamicTransition : Transition
    {
        public DynamicTransition(State sourceState, State destinationState, TriggerInfo trigger, string criterion)
            : base(sourceState, trigger)
        {
            this.DestinationState = destinationState;
            this.Criterion = criterion;
        }

        /// <summary>
        ///     The state where this transition finishes
        /// </summary>
        public State DestinationState { get; }

        /// <summary>
        ///     When is this transition followed
        /// </summary>
        public string Criterion { get; }
    }

    internal class StayTransition : Transition
    {
        public StayTransition(State sourceState, TriggerInfo trigger, IEnumerable<InvocationInfo> guards,
            bool executeEntryExitActions)
            : base(sourceState, trigger)
        {
            this.ExecuteEntryExitActions = executeEntryExitActions;
            this.Guards = guards;
        }

        public IEnumerable<InvocationInfo> Guards { get; }
    }
}