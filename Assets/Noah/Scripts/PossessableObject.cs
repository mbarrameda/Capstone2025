using UnityEngine;

public class PossessableObject : MonoBehaviour
{
    [Header("Possession Settings")]
    public float fearCost = 10f;
    public float fearReturnBonus = 5f;
    public bool isPossessed = false;
    public bool canBeTransformedInto = true;

    [Header("Clone Prefab")]
    public GameObject clonePrefab; // assign prefab for cloned version

    private GhostController possessingGhost;

    public bool TryPossess(GhostController ghost)
    {
        if (isPossessed || ghost.fear < fearCost)
            return false;

        ghost.fear -= fearCost;
        isPossessed = true;
        possessingGhost = ghost;
        ghost.RegisterPossessedObject(this);

        return true;
    }

    public void OnExplorerInteract()
    {
        if (isPossessed && possessingGhost != null)
        {
            possessingGhost.fear += fearCost + fearReturnBonus;
            possessingGhost.fear = Mathf.Min(possessingGhost.fear, 100f);
            isPossessed = false;

            if (TryGetComponent<Renderer>(out Renderer r))
                r.material.color = Color.white;
        }
    }
}
