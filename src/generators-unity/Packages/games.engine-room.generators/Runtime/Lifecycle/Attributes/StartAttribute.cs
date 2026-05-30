using System;

namespace EngineRoom.Runtime.Lifecycle
{
    /// <summary>
    /// Marks a method on a MonoBehaviour to be invoked during the generated
    /// <c>Start</c> dispatcher. Methods without an explicit <paramref name="order"/>
    /// run in the order they're declared in the class; methods with one are sorted
    /// by ascending order. <c>[Dependency]</c> field resolution always runs first
    /// regardless of order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StartAttribute : Attribute
    {
        public int Order { get; }

        public StartAttribute()
        {
        }

        public StartAttribute(int order)
        {
            Order = order;
        }
    }
}
