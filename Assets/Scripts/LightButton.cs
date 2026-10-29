using UnityEngine;
using UnityEngine.InputSystem;

public class LightButton : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private Transform windowLights;
    [SerializeField] private Transform button;
    [SerializeField, TextArea(2, 4)] private string[] pages;
    [SerializeField] private bool onlyOnce = true;

    private float interactDistance = 2.5f;
    private Color glowColor = new Color(1f, 0.62f, 0.35f);
    private float onGlow = 2f;
    private float offGlow = 0.15f;
    private float pressDepth = 0.012f;
    private float pressTime = 0.2f;

    private Transform view;
    private Renderer buttonRenderer;
    private MaterialPropertyBlock glow;
    private InputAction pressAction;
    private GUIStyle promptStyle;
    private Vector3 restPosition;
    private bool aiming;
    private bool lightsOn;
    private bool told;
    private float pressTimer;

    private void Awake()
    {
        view = FindAnyObjectByType<FirstPersonController>().GetComponentInChildren<Camera>().transform;
        pressAction = InputSystem.actions.FindAction(
            "Player/Attack",
            throwIfNotFound: true
        );
        buttonRenderer = button.GetComponent<Renderer>();
        restPosition = button.localPosition;
        glow = new MaterialPropertyBlock();
        lightsOn = windowLights.gameObject.activeSelf;
        Apply();
    }

    private void Update()
    {
        aiming = !TextBox.IsOpen
            && Physics.Raycast(view.position, view.forward, out RaycastHit hit, interactDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.transform.IsChildOf(transform);

        if (aiming && pressAction.WasPressedThisFrame())
        {
            lightsOn = !lightsOn;
            pressTimer = pressTime;
            Apply();

            if (!lightsOn && !(onlyOnce && told))
            {
                told = true;
                TextBox.Show(pages);
            }
        }

        if (pressTimer > 0f)
        {
            pressTimer = Mathf.Max(0f, pressTimer - Time.deltaTime);
            float depth = pressDepth * Mathf.Sin(pressTimer / pressTime * Mathf.PI);
            button.localPosition = restPosition + Vector3.back * depth;
        }
    }

    private void OnGUI()
    {
        if (!aiming)
            return;

        promptStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        promptStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.03f);
        Rect area = new Rect(0f, Screen.height * 0.65f, Screen.width, Screen.height * 0.2f);
        string action = lightsOn ? "off" : "on";
        GUI.Label(area, $"<b>Click</b> to turn {action} the lights", promptStyle);
    }

    private void Apply()
    {
        windowLights.gameObject.SetActive(lightsOn);
        glow.SetColor(EmissionColorId, glowColor * (lightsOn ? onGlow : offGlow));
        buttonRenderer.SetPropertyBlock(glow);
    }
}
