using UnityEngine;
using UnityEngine.UI;

public class ProximityGlowAuto : MonoBehaviour
{
    [Header("Setup")]
    public Transform target;
    public float detectionRadius = 10f;

    [Header("Glow")]
    public Color glowColor = Color.cyan;
    public float fadeSpeed = 4f;

    private Image screenBorder;
    private float currentAlpha = 0f;

    void Awake()
    {
        // Automatically find the border image inside this prefab
        screenBorder = GetComponentInChildren<Image>(true);

        if (screenBorder != null)
        {
            Color c = glowColor;
            c.a = 0f;
            screenBorder.color = c;
        }
        else
        {
            Debug.LogWarning($"{name}: No UI Image found inside prefab!");
        }
    }

    void Update()
    {
        if (target == null || screenBorder == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        bool isClose = distance <= detectionRadius;

        float targetAlpha = isClose ? 1f : 0f;

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

        Color c = glowColor;
        c.a = currentAlpha;
        screenBorder.color = c;
    }
}
