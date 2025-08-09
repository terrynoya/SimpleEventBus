using UnityEngine;
using System.Reflection;
using System;

public class SubscriberMethod
{
    public int EventId;
    public MethodInfo Method;
    
    public int Priority;
    public bool Sticky;

    // 参数签名缓存
    public int ParameterCount;
    public Type ParameterType;
}