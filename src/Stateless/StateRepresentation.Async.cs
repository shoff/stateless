#if TASKS

using System;
using System.Threading.Tasks;
using Stateless.Reflection;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal partial class StateRepresentation
        {
            public void AddActivateAction(Func<Task> action, InvocationInfo activateActionDescription)
            {
                ActivateActions.Add(new ActivateActionBehaviour.Async(UnderlyingState, action, activateActionDescription));
            }

            public void AddDeactivateAction(Func<Task> action, InvocationInfo deactivateActionDescription)
            {
                DeactivateActions.Add(new DeactivateActionBehaviour.Async(UnderlyingState, action, deactivateActionDescription));
            }

            public void AddEntryAction(TTrigger trigger, Func<Transition, object[], Task> action,
                InvocationInfo entryActionDescription)
            {
                if (action == null) throw new ArgumentNullException(nameof(action));

                EntryActions.Add(
                    new EntryActionBehavior.Async((t, args) =>
                        {
                            if (t.Trigger.Equals(trigger))
                                return action(t, args);

                            return TaskResult.done;
                        },
                        entryActionDescription));
            }

            public void AddEntryAction(Func<Transition, object[], Task> action, InvocationInfo entryActionDescription)
            {
                EntryActions.Add(
                    new EntryActionBehavior.Async(
                        action,
                        entryActionDescription));
            }

            public void AddExitAction(Func<Transition, Task> action, InvocationInfo exitActionDescription)
            {
                ExitActions.Add(new ExitActionBehavior.Async(action, exitActionDescription));
            }

            public async Task ActivateAsync()
            {
                if (Superstate != null)
                    await Superstate.ActivateAsync().ConfigureAwait(false);

                if (active)
                    return;

                await ExecuteActivationActionsAsync().ConfigureAwait(false);
                active = true;
            }

            public async Task DeactivateAsync()
            {
                if (!active)
                    return;

                await ExecuteDeactivationActionsAsync().ConfigureAwait(false);
                active = false;

                if (Superstate != null)
                    await Superstate.DeactivateAsync().ConfigureAwait(false);
            }

            private async Task ExecuteActivationActionsAsync()
            {
                foreach (var action in ActivateActions)
                    await action.ExecuteAsync().ConfigureAwait(false);
            }

            private async Task ExecuteDeactivationActionsAsync()
            {
                foreach (var action in DeactivateActions)
                    await action.ExecuteAsync().ConfigureAwait(false);
            }

            public async Task EnterAsync(Transition transition, params object[] entryArgs)
            {
                if (transition.IsReentry)
                {
                    await ExecuteEntryActionsAsync(transition, entryArgs).ConfigureAwait(false);
                    await ExecuteActivationActionsAsync().ConfigureAwait(false);
                }
                else if (!Includes(transition.Source))
                {
                    if (Superstate != null)
                        await Superstate.EnterAsync(transition, entryArgs).ConfigureAwait(false);

                    await ExecuteEntryActionsAsync(transition, entryArgs).ConfigureAwait(false);
                    await ExecuteActivationActionsAsync().ConfigureAwait(false);
                }
            }

            public async Task<Transition> ExitAsync(Transition transition)
            {
                if (transition.IsReentry)
                {
                    await ExecuteDeactivationActionsAsync().ConfigureAwait(false);
                    await ExecuteExitActionsAsync(transition).ConfigureAwait(false);
                }
                else if (!Includes(transition.Destination))
                {
                    await ExecuteDeactivationActionsAsync().ConfigureAwait(false);
                    await ExecuteExitActionsAsync(transition).ConfigureAwait(false);

                    if (Superstate != null)
                    {
                        transition = new Transition(Superstate.UnderlyingState, transition.Destination,
                            transition.Trigger);
                        return await Superstate.ExitAsync(transition).ConfigureAwait(false);
                    }
                }

                return transition;
            }

            private async Task ExecuteEntryActionsAsync(Transition transition, object[] entryArgs)
            {
                foreach (var action in EntryActions)
                    await action.ExecuteAsync(transition, entryArgs).ConfigureAwait(false);
            }

            private async Task ExecuteExitActionsAsync(Transition transition)
            {
                foreach (var action in ExitActions)
                    await action.ExecuteAsync(transition).ConfigureAwait(false);
            }

            private async Task ExecuteInternalActionsAsync(Transition transition, object[] args)
            {
                InternalTriggerBehaviour internalTransition = null;

                // Look for actions in superstate(s) recursivly until we hit the topmost superstate, or we actually find some trigger handlers.
                var aStateRep = this;
                while (aStateRep != null)
                {
                    if (aStateRep.TryFindLocalHandler(transition.Trigger, args, out var result))
                    {
                        // Trigger handler(s) found in this state
                        internalTransition = result.Handler as InternalTriggerBehaviour;
                        break;
                    }

                    // Try to look for trigger handlers in superstate (if it exists)
                    aStateRep = aStateRep.Superstate;
                }

                // Execute internal transition event handler
                if (internalTransition == null)
                    throw new ArgumentNullException(
                        "The configuration is incorrect, no action assigned to this internal transition.");
                await internalTransition.ExecuteAsync(transition, args).ConfigureAwait(false);
            }

            internal Task InternalActionAsync(Transition transition, object[] args)
            {
                return ExecuteInternalActionsAsync(transition, args);
            }
        }
    }
}

#endif