using System;

namespace UEventBus
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class SubscribeTypeAttribute : Attribute
    {
        public Type EventType { get; }

        public SubscribeTypeAttribute(Type eventType)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        }
    }
}


