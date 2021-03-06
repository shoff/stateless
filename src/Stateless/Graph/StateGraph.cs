﻿using System;
using System.Collections.Generic;
using System.Linq;
using Stateless.Reflection;

namespace Stateless.Graph
{
    /// <summary>
    ///     This class is used to generate a symbolic representation of the
    ///     graph structure, in preparation for feeding it to a diagram
    ///     generator
    /// </summary>
    internal class StateGraph
    {
        public StateGraph(StateMachineInfo machineInfo)
        {
            // Start with top-level superstates
            AddSuperstates(machineInfo);

            // Now add any states that aren't part of a tree
            AddSingleStates(machineInfo);

            // Now grab transitions
            AddTransitions(machineInfo);

            // Handle "OnEntryFrom"
            ProcessOnEntryFrom(machineInfo);
        }

        /// <summary>
        ///     List of all states in the graph, indexed by the string representation of the underlying State object.
        /// </summary>
        public Dictionary<string, State> States { get; } = new Dictionary<string, State>();

        /// <summary>
        ///     List of all transitions in the graph
        /// </summary>
        public List<Transition> Transitions { get; } = new List<Transition>();

        /// <summary>
        ///     List of all decision nodes in the graph.  A decision node is generated each time there
        ///     is a PermitDynamic() transition.
        /// </summary>
        public List<Decision> Decisions { get; } = new List<Decision>();

        /// <summary>
        ///     Convert the graph into a string representation, using the specified style.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public string ToGraph(GraphStyle style)
        {
            var dirgraphText = style.GetPrefix().Replace("\n", Environment.NewLine);

            // Start with the clusters
            foreach (var state in this.States.Values.Where(x => x is SuperState))
                dirgraphText += style.FormatOneCluster((SuperState) state).Replace("\n", Environment.NewLine);

            // Next process all non-cluster states
            foreach (var state in this.States.Values)
            {
                if (state is SuperState || state is Decision || state.SuperState != null)
                    continue;
                dirgraphText += style.FormatOneState(state).Replace("\n", Environment.NewLine);
            }

            // Finally, add decision nodes
            foreach (var dec in this.Decisions)
                dirgraphText += style.FormatOneDecisionNode(dec.NodeName, dec.Method.Description)
                    .Replace("\n", Environment.NewLine);

            // now build behaviours
            var transits = style.FormatAllTransitions(this.Transitions);
            foreach (var transit in transits)
                dirgraphText += Environment.NewLine + transit;

            dirgraphText += Environment.NewLine + "}";

            return dirgraphText;
        }

        /// <summary>
        ///     Process all entry actions that have a "FromTrigger" (meaning they are
        ///     only executed when the state is entered because the specified trigger
        ///     was fired).
        /// </summary>
        /// <param name="machineInfo"></param>
        private void ProcessOnEntryFrom(StateMachineInfo machineInfo)
        {
            foreach (var stateInfo in machineInfo.States)
            {
                var state = this.States[stateInfo.UnderlyingState.ToString()];
                foreach (var entryAction in stateInfo.EntryActions)
                    if (entryAction.FromTrigger != null)
                        foreach (var transit in state.Arriving)
                            if (transit.ExecuteEntryExitActions
                                && transit.Trigger.UnderlyingTrigger.ToString() == entryAction.FromTrigger)
                                transit.destinationEntryActions.Add(entryAction);
            }
        }


        /// <summary>
        ///     Add all transitions to the graph
        /// </summary>
        /// <param name="machineInfo"></param>
        private void AddTransitions(StateMachineInfo machineInfo)
        {
            foreach (var stateInfo in machineInfo.States)
            {
                var fromState = this.States[stateInfo.UnderlyingState.ToString()];
                foreach (var fix in stateInfo.FixedTransitions)
                {
                    var toState = this.States[fix.DestinationState.UnderlyingState.ToString()];
                    if (fromState == toState)
                    {
                        var stay = new StayTransition(fromState, fix.Trigger, fix.guardConditionsMethodDescriptions,
                            true);
                        this.Transitions.Add(stay);
                        fromState.Leaving.Add(stay);
                        fromState.Arriving.Add(stay);
                    }
                    else
                    {
                        var trans = new FixedTransition(fromState, toState, fix.Trigger,
                            fix.guardConditionsMethodDescriptions);
                        this.Transitions.Add(trans);
                        fromState.Leaving.Add(trans);
                        toState.Arriving.Add(trans);
                    }
                }

                foreach (var dyno in stateInfo.DynamicTransitions)
                {
                    var decide = new Decision(dyno.DestinationStateSelectorDescription, this.Decisions.Count + 1);
                    this.Decisions.Add(decide);
                    var trans = new FixedTransition(fromState, decide, dyno.Trigger,
                        dyno.guardConditionsMethodDescriptions);
                    this.Transitions.Add(trans);
                    fromState.Leaving.Add(trans);
                    decide.Arriving.Add(trans);
                    if (dyno.PossibleDestinationStates != null)
                        foreach (var dynamicStateInfo in dyno.PossibleDestinationStates)
                        {
                            State toState = null;
                            this.States.TryGetValue(dynamicStateInfo.DestinationState, out toState);
                            if (toState != null)
                            {
                                var dtrans = new DynamicTransition(decide, toState, dyno.Trigger,
                                    dynamicStateInfo.Criterion);
                                this.Transitions.Add(dtrans);
                                decide.Leaving.Add(dtrans);
                                toState.Arriving.Add(dtrans);
                            }
                        }
                }

                foreach (var igno in stateInfo.IgnoredTriggers)
                {
                    var stay = new StayTransition(fromState, igno.Trigger, igno.guardConditionsMethodDescriptions,
                        false);
                    this.Transitions.Add(stay);
                    fromState.Leaving.Add(stay);
                    fromState.Arriving.Add(stay);
                }
            }
        }


        /// <summary>
        ///     Add states to the graph that are neither superstates, nor substates of a superstate.
        /// </summary>
        /// <param name="machineInfo"></param>
        private void AddSingleStates(StateMachineInfo machineInfo)
        {
            foreach (var stateInfo in machineInfo.States)
                if (!this.States.ContainsKey(stateInfo.UnderlyingState.ToString()))
                    this.States[stateInfo.UnderlyingState.ToString()] = new State(stateInfo);
        }

        /// <summary>
        ///     Add superstates to the graph (states that have substates)
        /// </summary>
        /// <param name="machineInfo"></param>
        private void AddSuperstates(StateMachineInfo machineInfo)
        {
            foreach (var stateInfo in machineInfo.States.Where(sc => sc.Substates?.Count() > 0 && sc.Superstate == null)
            )
            {
                var state = new SuperState(stateInfo);
                this.States[stateInfo.UnderlyingState.ToString()] = state;
                AddSubstates(state, stateInfo.Substates);
            }
        }

        private void AddSubstates(SuperState superState, IEnumerable<StateInfo> substates)
        {
            foreach (var subState in substates)
                if (this.States.ContainsKey(subState.UnderlyingState.ToString()))
                {
                    // This shouldn't happen
                }
                else if (subState.Substates.Any())
                {
                    var sub = new SuperState(subState);
                    this.States[subState.UnderlyingState.ToString()] = sub;
                    superState.SubStates.Add(sub);
                    sub.SuperState = superState;
                    AddSubstates(sub, subState.Substates);
                }
                else
                {
                    var sub = new State(subState);
                    this.States[subState.UnderlyingState.ToString()] = sub;
                    superState.SubStates.Add(sub);
                    sub.SuperState = superState;
                }
        }
    }
}