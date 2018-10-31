using System;
using Stateless.Reflection;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal class DynamicTriggerBehaviour : TriggerBehaviour
        {
            private readonly Func<object[], TState> destination;

            public DynamicTriggerBehaviour(TTrigger trigger, Func<object[], TState> destination,
                TransitionGuard transitionGuard, DynamicTransitionInfo info)
                : base(trigger, transitionGuard)
            {
                this.destination = destination ?? throw new ArgumentNullException(nameof(destination));
                TransitionInfo = info ?? throw new ArgumentNullException(nameof(info));
            }

            internal DynamicTransitionInfo TransitionInfo { get; }

            public override bool ResultsInTransitionFrom(TState source, object[] args, out TState destination)
            {
                destination = this.destination(args);
                return true;
            }
        }
    }
}