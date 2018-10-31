using System;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class InternalActionBehaviour
        {
            public abstract void Execute(Transition transition, object[] args);
            public abstract Task ExecuteAsync(Transition transition, object[] args);

            public class Sync : InternalActionBehaviour
            {
                private readonly Action<Transition, object[]> action;

                public Sync(Action<Transition, object[]> action)
                {
                    this.action = action;
                }

                public override void Execute(Transition transition, object[] args)
                {
                    this.action(transition, args);
                }

                public override Task ExecuteAsync(Transition transition, object[] args)
                {
                    Execute(transition, args);
                    return TaskResult.done;
                }
            }

            public class Async : InternalActionBehaviour
            {
                private readonly Func<Transition, object[], Task> action;

                public Async(Func<Transition, object[], Task> action)
                {
                    this.action = action;
                }

                public override void Execute(Transition transition, object[] args)
                {
                    throw new InvalidOperationException(
                        $"Cannot execute asynchronous action specified in OnEntry event for '{transition.Destination}' state. " +
                        "Use asynchronous version of Fire [FireAsync]");
                }

                public override Task ExecuteAsync(Transition transition, object[] args)
                {
                    return this.action(transition, args);
                }
            }
        }
    }
}