﻿using System;
using System.Threading.Tasks;
using Stateless.Reflection;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class EntryActionBehavior
        {
            protected EntryActionBehavior(InvocationInfo description)
            {
                Description = description;
            }

            public InvocationInfo Description { get; }

            public abstract void Execute(Transition transition, object[] args);
            public abstract Task ExecuteAsync(Transition transition, object[] args);

            public class Sync : EntryActionBehavior
            {
                private readonly Action<Transition, object[]> action;

                public Sync(Action<Transition, object[]> action, InvocationInfo description) : base(description)
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

            public class SyncFrom<TTriggerType> : Sync
            {
                public SyncFrom(TTriggerType trigger, Action<Transition, object[]> action, InvocationInfo description)
                    : base(action, description)
                {
                    Trigger = trigger;
                }

                internal TTriggerType Trigger { get; }

                public override void Execute(Transition transition, object[] args)
                {
                    if (transition.Trigger.Equals(Trigger))
                        base.Execute(transition, args);
                }

                public override Task ExecuteAsync(Transition transition, object[] args)
                {
                    Execute(transition, args);
                    return TaskResult.done;
                }
            }

            public class Async : EntryActionBehavior
            {
                private readonly Func<Transition, object[], Task> action;

                public Async(Func<Transition, object[], Task> action, InvocationInfo description) : base(description)
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