using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ImageFadeAndLoad : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInDuration = 2f;
    public float holdDuration = 1f;
    public float fadeOutDuration = 2f;

    [Header("Scene Settings")]
    public string nextSceneName;

    private Image image;
    private Color imageColor;

    void Start()
    {
        image = GetComponent<Image>();
        imageColor = image.color;
        imageColor.a = 0f;
        image.color = imageColor;

        StartCoroutine(FadeSequence());
    }

    IEnumerator FadeSequence()
    {
        // Fade In
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade Out
        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));

        // Load Scene
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            imageColor.a = alpha;
            image.color = imageColor;
            yield return null;
        }

        imageColor.a = endAlpha;
        image.color = imageColor;
    }
}
