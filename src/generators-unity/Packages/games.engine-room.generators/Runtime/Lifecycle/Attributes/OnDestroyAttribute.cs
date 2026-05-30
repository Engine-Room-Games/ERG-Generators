using System;

namespace EngineRoom.Runtime.Lifecycle
{
    /// <summary>
    /// Marks a method on a MonoBehaviour to be invoked during the generated
    /// <c>OnDestroy</c> dispatcher. Methods without an explicit <paramref name="order"/>
    /// run in the order they're declared in the class; methods with one are sorted
    /// by ascending order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnDestroyAttribute : Attribute
    {
        public int Order { get; }

        public OnDestroyAttribute()
        {
        }

        public OnDestroyAttribute(int order)
        {
            Order = order;
        }
    }
}
