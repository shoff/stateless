using System.Collections.Generic;
using Stateless.Reflection;

namespace Stateless.Graph
{
    /// <summary>
    ///     Used to keep track of a state that has substates
    /// </summary>
    public class State
    {
        internal State(StateInfo stateInfo)
        {
            this.NodeName = stateInfo.UnderlyingState.ToString();
            this.StateName = stateInfo.UnderlyingState.ToString();

            // Only include entry actions that aren't specific to a trigger
            foreach (var entryAction in stateInfo.EntryActions)
                if (entryAction.FromTrigger == null)
                    this.EntryActions.Add(entryAction.Method.Description);

            foreach (var exitAction in stateInfo.ExitActions)
                this.ExitActions.Add(exitAction.Description);
        }

        internal State(string nodeName)
        {
            this.NodeName = nodeName;
            this.StateName = null;
        }

        /// <summary>
        ///     The superstate of this state (null if none)
        /// </summary>
        public SuperState SuperState { get; set; } = null;

        /// <summary>
        ///     List of all transitions that leave this state (never null)
        /// </summary>
        public List<Transition> Leaving { get; } = new List<Transition>();

        /// <summary>
        ///     List of all transitions that enter this state (never null)
        /// </summary>
        public List<Transition> Arriving { get; } = new List<Transition>();

        /// <summary>
        ///     Unique name of this object
        /// </summary>
        public string NodeName { get; }

        /// <summary>
        ///     Name of the state represented by this object
        /// </summary>
        public string StateName { get; }

        /// <summary>
        ///     Actions that are executed when you enter this state from any trigger
        /// </summary>
        public List<string> EntryActions { get; } = new List<string>();

        /// <summary>
        ///     Actions that are executed when you exit this state
        /// </summary>
        public List<string> ExitActions { get; } = new List<string>();
    }
}