using System;

namespace UEventBus
{
    public class Subscription
    {
        // 由 Attribute 反射注册时用于批量卸载
        public object Subscriber;

        // 用于日志与元信息
        public SubscriberMethod Method;

        // 非反射调用：已绑定的委托（二者其一非空）
        public Action NoArgAction; // 对应无参方法
        public Action<EventData> DataAction; // 对应单参方法（EventData 或其子类）

        // 用于非反射反注册的原始委托引用（保持引用相等）
        public Delegate RawDelegate;

        public Subscription(object subscriber, SubscriberMethod method)
        {
            Subscriber = subscriber;
            Method = method;
        }

        public Subscription(Action noArgAction, Delegate rawDelegate, SubscriberMethod method = null)
        {
            NoArgAction = noArgAction;
            RawDelegate = rawDelegate;
            Method = method;
        }

        public Subscription(Action<EventData> dataAction, Delegate rawDelegate, SubscriberMethod method = null)
        {
            DataAction = dataAction;
            RawDelegate = rawDelegate;
            Method = method;
        }
    }
}