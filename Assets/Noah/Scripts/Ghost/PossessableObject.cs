using UnityEngine;

public class PossessableObject : MonoBehaviour
{
    [Header("Possession Settings")]
    public float fearCost = 10f;
    public float fearReturnBonus = 5f;
    public bool isPossessed = false;
    public bool canBeTransformedInto = true;

    [Header("Clone Prefab")]
    [Tooltip("The prefab that represents what this object looks like when transformed into")]
    public GameObject clonePrefab;

    [Header("UI Info")]
    public string displayName = "Unnamed Object";
    [Tooltip("Icon to show in the possession menu")]
    public Sprite icon;

    private GhostController possessingGhost;

    private void Start()
    {
        // Validate setup
        if (clonePrefab == null)
        {
            Debug.LogWarning($"⚠️ {displayName} has no clone prefab assigned! Ghost won't be able to transform into this object.");
        }
        else
        {
            // Verify the prefab has a mesh
            MeshFilter mf = clonePrefab.GetComponentInChildren<MeshFilter>();
            MeshRenderer mr = clonePrefab.GetComponentInChildren<MeshRenderer>();

            if (mf == null || mr == null)
            {
                Debug.LogError($"❌ {displayName}'s clone prefab ({clonePrefab.name}) is missing MeshFilter or MeshRenderer! Add these components.");
            }
            else
            {
                Debug.Log($"✅ {displayName} properly configured with mesh: {mf.sharedMesh?.name}");
            }
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = gameObject.name.Replace("(Clone)", "").Trim();
        }
    }

    public bool TryPossess(GhostController ghost)
    {
        if (isPossessed || ghost.fear < fearCost)
            return false;

        ghost.fear -= fearCost;
        isPossessed = true;
        possessingGhost = ghost;
        ghost.RegisterPossessedObject(this);

        // Visual feedback
        if (TryGetComponent<Renderer>(out Renderer r))
        {
            r.material.color = new Color(0.5f, 0.8f, 1f); // Light blue tint
        }

        Debug.Log($"✅ {displayName} possessed by ghost");
        return true;
    }

    public void OnExplorerInteract()
    {
        if (isPossessed && possessingGhost != null)
        {
            possessingGhost.fear += fearCost + fearReturnBonus;
            possessingGhost.fear = Mathf.Min(possessingGhost.fear, 100f);
            isPossessed = false;

            // Visual feedback
            if (TryGetComponent<Renderer>(out Renderer r))
                r.material.color = Color.white;

            Debug.Log($"✅ {displayName} unpossessed - ghost gained {fearCost + fearReturnBonus} fear");
        }
    }
}