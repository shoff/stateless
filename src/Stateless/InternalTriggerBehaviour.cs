using System;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class InternalTriggerBehaviour : TriggerBehaviour
        {
            protected InternalTriggerBehaviour(TTrigger trigger, TransitionGuard guard) : base(trigger, guard)
            {
            }

            public abstract void Execute(Transition transition, object[] args);
            public abstract Task ExecuteAsync(Transition transition, object[] args);

            public override bool ResultsInTransitionFrom(TState source, object[] args, out TState destination)
            {
                destination = source;
                return false;
            }


            public class Sync : InternalTriggerBehaviour
            {
                public Sync(TTrigger trigger, Func<object[], bool> guard, Action<Transition, object[]> internalAction,
                    string guardDescription = null) : base(trigger, new TransitionGuard(guard, guardDescription))
                {
                    InternalAction = internalAction;
                }

                public Action<Transition, object[]> InternalAction { get; }

                public override void Execute(Transition transition, object[] args)
                {
                    InternalAction(transition, args);
                }

                public override Task ExecuteAsync(Transition transition, object[] args)
                {
                    Execute(transition, args);
                    return TaskResult.done;
                }
            }

            public class Async : InternalTriggerBehaviour
            {
                private readonly Func<Transition, object[], Task> internalAction;

                public Async(TTrigger trigger, Func<bool> guard, Func<Transition, object[], Task> internalAction,
                    string guardDescription = null) : base(trigger, new TransitionGuard(guard, guardDescription))
                {
                    this.internalAction = internalAction;
                }

                public override void Execute(Transition transition, object[] args)
                {
                    throw new InvalidOperationException(
                        $"Cannot execute asynchronous action specified in OnEntry event for '{transition.Destination}' state. " +
                        "Use asynchronous version of Fire [FireAsync]");
                }

                public override Task ExecuteAsync(Transition transition, object[] args)
                {
                    return this.internalAction(transition, args);
                }
            }
        }
    }
}