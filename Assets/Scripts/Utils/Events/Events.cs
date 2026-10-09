using System;
using UnityEngine;

public interface IEvent { }

public static class EventExtensions
{
    // Allows you to just call an event via .Invoke() instead of typing out the entire event bus.
    public static void Invoke<TEvent>(this TEvent anEvent) where TEvent : IEvent => EventBus.Raise(anEvent);

    // Allows you to directly subscribe a lifecycle managed event for any Monobehaviour.
    // Handler callback will be called whenever TEvent is Invoked. 
    public static void Subscribe<TEvent>(this MonoBehaviour caller, Action<TEvent> handler) where TEvent : IEvent
    {
        if (!caller.TryGetComponent(out EventLifecycleHost host))
        {
            host = caller.gameObject.AddComponent<EventLifecycleHost>();
        }

        host.AddSubscription(handler);
    }

    public static void UnSubscribe<TEvent>(this MonoBehaviour caller, Action<TEvent> handler) where TEvent : IEvent
    {
        if (caller.TryGetComponent(out EventLifecycleHost host))
        {
            host.RemoveSubscription(handler);
        }
    }
}