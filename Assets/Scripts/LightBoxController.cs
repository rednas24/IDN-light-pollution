using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightBoxController : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private struct LightSetting
    {
        public string Name;
        public Color Color;
        public float Intensity;
    }

    [SerializeField] private Transform streetLights;
    [SerializeField] private Transform handle;

    private readonly LightSetting[] settings =
    {
        new LightSetting { Name = "Off", Color = Color.black, Intensity = 0f },
        new LightSetting { Name = "Warm", Color = new Color(1f, 0.55f, 0.15f), Intensity = 100f },
        new LightSetting { Name = "White", Color = new Color(0.92f, 0.96f, 1f), Intensity = 250f },
    };
    private float interactDistance = 2.5f;
    private float glowIntensity = 1.5f;
    private float handleTravel = 0.25f;
    private float dragSensitivity = 0.001f;

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
    private float level = 1f;

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

    private void OnDisable()
    {
        if (dragging)
            SetDragging(false);
    }

    private void Update()
    {
        aiming = Physics.Raycast(view.position, view.forward, out RaycastHit hit, interactDistance)
            && hit.collider.transform.IsChildOf(transform);

        if (aiming && grabAction.WasPressedThisFrame())
            SetDragging(true);
        else if (dragging && (!aiming || !grabAction.IsPressed()))
            SetDragging(false);

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

    private void Apply()
    {
        float position = level * (settings.Length - 1);
        int index = Mathf.Min((int)position, settings.Length - 2);
        float t = position - index;
        Color color = Color.Lerp(settings[index].Color, settings[index + 1].Color, t);
        float intensity = Mathf.Lerp(settings[index].Intensity, settings[index + 1].Intensity, t);

        foreach (Light light in lights)
        {
            light.enabled = intensity > 0f;
            light.color = color;
            light.intensity = intensity;
        }

        glow.SetColor(EmissionColorId, color * glowIntensity);
        foreach ((Renderer lamp, int material) in lamps)
            lamp.SetPropertyBlock(glow, material);
        handleRenderer.SetPropertyBlock(glow);

        Vector3 handlePosition = handle.localPosition;
        handlePosition.y = Mathf.Lerp(-handleTravel, handleTravel, level);
        handle.localPosition = handlePosition;

        prompt = BuildPrompt(Mathf.RoundToInt(position));
    }

    private string BuildPrompt(int nearest)
    {
        string[] labels = new string[settings.Length];
        for (int i = 0; i < settings.Length; i++)
        {
            string label = settings[i].Name;
            labels[i] = i == nearest ? $"<b>{label}</b>" : $"<color=#ffffff59>{label}</color>";
        }
        return "<b>Drag</b> to change the street lights\n" + string.Join("      ", labels);
    }
}
