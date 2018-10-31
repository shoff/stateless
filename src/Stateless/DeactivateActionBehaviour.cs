using System;
using System.Threading.Tasks;
using Stateless.Reflection;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class DeactivateActionBehaviour
        {
            private readonly TState state;

            protected DeactivateActionBehaviour(TState state, InvocationInfo actionDescription)
            {
                this.state = state;
                Description = actionDescription ?? throw new ArgumentNullException(nameof(actionDescription));
            }

            internal InvocationInfo Description { get; }

            public abstract void Execute();
            public abstract Task ExecuteAsync();

            public class Sync : DeactivateActionBehaviour
            {
                private readonly Action action;

                public Sync(TState state, Action action, InvocationInfo actionDescription)
                    : base(state, actionDescription)
                {
                    this.action = action;
                }

                public override void Execute()
                {
                    this.action();
                }

                public override Task ExecuteAsync()
                {
                    Execute();
                    return TaskResult.done;
                }
            }

            public class Async : DeactivateActionBehaviour
            {
                private readonly Func<Task> action;

                public Async(TState state, Func<Task> action, InvocationInfo actionDescription)
                    : base(state, actionDescription)
                {
                    this.action = action;
                }

                public override void Execute()
                {
                    throw new InvalidOperationException(
                        $"Cannot execute asynchronous action specified in OnDeactivateAsync for '{this.state}' state. " +
                        "Use asynchronous version of Deactivate [DeactivateAsync]");
                }

                public override Task ExecuteAsync()
                {
                    return this.action();
                }
            }
        }
    }
}