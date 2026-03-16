using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpScareUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float fadeOutSpeed = 0.5f;

    public void Trigger()
    {
        canvasGroup.alpha = 1;
        audioSource.Play();
    }

    private void Update()
    {
        if (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeOutSpeed;
        }
    }
}
