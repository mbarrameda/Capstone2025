using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [SerializeField] private Light pointLight;
    [SerializeField] private Light spotLight;

    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float maxOnTime = 5f;
    [SerializeField] private float maxOffTime = 0.5f;
    [SerializeField] private float minOnTime = 1f;
    [SerializeField] private float minOffTime = 0.1f;

    private float randomOnTime;
    private float randomOffTime;
    private bool isLightOn = true;
    private float currentTime = 0f;

    private void Start()
    {
        SetLightIntensity(maxIntensity);

        randomOffTime = maxOffTime;
        randomOnTime = maxOnTime;
    }

    private void Update()
    {
        currentTime += Time.deltaTime;
        SimpleFlicker();
    }

    private void SimpleFlicker()
    {
        if (isLightOn)
        {
            if (currentTime >= randomOnTime)
            {
                randomOffTime = Random.Range(minOffTime, maxOffTime);
                ToggleLight();
            }
        }
        else
        {
            if (currentTime >= randomOffTime)
            {
                randomOnTime = Random.Range(minOnTime, maxOnTime);
                ToggleLight();
            }
        }
    }

    private void ToggleLight()
    {
        currentTime = 0f;
        isLightOn = !isLightOn;

        SetLightIntensity(isLightOn ? maxIntensity : 0f);
    }

    private void SetLightIntensity(float value)
    {
        if (pointLight != null)
            pointLight.intensity = value;

        if (spotLight != null)
            spotLight.intensity = value;
    }
}
