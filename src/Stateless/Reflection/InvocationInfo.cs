using System;

namespace Stateless.Reflection
{
    /// <summary>
    ///     Describes a method - either an action (activate, deactivate, etc.) or a transition guard
    /// </summary>
    public class InvocationInfo
    {
        private readonly string description; // _description can be null if user didn't specify a description
        private readonly Timing timing;

        private InvocationInfo(string methodName, string description,
            Timing timing) // description can be null if user didn't specify a description
        {
            this.MethodName = methodName;
            this.description = description;
            this.timing = timing;
        }

        /// <summary>
        ///     The name of the invoked method.  If the method is a lambda or delegate, the name will be a compiler-generated
        ///     name that is often not human-friendly (e.g. "(.ctor)b__2_0" except with angle brackets instead of parentheses)
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        ///     Text returned for compiler-generated functions where the caller has not specified a description
        /// </summary>
        public static string DefaultFunctionDescription { get; set; } = "Function";

        /// <summary>
        ///     A description of the invoked method.  Returns:
        ///     1) The user-specified description, if any
        ///     2) else if the method name is compiler-generated, returns DefaultFunctionDescription
        ///     3) else the method name
        /// </summary>
        public string Description
        {
            get
            {
                if (this.description != null)
                {
                    return this.description;
                }

                if (this.MethodName.IndexOfAny(new[] {'<', '>', '`'}) >= 0)
                {
                    return DefaultFunctionDescription;
                }

                return this.MethodName ?? "<null>";
            }
        }

        /// <summary>
        ///     Returns true if the method is invoked asynchronously.
        /// </summary>
        public bool IsAsync => this.timing == Timing.Asynchronous;

        internal static InvocationInfo Create(Delegate method, string description, Timing timing = Timing.Synchronous)
        {
            return new InvocationInfo(method?.TryGetMethodName(), description, timing);
        }

        /// <summary>
        ///     Is the method synchronous or asynchronous?
        /// </summary>
        internal enum Timing
        {
            /// <summary>Method is synchronous</summary>
            Synchronous,

            /// <summary>Method is asynchronous</summary>
            Asynchronous
        }
    }
}