using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LightReactiveObject : MonoBehaviour
{
    [Header("Glow Settings")]
    public Color glowColor = Color.cyan;
    public float glowIntensity = 2f;
    public float fadeSpeed = 3f;

    [HideInInspector] public bool isActivated = false;
    [HideInInspector] public LightPuzzleManager puzzleManager;

    private Renderer rend;
    private Material mat;
    private Color baseEmission;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;

        if (mat.HasProperty("_EmissionColor"))
        {
            baseEmission = mat.GetColor("_EmissionColor");
            mat.EnableKeyword("_EMISSION");
        }
    }

    // Called when the flashlight light hits this object
    public void ActivateGlow()
    {
        if (isActivated) return;

        isActivated = true;
        StartCoroutine(GlowUp());
        puzzleManager?.RegisterGlow(this);
    }

    private System.Collections.IEnumerator GlowUp()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            Color emission = Color.Lerp(baseEmission, glowColor * glowIntensity, t);
            mat.SetColor("_EmissionColor", emission);
            yield return null;
        }
    }
}
