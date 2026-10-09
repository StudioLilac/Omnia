using System;
using System.Collections.Generic;
using UnityEngine;

public class EventLifecycleHost : MonoBehaviour
{
    private readonly Dictionary<Delegate, ISubscription> activeSubscriptions = new();

    private void OnEnable()
    {
        foreach (var sub in activeSubscriptions.Values) sub.Enable();
    }

    private void OnDisable()
    {
        foreach (var sub in activeSubscriptions.Values) sub.Disable();
    }

    public void AddSubscription<T>(Action<T> handler) where T : IEvent
    {
        if (activeSubscriptions.ContainsKey(handler))
        {
            Debug.LogWarning($"Attempted to add duplicate subscription for event {typeof(T).Name} on {gameObject.name}. Ignoring.");
            return;
        }

        EventSubscription<T> subscription = new(handler);

        activeSubscriptions.Add(handler, subscription);
        if (enabled && gameObject.activeInHierarchy)
            subscription.Enable();
    }

    public void RemoveSubscription<T>(Action<T> handler) where T : IEvent
    {
        if (activeSubscriptions.TryGetValue(handler, out var subscription))
        {
            if (enabled && gameObject.activeInHierarchy)
                subscription.Disable();
            
            activeSubscriptions.Remove(handler);
        }
    }

    private void OnDestroy()
    {
        foreach (var sub in activeSubscriptions.Values) sub.Disable();
        activeSubscriptions.Clear();
    }

    private interface ISubscription
    {
        void Enable();
        void Disable();
    }

    private record EventSubscription<T>(Action<T> Callback) : ISubscription where T : IEvent
    {
        public void Enable() => EventBus.Register(Callback);
        public void Disable() => EventBus.Unregister(Callback);
    }
}