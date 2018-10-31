using System;
using System.Threading.Tasks;
using Stateless.Reflection;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class ExitActionBehavior
        {
            protected ExitActionBehavior(InvocationInfo actionDescription)
            {
                Description = actionDescription ?? throw new ArgumentNullException(nameof(actionDescription));
            }

            internal InvocationInfo Description { get; }
            public abstract void Execute(Transition transition);
            public abstract Task ExecuteAsync(Transition transition);

            public class Sync : ExitActionBehavior
            {
                private readonly Action<Transition> action;

                public Sync(Action<Transition> action, InvocationInfo actionDescription) : base(actionDescription)
                {
                    this.action = action;
                }

                public override void Execute(Transition transition)
                {
                    this.action(transition);
                }

                public override Task ExecuteAsync(Transition transition)
                {
                    Execute(transition);
                    return TaskResult.done;
                }
            }

            public class Async : ExitActionBehavior
            {
                private readonly Func<Transition, Task> action;

                public Async(Func<Transition, Task> action, InvocationInfo actionDescription) : base(actionDescription)
                {
                    this.action = action;
                }

                public override void Execute(Transition transition)
                {
                    throw new InvalidOperationException(
                        $"Cannot execute asynchronous action specified in OnExit event for '{transition.Source}' state. " +
                        "Use asynchronous version of Fire [FireAsync]");
                }

                public override Task ExecuteAsync(Transition transition)
                {
                    return this.action(transition);
                }
            }
        }
    }
}