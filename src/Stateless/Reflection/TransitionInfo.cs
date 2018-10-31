using System.Collections.Generic;

namespace Stateless.Reflection
{
    /// <summary>
    /// </summary>
    public abstract class TransitionInfo
    {
        /// <summary>
        ///     Method descriptions of the guard conditions.
        ///     Returns a non-null but empty list if there are no guard conditions
        /// </summary>
        public IEnumerable<InvocationInfo> guardConditionsMethodDescriptions;

        /// <summary>
        ///     The trigger whose firing resulted in this transition.
        /// </summary>
        public TriggerInfo Trigger { get; protected set; }
    }
}