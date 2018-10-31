using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        private class OnTransitionedEvent
        {
            private readonly List<Func<Transition, Task>> onTransitionedAsync = new List<Func<Transition, Task>>();
            private event Action<Transition> OnTransitioned;

            public void Invoke(Transition transition)
            {
                if (this.onTransitionedAsync.Count != 0)
                    throw new InvalidOperationException(
                        "Cannot execute asynchronous action specified as OnTransitioned callback. " +
                        "Use asynchronous version of Fire [FireAsync]");

                this.OnTransitioned?.Invoke(transition);
            }

#if TASKS
            public async Task InvokeAsync(Transition transition)
            {
                this.OnTransitioned?.Invoke(transition);

                foreach (var callback in this.onTransitionedAsync)
                    await callback(transition).ConfigureAwait(false);
            }
#endif

            public void Register(Action<Transition> action)
            {
                this.OnTransitioned += action;
            }

            public void Register(Func<Transition, Task> action)
            {
                this.onTransitionedAsync.Add(action);
            }
        }
    }
}