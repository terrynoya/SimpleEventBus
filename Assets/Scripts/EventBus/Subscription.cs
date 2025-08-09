using UnityEngine;

namespace UEventBus
{
    public class Subscription
    {
        public object Subscriber;
        public SubscriberMethod Method;

        public Subscription(object subscriber, SubscriberMethod method)
        {
            Subscriber = subscriber;
            Method = method;
        }
    }
}