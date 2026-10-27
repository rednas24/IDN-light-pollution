using System.Collections.Generic;
using UnityEngine;

public class LightPollutionController : MonoBehaviour
{
    private static readonly int PollutionId = Shader.PropertyToID("_LightPollution");

    [SerializeField] private Transform lightSources;
    private float fullPollutionPerLight = 250f;
    private AnimationCurve pollutionCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 2f, 2f)
    );
    private float smoothTime = 0.3f;

    private readonly List<Light> lights = new List<Light>();
    private Material sky;
    private float pollution;
    private float velocity;

    private void Awake()
    {
        sky = new Material(RenderSettings.skybox);
        RenderSettings.skybox = sky;
        pollution = MeasurePollution();
    }

    private void Update()
    {
        float target = MeasurePollution();
        pollution = Mathf.SmoothDamp(pollution, target, ref velocity, smoothTime);
        sky.SetFloat(PollutionId, pollution);
    }

    private void OnDestroy()
    {
        Destroy(sky);
    }

    private float MeasurePollution()
    {
        lightSources.GetComponentsInChildren(lights);
        if (lights.Count == 0)
            return 0f;

        float intensity = 0f;
        foreach (Light light in lights)
        {
            if (light.enabled)
                intensity += light.intensity;
        }
        float amount = Mathf.Clamp01(intensity / lights.Count / fullPollutionPerLight);
        return pollutionCurve.Evaluate(amount);
    }
}
