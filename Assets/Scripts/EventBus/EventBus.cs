using UnityEngine;
using UnityEditor;
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
            Type subClass = subscriber.GetType();
            List<SubscriberMethod> methodList = FindSubscriberMethods(subClass);
            for (int i = 0; i < methodList.Count; i++)
            {
                SubscriberMethod method = methodList[i];
                Subscribe(subscriber, method);
            }
        }

        public void UnRegister(object subscriber)
        {
            Type subClass = subscriber.GetType();
            List<SubscriberMethod> methodList = FindSubscriberMethods(subClass);
            for (int i = 0; i < methodList.Count; i++)
            {
                SubscriberMethod method = methodList[i];
                UnSubscribe(subscriber, method);
            }
        }

        public void Post(int eventId, object data = null)
        {
            if (!_subMap.ContainsKey(eventId))
            {
                return;
            }
            List<Subscription> subList = _subMap[eventId];
            for (int i = 0; i < subList.Count; i++)
            {
                Subscription sub = subList[i];
                if (data != null)
                {
                    sub.Method.Method.Invoke(sub.Subscriber, new object[] { data });
                }
                else
                {
                    sub.Method.Method.Invoke(sub.Subscriber, null);
                }
            }
        }

        private List<SubscriberMethod> FindSubscriberMethods(Type subscriberClass)
        {
            List<SubscriberMethod> rlt = new List<SubscriberMethod>();

            MethodInfo[] methods = subscriberClass.GetMethods();

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                SubscribeAttribute attr = method.GetCustomAttribute<SubscribeAttribute>();
                if (attr != null)
                {
                    SubscriberMethod m = new SubscriberMethod();
                    m.EventId = attr.EventId;
                    m.Method = method;
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
            List<Subscription> subList = _subMap[eventId];
            subList.Add(new Subscription(subscriber, method));
        }

        private void UnSubscribe(object subscriber, SubscriberMethod method)
        {
            int eventId = method.EventId;
            if (!_subMap.ContainsKey(eventId)) 
            {
                return;
            }
            List<Subscription> subList = _subMap[eventId];
            List<Subscription> removeSubList = new List<Subscription>();
            for (int i = 0; i < subList.Count; i++)
            {
                Subscription subscription = subList[i];
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