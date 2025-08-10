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
    private Action _noArgHandler;                    // 非反射：无参
    private Action<EventData> _dataHandler;          // 非反射：带数据

    private void OnEnable()
    {
        EventBus.Default.Register(this);
        Debug.Log("[EventBusTest] Registered EventBusTestBehaviour");

        // 非反射订阅示例
        _noArgHandler = () => Debug.Log("[EventBusTest][NonReflect] NoArg received");
        EventBus.Default.Subscribe(EventIds.NoArg, _noArgHandler);

        _dataHandler = e => Debug.Log($"[EventBusTest][NonReflect] WithData received -> Source: {e?.EventSource}, Time(UTC): {e?.EventTime:O}");
        EventBus.Default.Subscribe<EventData>(EventIds.WithData, _dataHandler);
    }

    private void OnDisable()
    {
        EventBus.Default.UnRegister(this);
        Debug.Log("[EventBusTest] Unregistered EventBusTestBehaviour (OnDisable)");

        // 取消非反射订阅
        if (_noArgHandler != null)
        {
            EventBus.Default.UnSubscribe(EventIds.NoArg, _noArgHandler);
        }
        if (_dataHandler != null)
        {
            EventBus.Default.UnSubscribe<EventData>(EventIds.WithData, _dataHandler);
        }
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
        // 同时取消非反射订阅，演示非反射反注册
        if (_noArgHandler != null)
        {
            EventBus.Default.UnSubscribe(EventIds.NoArg, _noArgHandler);
            _noArgHandler = null;
        }
        if (_dataHandler != null)
        {
            EventBus.Default.UnSubscribe<EventData>(EventIds.WithData, _dataHandler);
            _dataHandler = null;
        }

        Debug.Log("[EventBusTest] Manually Unregistered in Start (including non-reflect) -> Post events again (should not trigger)");

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


