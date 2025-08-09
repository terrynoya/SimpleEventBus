using System;
using System.Collections;
using UnityEngine;
using UEventBus;

public static class EventIds
{
    public const int NoArg = 1;
    public const int WithData = 2;
}

public class EventBusTestBehaviour : MonoBehaviour
{
    private bool _unregisteredInStart;

    private void OnEnable()
    {
        EventBus.Default.Register(this);
        Debug.Log("[EventBusTest] Registered EventBusTestBehaviour");
    }

    private void OnDisable()
    {
        EventBus.Default.UnRegister(this);
        Debug.Log("[EventBusTest] Unregistered EventBusTestBehaviour (OnDisable)");
    }

    private IEnumerator Start()
    {
        Debug.Log("[EventBusTest] Start -> Post initial events");

        // 无参事件
        EventBus.Default.Post(EventIds.NoArg);

        // 携带数据事件
        var data = new EventData
        {
            EventSource = this,
            EventTime = DateTime.UtcNow
        };
        EventBus.Default.Post(EventIds.WithData, data);

        // 等待一帧，随后取消注册并再次发送，验证不会再收到
        yield return null;

        EventBus.Default.UnRegister(this);
        _unregisteredInStart = true;
        Debug.Log("[EventBusTest] Manually Unregistered in Start -> Post events again (should not trigger)");

        EventBus.Default.Post(EventIds.NoArg);
        EventBus.Default.Post(EventIds.WithData, data);
    }

    [Subscribe(EventIds.NoArg)]
    private void OnNoArgEvent()
    {
        Debug.Log("[EventBusTest] OnNoArgEvent received");
    }

    [Subscribe(EventIds.WithData)]
    private void OnDataEvent(EventData eventData)
    {
        Debug.Log($"[EventBusTest] OnDataEvent received -> Source: {eventData.EventSource}, Time(UTC): {eventData.EventTime:O}");
    }
}

public class EventBusSecondaryListener : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Default.Register(this);
        Debug.Log("[EventBusTest] Registered SecondaryListener");
    }

    private void OnDisable()
    {
        EventBus.Default.UnRegister(this);
        Debug.Log("[EventBusTest] Unregistered SecondaryListener");
    }

    [Subscribe(EventIds.WithData)]
    private void OnDataEventFromSecondary(EventData eventData)
    {
        Debug.Log("[EventBusTest][Secondary] OnDataEvent received");
    }
}


