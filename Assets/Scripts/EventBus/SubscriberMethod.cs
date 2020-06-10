using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

public class SubscriberMethod
{
    public int EventId;
    public MethodInfo Method;
    
    public int Priority;
    public bool Sticky;
}