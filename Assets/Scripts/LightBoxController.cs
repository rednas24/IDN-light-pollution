using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightBoxController : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static event Action LightsChanged;

    private enum Mode
    {
        Switch,
        Tune
    }

    private struct LightSetting
    {
        public string Name;
        public Color Color;
        public float Intensity;
    }

    [SerializeField] private Transform streetLights;
    [SerializeField] private Transform handle;
    [SerializeField] private Mode mode;
    [SerializeField, TextArea(2, 4)] private string[] pages;
    [SerializeField, TextArea(2, 4)] private string[] onPages;
    [SerializeField] private bool onlyOnce = true;

    private readonly LightSetting[] settings =
    {
        new LightSetting { Name = "Warm", Color = new Color(1f, 0.55f, 0.15f), Intensity = 100f },
        new LightSetting { Name = "White", Color = new Color(0.92f, 0.96f, 1f), Intensity = 250f },
    };
    private float interactDistance = 2.5f;
    private float glowIntensity = 1.5f;
    private float handleTravel = 0.25f;
    private float dragSensitivity = 0.001f;
    private float minTuneChange = 0.05f;

    private readonly List<Light> lights = new List<Light>();
    private readonly List<(Renderer, int)> lamps = new List<(Renderer, int)>();
    private MaterialPropertyBlock glow;
    private FirstPersonController player;
    private Transform view;
    private Renderer handleRenderer;
    private InputAction grabAction;
    private InputAction lookAction;
    private GUIStyle promptStyle;
    private string prompt;
    private bool aiming;
    private bool dragging;
    private bool told;
    private bool toldOn;
    private float level = 1f;
    private float grabbedLevel;

    private void Awake()
    {
        player = FindAnyObjectByType<FirstPersonController>();
        view = player.GetComponentInChildren<Camera>().transform;
        grabAction = InputSystem.actions.FindAction(
            "Player/Attack",
            throwIfNotFound: true
        );
        lookAction = InputSystem.actions.FindAction(
            "Player/Look",
            throwIfNotFound: true
        );
        handleRenderer = handle.GetComponent<Renderer>();

        streetLights.GetComponentsInChildren(true, lights);
        foreach (Renderer lamp in streetLights.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = lamp.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].IsKeywordEnabled("_EMISSION"))
                    lamps.Add((lamp, i));
            }
        }

        glow = new MaterialPropertyBlock();
        Apply();
    }

    private void OnEnable()
    {
        LightsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LightsChanged -= Refresh;
        if (dragging)
            SetDragging(false);
    }

    private void Update()
    {
        aiming = !TextBox.IsOpen
            && Physics.Raycast(view.position, view.forward, out RaycastHit hit, interactDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.transform.IsChildOf(transform);

        if (aiming && grabAction.WasPressedThisFrame())
        {
            grabbedLevel = level;
            SetDragging(true);
        }
        else if (dragging && (!aiming || !grabAction.IsPressed()))
        {
            SetDragging(false);
            Release();
        }

        if (!dragging)
            return;

        float delta = lookAction.ReadValue<Vector2>().y * dragSensitivity;
        if (delta != 0f)
        {
            level = Mathf.Clamp01(level + delta);
            Apply();
        }
    }

    private void OnGUI()
    {
        if (!aiming && !dragging)
            return;

        promptStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        promptStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.03f);
        Rect area = new Rect(0f, Screen.height * 0.65f, Screen.width, Screen.height * 0.2f);
        GUI.Label(area, prompt, promptStyle);
    }

    private void SetDragging(bool value)
    {
        dragging = value;
        player.CanLook = !value;
    }

    private void Release()
    {
        if (mode == Mode.Switch)
        {
            level = level > 0.5f ? 1f : 0f;
            Apply();
        }

        if (Mathf.Abs(level - grabbedLevel) < minTuneChange)
            return;

        bool turnedOn = mode == Mode.Switch && level > 0f;
        if (onlyOnce && (turnedOn ? toldOn : told))
            return;

        if (turnedOn)
            toldOn = true;
        else
            told = true;
        TextBox.Show(turnedOn ? onPages : pages);
    }

    private void Apply()
    {
        if (mode == Mode.Switch)
        {
            foreach (Light light in lights)
                light.enabled = level > 0.5f;
        }
        else
        {
            float position = level * (settings.Length - 1);
            int index = Mathf.Min((int)position, settings.Length - 2);
            float t = position - index;
            Color color = Color.Lerp(settings[index].Color, settings[index + 1].Color, t);
            float intensity = Mathf.Lerp(settings[index].Intensity, settings[index + 1].Intensity, t);

            foreach (Light light in lights)
            {
                light.color = color;
                light.intensity = intensity;
            }
        }

        Vector3 handlePosition = handle.localPosition;
        handlePosition.y = Mathf.Lerp(-handleTravel, handleTravel, level);
        handle.localPosition = handlePosition;

        prompt = BuildPrompt();
        LightsChanged?.Invoke();
    }

    private void Refresh()
    {
        if (lights.Count == 0)
            return;

        Color color = lights[0].color * glowIntensity;
        glow.SetColor(EmissionColorId, lights[0].enabled ? color : Color.black);
        foreach ((Renderer lamp, int material) in lamps)
            lamp.SetPropertyBlock(glow, material);

        if (mode == Mode.Tune)
            glow.SetColor(EmissionColorId, color);
        handleRenderer.SetPropertyBlock(glow);
    }

    private string BuildPrompt()
    {
        string[] labels = mode == Mode.Switch
            ? new[] { "Off", "On" }
            : Array.ConvertAll(settings, setting => setting.Name);
        string action = mode == Mode.Switch ? "turn the street lights on or off" : "tune the color of the street lights";

        int nearest = Mathf.RoundToInt(level * (labels.Length - 1));
        for (int i = 0; i < labels.Length; i++)
            labels[i] = i == nearest ? $"<b>{labels[i]}</b>" : $"<color=#ffffff59>{labels[i]}</color>";
        return $"<b>Drag</b> to {action}\n" + string.Join("      ", labels);
    }
}
