using UnityEngine;
using System;

// This class holds the event and does not need to be attached to a GameObject.
public static class ChangeTracker
{
    // The static event that other scripts can subscribe to.
    public static event Action OnChangeOccurred;

    // The public method that tracked objects will call to signal a change.
    public static void TriggerChange()
    {
        // Null-check ensures there are listeners before the event is invoked.
        OnChangeOccurred?.Invoke();
        Debug.Log("Game Change Event Triggered!");
    }
}