using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enhanced version with per-object settings for rotation and scale overrides.
/// FIXED: Now properly uses the prefab's actual scale instead of ghost's scale.
/// </summary>
public static class TransformApplier
{
    private class Snapshot
    {
        public GameObject visualClone;
        public List<(Renderer renderer, bool wasEnabled)> originalRenderers = new();
        public List<Collider> addedColliders = new();
        public Vector3 originalLocalScale;
    }

    private static readonly Dictionary<GameObject, Snapshot> _snapshots = new();

    public static void Apply(GameObject target, GameObject source, PossessableObject settings = null)
    {
        if (target == null || source == null)
        {
            Debug.LogError("TransformApplier.Apply: target or source is null.");
            return;
        }

        if (_snapshots.ContainsKey(target))
            Revert(target);

        var snap = new Snapshot();
        snap.originalLocalScale = target.transform.localScale;

        // Hide ALL renderers on the ghost (mesh + children)
        Renderer[] allRenderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in allRenderers)
        {
            if (r != null)
            {
                snap.originalRenderers.Add((r, r.enabled));
                r.enabled = false;
            }
        }

        // Instantiate the source's visual structure as a child
        snap.visualClone = Object.Instantiate(source);
        snap.visualClone.name = source.name + "_VisualClone";

        // Parent it to the target, positioned at target's origin
        snap.visualClone.transform.SetParent(target.transform, false);
        snap.visualClone.transform.localPosition = Vector3.zero;

        // Apply rotation override if specified in settings
        Quaternion rotationOverride = Quaternion.identity;
        if (settings != null && settings.useRotationOverride)
        {
            rotationOverride = Quaternion.Euler(settings.rotationOverride);
            Debug.Log($"📐 Applying rotation override: {settings.rotationOverride}");
        }
        snap.visualClone.transform.localRotation = rotationOverride;

        // 🔥 FIX: Use the prefab's actual local scale, NOT lossyScale
        // lossyScale gives you the world scale which includes parent transforms
        // localScale gives you the prefab's own scale
        Vector3 finalScale = source.transform.localScale;

        // If settings specify a scale override, use that instead
        if (settings != null && settings.useScaleOverride)
        {
            finalScale = settings.scaleOverride;
            Debug.Log($"📏 Applying scale override: {settings.scaleOverride}");
        }
        else
        {
            Debug.Log($"📏 Using prefab's original scale: {finalScale}");
        }

        snap.visualClone.transform.localScale = finalScale;

        // Remove any scripts/logic from the clone — we only want visuals
        StripNonVisualComponents(snap.visualClone);

        // Copy colliders from source root to target root
        Collider[] srcColliders = source.GetComponents<Collider>();
        foreach (Collider srcCol in srcColliders)
        {
            Collider newCol = CopyCollider(target, srcCol);
            if (newCol != null)
                snap.addedColliders.Add(newCol);
        }

        // Also grab colliders from child objects if they exist
        Collider[] childColliders = source.GetComponentsInChildren<Collider>();
        foreach (Collider srcCol in childColliders)
        {
            if (srcCol.gameObject == source) continue;

            Collider newCol = CopyColliderFromChild(target, srcCol, source.transform);
            if (newCol != null)
                snap.addedColliders.Add(newCol);
        }

        _snapshots[target] = snap;

        Debug.Log($"✅ TransformApplier: cloned visual structure from '{source.name}' " +
                  $"(scale {finalScale}) onto '{target.name}' with {snap.addedColliders.Count} collider(s).");
    }

    public static void Revert(GameObject target)
    {
        if (target == null) return;

        if (!_snapshots.TryGetValue(target, out Snapshot snap))
        {
            Debug.LogWarning("TransformApplier.Revert: nothing to revert on " + target.name);
            return;
        }

        // Destroy the visual clone
        if (snap.visualClone != null)
            Object.Destroy(snap.visualClone);

        // Remove added colliders
        foreach (Collider col in snap.addedColliders)
        {
            if (col != null) Object.Destroy(col);
        }

        // Restore all ghost renderers to their original states
        foreach (var (renderer, wasEnabled) in snap.originalRenderers)
        {
            if (renderer != null)
                renderer.enabled = wasEnabled;
        }

        // Restore original scale
        target.transform.localScale = snap.originalLocalScale;

        _snapshots.Remove(target);

        Debug.Log($"✅ TransformApplier: reverted '{target.name}' back to ghost.");
    }

    private static void StripNonVisualComponents(GameObject clone)
    {
        foreach (MonoBehaviour script in clone.GetComponentsInChildren<MonoBehaviour>())
        {
            Object.Destroy(script);
        }

        foreach (Collider col in clone.GetComponentsInChildren<Collider>())
        {
            Object.Destroy(col);
        }

        foreach (Rigidbody rb in clone.GetComponentsInChildren<Rigidbody>())
        {
            Object.Destroy(rb);
        }
    }

    private static Collider CopyCollider(GameObject target, Collider source)
    {
        if (source is BoxCollider box)
        {
            var c = target.AddComponent<BoxCollider>();
            c.center = box.center;
            c.size = box.size;
            c.isTrigger = box.isTrigger;
            c.material = box.material;
            return c;
        }
        if (source is SphereCollider sphere)
        {
            var c = target.AddComponent<SphereCollider>();
            c.center = sphere.center;
            c.radius = sphere.radius;
            c.isTrigger = sphere.isTrigger;
            c.material = sphere.material;
            return c;
        }
        if (source is CapsuleCollider capsule)
        {
            var c = target.AddComponent<CapsuleCollider>();
            c.center = capsule.center;
            c.radius = capsule.radius;
            c.height = capsule.height;
            c.direction = capsule.direction;
            c.isTrigger = capsule.isTrigger;
            c.material = capsule.material;
            return c;
        }
        if (source is MeshCollider mesh)
        {
            var c = target.AddComponent<MeshCollider>();
            c.sharedMesh = mesh.sharedMesh;
            c.convex = mesh.convex;
            c.isTrigger = mesh.isTrigger;
            c.material = mesh.material;
            return c;
        }

        Debug.LogWarning($"TransformApplier: unsupported collider type '{source.GetType().Name}' " +
                         $"on '{source.gameObject.name}'. Skipped.");
        return null;
    }

    private static Collider CopyColliderFromChild(GameObject target, Collider source, Transform sourceRoot)
    {
        Vector3 localOffset = sourceRoot.InverseTransformPoint(source.transform.position);

        if (source is BoxCollider box)
        {
            var c = target.AddComponent<BoxCollider>();
            c.center = box.center + localOffset;
            c.size = box.size;
            c.isTrigger = box.isTrigger;
            c.material = box.material;
            return c;
        }
        if (source is SphereCollider sphere)
        {
            var c = target.AddComponent<SphereCollider>();
            c.center = sphere.center + localOffset;
            c.radius = sphere.radius;
            c.isTrigger = sphere.isTrigger;
            c.material = sphere.material;
            return c;
        }
        if (source is CapsuleCollider capsule)
        {
            var c = target.AddComponent<CapsuleCollider>();
            c.center = capsule.center + localOffset;
            c.radius = capsule.radius;
            c.height = capsule.height;
            c.direction = capsule.direction;
            c.isTrigger = capsule.isTrigger;
            c.material = capsule.material;
            return c;
        }
        if (source is MeshCollider mesh)
        {
            var c = target.AddComponent<MeshCollider>();
            c.sharedMesh = mesh.sharedMesh;
            c.convex = mesh.convex;
            c.isTrigger = mesh.isTrigger;
            c.material = mesh.material;
            return c;
        }

        return null;
    }
}