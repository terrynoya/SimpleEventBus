using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UEventBus
{

    public class EventBus
    {
        private static EventBus _instance;

        private Dictionary<int, List<Subscription>> _subMap;

        public static EventBus Default
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventBus();
                }
                return _instance;
            }
        }

        public EventBus()
        {
            _subMap = new Dictionary<int, List<Subscription>>();
        }

        public void Register(object subscriber)
        {
            var subClass = subscriber.GetType();
            var methodList = FindSubscriberMethods(subClass);
            for (int i = 0; i < methodList.Count; i++)
            {
                var method = methodList[i];
                Subscribe(subscriber, method);
            }
        }

        public void UnRegister(object subscriber)
        {
            var subClass = subscriber.GetType();
            var methodList = FindSubscriberMethods(subClass);
            for (int i = 0; i < methodList.Count; i++)
            {
                var method = methodList[i];
                UnSubscribe(subscriber, method);
            }
        }

        public void Post(int eventId, object data = null)
        {
            if (!_subMap.ContainsKey(eventId))
            {
                return;
            }
            var subList = _subMap[eventId];
            for (int i = 0; i < subList.Count; i++)
            {
                var sub = subList[i];
                var sm = sub.Method;

                if (sm.ParameterCount == 0)
                {
                    sm.Method.Invoke(sub.Subscriber, null);
                    continue;
                }

                if (sm.ParameterCount == 1)
                {
                    if (data == null)
                    {
                        if (sm.ParameterType.IsValueType && Nullable.GetUnderlyingType(sm.ParameterType) == null)
                        {
                            Debug.LogWarning($"[EventBus] Skip invoke: event {eventId} expects non-null '{sm.ParameterType.Name}' but data is null. Method: {sm.Method.DeclaringType.FullName}.{sm.Method.Name}");
                            continue;
                        }
                        sm.Method.Invoke(sub.Subscriber, new object[] { null });
                        continue;
                    }

                    if (!sm.ParameterType.IsInstanceOfType(data))
                    {
                        Debug.LogWarning($"[EventBus] Skip invoke: event {eventId} expects '{sm.ParameterType.Name}', got '{data.GetType().Name}'. Method: {sm.Method.DeclaringType.FullName}.{sm.Method.Name}");
                        continue;
                    }

                    sm.Method.Invoke(sub.Subscriber, new object[] { data });
                    continue;
                }

                Debug.LogWarning($"[EventBus] Skip invoke: method with unsupported parameter count {sm.ParameterCount}. Method: {sm.Method.DeclaringType.FullName}.{sm.Method.Name}");
            }
        }

        private List<SubscriberMethod> FindSubscriberMethods(Type subscriberClass)
        {
            var rlt = new List<SubscriberMethod>();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var methods = subscriberClass.GetMethods(flags);

            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (method.IsStatic)
                {
                    continue;
                }
                var attr = method.GetCustomAttribute<SubscribeAttribute>();
                if (attr != null)
                {
                    var m = new SubscriberMethod();
                    m.EventId = attr.EventId;
                    m.Method = method;
                    var parameters = method.GetParameters();
                    m.ParameterCount = parameters.Length;
                    if (m.ParameterCount > 1)
                    {
                        throw new InvalidOperationException(
                            $"[EventBus] Subscribe method supports only 0 or 1 parameter. Found {m.ParameterCount}. Method: {method.DeclaringType.FullName}.{method.Name}");
                    }

                    if (m.ParameterCount == 1)
                    {
                        var paramType = parameters[0].ParameterType;
                        if (!typeof(EventData).IsAssignableFrom(paramType))
                        {
                            throw new InvalidOperationException(
                                $"[EventBus] Subscribe method's single parameter must be EventData or its subclass. Found: {paramType.FullName}. Method: {method.DeclaringType.FullName}.{method.Name}");
                        }
                        m.ParameterType = paramType;
                    }
                    else
                    {
                        m.ParameterType = null;
                    }
                    rlt.Add(m);
                }
            }
            return rlt;
        }

        private void Subscribe(object subscriber, SubscriberMethod method)
        {
            int eventId = method.EventId;
            if (!_subMap.ContainsKey(eventId))
            {
                _subMap.Add(eventId, new List<Subscription>());
            }
            var subList = _subMap[eventId];
            subList.Add(new Subscription(subscriber, method));
        }

        private void UnSubscribe(object subscriber, SubscriberMethod method)
        {
            int eventId = method.EventId;
            if (!_subMap.ContainsKey(eventId)) 
            {
                return;
            }
            var subList = _subMap[eventId];
            var removeSubList = new List<Subscription>();
            for (int i = 0; i < subList.Count; i++)
            {
                var subscription = subList[i];
                if(subscription.Subscriber == subscriber)
                {
                    removeSubList.Add(subscription);
                }
            }

            for (int i = 0; i < removeSubList.Count; i++)
            {
                subList.Remove(removeSubList[i]);
            }
        }
    }
}