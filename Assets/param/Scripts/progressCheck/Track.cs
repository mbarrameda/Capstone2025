using UnityEngine;

public class TrackedObject : MonoBehaviour
{
    // A flag to prevent the event from firing multiple times per object
    private bool hasChanged = false;

    // --- Initial States ---
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private int _initialChildCount;
    // Set to Color.clear as a default if no Renderer is found
    private Color _initialColor = Color.clear;

    // --- Components & Thresholds ---
    private Renderer _renderer;

    [Tooltip("The distance (in meters) the object must move from its start position to be considered a 'change'.")]
    public float positionThreshold = 0.5f;

    [Tooltip("The angle (in degrees) the object must rotate from its start rotation to be considered a 'change'.")]
    public float rotationThreshold = 5.0f;

    void Start()
    {
        // 1. Record initial Transform properties
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _initialChildCount = transform.childCount;

        // 2. Try to get the Renderer component for color tracking
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            // Assuming the object uses a material with a main color property (standard shader)
            _initialColor = _renderer.material.color;
        }
        else
        {
            // Optional: Log a warning if color tracking is requested but no renderer is present
            Debug.LogWarning($"Object '{gameObject.name}' has no Renderer component. Color changes will not be tracked.");
        }
    }

    // This Update function now automatically checks for all four types of changes
    void Update()
    {
        // Stop checking once the change has been registered
        if (hasChanged) return;

        bool changeDetected = false;
        string changeType = "None";

        // 1. Check Position Change
        if (Vector3.Distance(transform.position, _initialPosition) >= positionThreshold)
        {
            changeDetected = true;
            changeType = "Position";
        }

        // 2. Check Rotation Change (using Quaternion.Angle for threshold comparison)
        if (!changeDetected && Quaternion.Angle(transform.rotation, _initialRotation) >= rotationThreshold)
        {
            changeDetected = true;
            changeType = "Rotation";
        }

        // 3. Check Child Count Change (when children are added or removed)
        if (!changeDetected && transform.childCount != _initialChildCount)
        {
            changeDetected = true;
            changeType = "Child Count";
        }

        // 4. Check Color Change (only if a renderer exists and color is different)
        if (!changeDetected && _renderer != null && _renderer.material.color != _initialColor)
        {
            // Note: Direct Color comparison works for simple changes like color = new Color(...)
            changeDetected = true;
            changeType = "Color";
        }

        // If any of the checks passed, trigger the global event
        if (changeDetected)
        {
            ChangeTracker.TriggerChange();
            hasChanged = true;
            Debug.Log($"Object '{gameObject.name}' registered a change due to: {changeType}.");
        }
    }
}