using Stateless.Reflection;

namespace Stateless.Graph
{
    /// <summary>
    ///     Used to keep track of the decision point of a dynamic transition
    /// </summary>
    internal class Decision : State
    {
        internal Decision(InvocationInfo method, int num)
            : base("Decision" + num)
        {
            this.Method = method;
        }

        public InvocationInfo Method { get; }
    }
}