using System;
using System.Collections.Generic;
using System.Linq;
using Stateless.Reflection;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal partial class StateRepresentation
        {
            private readonly ICollection<StateRepresentation> substates = new List<StateRepresentation>();

            private bool active;

            public StateRepresentation(TState state)
            {
                UnderlyingState = state;
            }

            internal IDictionary<TTrigger, ICollection<TriggerBehaviour>> TriggerBehaviours { get; } =
                new Dictionary<TTrigger, ICollection<TriggerBehaviour>>();

            internal ICollection<EntryActionBehavior> EntryActions { get; } = new List<EntryActionBehavior>();
            internal ICollection<ExitActionBehavior> ExitActions { get; } = new List<ExitActionBehavior>();

            internal ICollection<ActivateActionBehaviour> ActivateActions { get; } =
                new List<ActivateActionBehaviour>();

            internal ICollection<DeactivateActionBehaviour> DeactivateActions { get; } =
                new List<DeactivateActionBehaviour>();

            public TState InitialTransitionTarget { get; private set; }

            public StateRepresentation Superstate { get; set; }

            public TState UnderlyingState { get; }

            public IEnumerable<TTrigger> PermittedTriggers => GetPermittedTriggers();

            public bool HasInitialTransition { get; private set; }

            internal ICollection<StateRepresentation> GetSubstates()
            {
                return this.substates;
            }

            public bool CanHandle(TTrigger trigger, params object[] args)
            {
                return TryFindHandler(trigger, args, out var unused);
            }

            public bool TryFindHandler(TTrigger trigger, object[] args, out TriggerBehaviourResult handler)
            {
                return TryFindLocalHandler(trigger, args, out handler) ||
                       Superstate != null && Superstate.TryFindHandler(trigger, args, out handler);
            }

            private bool TryFindLocalHandler(TTrigger trigger, object[] args, out TriggerBehaviourResult handlerResult)
            {
                // Get list of candidate trigger handlers
                if (!TriggerBehaviours.TryGetValue(trigger, out var possible))
                {
                    handlerResult = null;
                    return false;
                }

                // Remove those that have unmet guard conditions
                // Guard functions are executed here
                var actual = possible
                    .Select(h => new TriggerBehaviourResult(h, h.UnmetGuardConditions(args)))
                    .Where(g => g.UnmetGuardConditions.Count == 0)
                    .ToArray();

                // Find a handler for the trigger
                handlerResult = TryFindLocalHandlerResult(trigger, actual, r => !r.UnmetGuardConditions.Any())
                                ?? TryFindLocalHandlerResult(trigger, actual, r => r.UnmetGuardConditions.Any());

                if (handlerResult == null) return false;

                return !handlerResult.UnmetGuardConditions.Any();
            }

            private TriggerBehaviourResult TryFindLocalHandlerResult(TTrigger trigger,
                IEnumerable<TriggerBehaviourResult> results, Func<TriggerBehaviourResult, bool> filter)
            {
                var actual = results
                    .Where(filter);

                if (actual.Count() > 1)
                    throw new InvalidOperationException(
                        string.Format(StateRepresentationResources.MultipleTransitionsPermitted,
                            trigger, UnderlyingState));

                return actual
                    .FirstOrDefault();
            }

            public void AddActivateAction(Action action, InvocationInfo activateActionDescription)
            {
                ActivateActions.Add(
                    new ActivateActionBehaviour.Sync(UnderlyingState, action, activateActionDescription));
            }

            public void AddDeactivateAction(Action action, InvocationInfo deactivateActionDescription)
            {
                DeactivateActions.Add(
                    new DeactivateActionBehaviour.Sync(UnderlyingState, action, deactivateActionDescription));
            }

            public void AddEntryAction(TTrigger trigger, Action<Transition, object[]> action,
                InvocationInfo entryActionDescription)
            {
                EntryActions.Add(new EntryActionBehavior.SyncFrom<TTrigger>(trigger, action, entryActionDescription));
            }

            public void AddEntryAction(Action<Transition, object[]> action, InvocationInfo entryActionDescription)
            {
                EntryActions.Add(new EntryActionBehavior.Sync(action, entryActionDescription));
            }

            public void AddExitAction(Action<Transition> action, InvocationInfo exitActionDescription)
            {
                ExitActions.Add(new ExitActionBehavior.Sync(action, exitActionDescription));
            }

            public void Activate()
            {
                if (Superstate != null)
                    Superstate.Activate();

                if (active)
                    return;

                ExecuteActivationActions();
                active = true;
            }

            public void Deactivate()
            {
                if (!active)
                    return;

                ExecuteDeactivationActions();
                active = false;

                if (Superstate != null)
                    Superstate.Deactivate();
            }

            private void ExecuteActivationActions()
            {
                foreach (var action in ActivateActions)
                    action.Execute();
            }

            private void ExecuteDeactivationActions()
            {
                foreach (var action in DeactivateActions)
                    action.Execute();
            }

            public void Enter(Transition transition, params object[] entryArgs)
            {
                if (transition.IsReentry)
                {
                    ExecuteEntryActions(transition, entryArgs);
                    ExecuteActivationActions();
                }
                else if (!Includes(transition.Source))
                {
                    if (Superstate != null)
                        Superstate.Enter(transition, entryArgs);

                    ExecuteEntryActions(transition, entryArgs);
                    ExecuteActivationActions();
                }
            }

            public Transition Exit(Transition transition)
            {
                if (transition.IsReentry)
                {
                    ExecuteDeactivationActions();
                    ExecuteExitActions(transition);
                }
                else if (!Includes(transition.Destination))
                {
                    ExecuteDeactivationActions();
                    ExecuteExitActions(transition);

                    // Must check if there is a superstate, and if we are leaving that superstate
                    if (Superstate != null)
                    {
                        // Check if destination is within the state list
                        if (IsIncludedIn(transition.Destination))
                        {
                            // Destination state is within the list, exit first superstate only if it is NOT the the first
                            if (!Superstate.UnderlyingState.Equals(transition.Destination))
                                return Superstate.Exit(transition);
                        }
                        else
                        {
                            // Exit the superstate as well
                            return Superstate.Exit(transition);
                        }
                    }
                }

                return transition;
            }

            private void ExecuteEntryActions(Transition transition, object[] entryArgs)
            {
                foreach (var action in EntryActions)
                    action.Execute(transition, entryArgs);
            }

            private void ExecuteExitActions(Transition transition)
            {
                foreach (var action in ExitActions)
                    action.Execute(transition);
            }

            internal void InternalAction(Transition transition, object[] args)
            {
                InternalTriggerBehaviour.Sync internalTransition = null;

                // Look for actions in superstate(s) recursivly until we hit the topmost superstate, or we actually find some trigger handlers.
                var aStateRep = this;
                while (aStateRep != null)
                {
                    if (aStateRep.TryFindLocalHandler(transition.Trigger, args, out var result))
                    {
                        // Trigger handler found in this state
                        if (result.Handler is InternalTriggerBehaviour.Async)
                            throw new InvalidOperationException(
                                "Running Async internal actions in synchronous mode is not allowed");

                        internalTransition = result.Handler as InternalTriggerBehaviour.Sync;
                        break;
                    }

                    // Try to look for trigger handlers in superstate (if it exists)
                    aStateRep = aStateRep.Superstate;
                }

                // Execute internal transition event handler
                if (internalTransition == null)
                    throw new ArgumentNullException(
                        "The configuration is incorrect, no action assigned to this internal transition.");
                internalTransition.InternalAction(transition, args);
            }

            public void AddTriggerBehaviour(TriggerBehaviour triggerBehaviour)
            {
                if (!TriggerBehaviours.TryGetValue(triggerBehaviour.Trigger, out var allowed))
                {
                    allowed = new List<TriggerBehaviour>();
                    TriggerBehaviours.Add(triggerBehaviour.Trigger, allowed);
                }

                allowed.Add(triggerBehaviour);
            }

            public void AddSubstate(StateRepresentation substate)
            {
                this.substates.Add(substate);
            }

            public bool Includes(TState state)
            {
                return UnderlyingState.Equals(state) || this.substates.Any(s => s.Includes(state));
            }

            public bool IsIncludedIn(TState state)
            {
                return
                    UnderlyingState.Equals(state) ||
                    Superstate != null && Superstate.IsIncludedIn(state);
            }

            public IEnumerable<TTrigger> GetPermittedTriggers(params object[] args)
            {
                var result = TriggerBehaviours
                    .Where(t => t.Value.Any(a => !a.UnmetGuardConditions(args).Any()))
                    .Select(t => t.Key);

                if (Superstate != null)
                    result = result.Union(Superstate.GetPermittedTriggers(args));

                return result.ToArray();
            }

            internal void SetInitialTransition(TState state)
            {
                InitialTransitionTarget = state;
                HasInitialTransition = true;
            }
        }
    }
}