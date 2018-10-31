using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        abstract class UnhandledTriggerAction
        {
            public abstract void Execute(TState state, TTrigger trigger, ICollection<string> unmetGuards);
            public abstract Task ExecuteAsync(TState state, TTrigger trigger, ICollection<string> unmetGuards);

            internal class Sync : UnhandledTriggerAction
            {
                private readonly Action<TState, TTrigger, ICollection<string>> action;

                internal Sync(Action<TState, TTrigger, ICollection<string>> action = null)
                {
                    this.action = action;
                }

                public override void Execute(TState state, TTrigger trigger, ICollection<string> unmetGuards)
                {
                    this.action(state, trigger, unmetGuards);
                }

                public override Task ExecuteAsync(TState state, TTrigger trigger, ICollection<string> unmetGuards)
                {
                    Execute(state, trigger, unmetGuards);
                    return TaskResult.done;
                }
            }

            internal class Async : UnhandledTriggerAction
            {
                private readonly Func<TState, TTrigger, ICollection<string>, Task> action;

                internal Async(Func<TState, TTrigger, ICollection<string>, Task> action)
                {
                    this.action = action;
                }

                public override void Execute(TState state, TTrigger trigger, ICollection<string> unmetGuards)
                {
                    throw new InvalidOperationException(
                        "Cannot execute asynchronous action specified in OnUnhandledTrigger. " +
                        "Use asynchronous version of Fire [FireAsync]");
                }

                public override Task ExecuteAsync(TState state, TTrigger trigger, ICollection<string> unmetGuards)
                {
                    return this.action(state, trigger, unmetGuards);
                }
            }
        }
    }
}