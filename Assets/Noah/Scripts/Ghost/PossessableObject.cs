using UnityEngine;

/// <summary>
/// Enhanced PossessableObject with transform override settings.
/// Use this for objects that need special rotation/scale adjustments (like walls).
/// </summary>
public class PossessableObject : MonoBehaviour
{
    [Header("Basic Settings")]
    public string displayName = "Unnamed Object";
    [Tooltip("Icon to show in the possession menu")]
    public Sprite icon;

    [Header("Clone Prefab")]
    [Tooltip("The prefab that represents what this object looks like when transformed into")]
    public GameObject clonePrefab;

    [Header("Transform Overrides")]
    [Tooltip("Apply custom rotation when transforming into this object")]
    public bool useRotationOverride = false;
    [Tooltip("Rotation to apply (in degrees). Example: (0, 180, 0) to flip around")]
    public Vector3 rotationOverride = Vector3.zero;

    [Tooltip("Apply custom scale when transforming into this object")]
    public bool useScaleOverride = false;
    [Tooltip("Scale to apply. Example: (2, 1, 1) to make it twice as wide")]
    public Vector3 scaleOverride = Vector3.one;

    [Header("Camera Settings (Optional)")]
    [Tooltip("Override third-person camera distance for this object")]
    public bool overrideCameraSettings = false;
    public float customCameraDistance = 3f;
    public float customCameraHeight = 1.5f;
    public Vector3 customCameraLookOffset = new Vector3(0f, 0.5f, 0f);

    private void Start()
    {
        // Validate setup
        if (clonePrefab == null)
        {
            Debug.LogWarning($"⚠️ {displayName} has no clone prefab assigned!");
        }
        else
        {
            // Verify the prefab has a mesh
            MeshFilter mf = clonePrefab.GetComponentInChildren<MeshFilter>();
            MeshRenderer mr = clonePrefab.GetComponentInChildren<MeshRenderer>();

            if (mf == null || mr == null)
            {
                Debug.LogError($"❌ {displayName}'s clone prefab ({clonePrefab.name}) is missing MeshFilter or MeshRenderer!");
            }
            else
            {
                Debug.Log($"✅ {displayName} properly configured");
                if (useRotationOverride)
                    Debug.Log($"  📐 Rotation override: {rotationOverride}");
                if (useScaleOverride)
                    Debug.Log($"  📏 Scale override: {scaleOverride}");
            }
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = gameObject.name.Replace("(Clone)", "").Trim();
        }
    }

    // Apply custom camera settings when ghost transforms into this object
    public void ApplyCameraSettings(TransformationCameraController cameraController)
    {
        if (cameraController == null || !overrideCameraSettings) return;

        Debug.Log($"📷 Applying custom camera settings for {displayName}");
        cameraController.SetThirdPersonDistance(customCameraDistance);
        cameraController.thirdPersonHeight = customCameraHeight;
        cameraController.thirdPersonTargetOffset = customCameraLookOffset;
    }

    // Helper to validate the object is set up correctly
    private void OnValidate()
    {
        // Ensure scale override is never zero
        if (useScaleOverride)
        {
            if (scaleOverride.x == 0) scaleOverride.x = 1;
            if (scaleOverride.y == 0) scaleOverride.y = 1;
            if (scaleOverride.z == 0) scaleOverride.z = 1;
        }
    }
}