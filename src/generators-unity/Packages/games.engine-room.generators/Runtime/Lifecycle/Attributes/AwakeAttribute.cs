using System;

namespace EngineRoom.Runtime.Lifecycle
{
    /// <summary>
    /// Marks a method on a MonoBehaviour to be invoked during the generated
    /// <c>Awake</c> dispatcher. Methods without an explicit <paramref name="order"/>
    /// run in the order they're declared in the class; methods with one are sorted
    /// by ascending order. <c>[Singleton]</c> initialization always runs first
    /// regardless of order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AwakeAttribute : Attribute
    {
        public int Order { get; }

        public AwakeAttribute()
        {
        }

        public AwakeAttribute(int order)
        {
            Order = order;
        }
    }
}
