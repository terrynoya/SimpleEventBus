using UnityEngine;
using UnityEditor;
using System;

namespace UEventBus
{
    public class SubscribeAttribute : Attribute
    {
        public int EventId;

        public SubscribeAttribute(int eventId)
        {
            EventId = eventId;
        }
    }
}