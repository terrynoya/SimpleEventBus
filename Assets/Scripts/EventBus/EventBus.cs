using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UEventBus
{

    public class EventBus
    {
        private static EventBus _instance;

        private readonly Dictionary<int, List<Subscription>> _subMap;
        private readonly Dictionary<Type, List<SubscriberMethod>> _methodCache = new Dictionary<Type, List<SubscriberMethod>>();

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

                // 优先使用已绑定委托
                if (sub.NoArgAction != null)
                {
                    sub.NoArgAction();
                    continue;
                }

                if (sub.DataAction != null)
                {
                    if (data == null)
                    {
                        sub.DataAction(null);
                        continue;
                    }

                    if (!sm.ParameterType.IsInstanceOfType(data))
                    {
                        Debug.LogWarning($"[EventBus] Skip invoke: event {eventId} expects '{sm.ParameterType.Name}', got '{data.GetType().Name}'. Method: {sm.Method.DeclaringType.FullName}.{sm.Method.Name}");
                        continue;
                    }
                    sub.DataAction((EventData)data);
                    continue;
                }

                // 兼容：无委托缓存时回退反射调用（不建议出现）
                if (sm.ParameterCount == 0)
                {
                    sm.Method.Invoke(sub.Subscriber, null);
                    continue;
                }
                if (sm.ParameterCount == 1)
                {
                    sm.Method.Invoke(sub.Subscriber, new object[] { data });
                    continue;
                }

                Debug.LogWarning($"[EventBus] Skip invoke: method with unsupported parameter count {sm.ParameterCount}. Method: {sm.Method.DeclaringType.FullName}.{sm.Method.Name}");
            }
        }

        private List<SubscriberMethod> FindSubscriberMethods(Type subscriberClass)
        {
            if (_methodCache.TryGetValue(subscriberClass, out var cached))
            {
                return cached;
            }

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
            _methodCache[subscriberClass] = rlt;
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
            var subscription = new Subscription(subscriber, method);

            // 绑定委托以避免 Post 时反射
            if (method.ParameterCount == 0)
            {
                try
                {
                    var action = (Action)method.Method.CreateDelegate(typeof(Action), subscriber);
                    subscription.NoArgAction = action;
                }
                catch { /* 忽略，保留反射后备方案 */ }
            }
            else if (method.ParameterCount == 1)
            {
                try
                {
                    var paramType = method.ParameterType;
                    if (paramType == typeof(EventData))
                    {
                        var action = (Action<EventData>)method.Method.CreateDelegate(typeof(Action<EventData>), subscriber);
                        subscription.DataAction = action;
                    }
                    else
                    {
                        // 为派生类型生成包装委托
                        subscription.DataAction = CreateDataActionWrapper(subscriber, method.Method, paramType);
                    }
                }
                catch { /* 忽略，保留反射后备方案 */ }
            }

            subList.Add(subscription);
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

        // 非反射注册 API
        public void Subscribe(int eventId, Action handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_subMap.ContainsKey(eventId))
            {
                _subMap.Add(eventId, new List<Subscription>());
            }
            _subMap[eventId].Add(new Subscription(handler, handler, null));
        }

        public void Subscribe<T>(int eventId, Action<T> handler) where T : EventData
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_subMap.ContainsKey(eventId))
            {
                _subMap.Add(eventId, new List<Subscription>());
            }
            // 存储为统一的 Action<EventData> 包装
            Action<EventData> wrapper = e => handler((T)e);
            var sub = new Subscription(wrapper, handler, null)
            {
                // Method 仅用于日志，在非反射注册场景可为空
                Method = new SubscriberMethod { EventId = eventId, ParameterCount = 1, ParameterType = typeof(T) }
            };
            _subMap[eventId].Add(sub);
        }

        public void UnSubscribe(int eventId, Action handler)
        {
            if (!_subMap.ContainsKey(eventId) || handler == null) return;
            var subList = _subMap[eventId];
            subList.RemoveAll(s => s.RawDelegate == (Delegate)handler || s.NoArgAction == handler);
        }

        public void UnSubscribe<T>(int eventId, Action<T> handler) where T : EventData
        {
            if (!_subMap.ContainsKey(eventId) || handler == null) return;
            var subList = _subMap[eventId];
            subList.RemoveAll(s => Equals(s.RawDelegate, handler));
        }

        private static Action<EventData> CreateDataActionWrapper(object target, MethodInfo method, Type paramType)
        {
            // 通过泛型辅助方法在注册阶段创建包装委托
            var helper = typeof(EventBus).GetMethod(nameof(CreateWrapperGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            var generic = helper.MakeGenericMethod(paramType);
            return (Action<EventData>)generic.Invoke(null, new object[] { target, method });
        }

        private static Action<EventData> CreateWrapperGeneric<T>(object target, MethodInfo method) where T : EventData
        {
            var typed = (Action<T>)method.CreateDelegate(typeof(Action<T>), target);
            return (EventData e) => typed((T)e);
        }
    }
}