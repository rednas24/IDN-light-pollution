using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TextBox : MonoBehaviour
{
    private static TextBox instance;

    private float lettersPerSecond = 40f;
    private int linesPerPage = 2;
    private Color fillColor = new Color(0.97f, 0.97f, 0.94f);
    private Color borderColor = new Color(0.18f, 0.21f, 0.29f);
    private Color frameColor = new Color(0.52f, 0.64f, 0.78f);
    private Color textColor = new Color(0.25f, 0.25f, 0.27f);
    private Color shadowColor = new Color(0.8f, 0.8f, 0.76f);
    private Color arrowColor = new Color(0.85f, 0.27f, 0.22f);

    private readonly Queue<string> texts = new Queue<string>();
    private readonly Queue<string> pages = new Queue<string>();
    private FirstPersonController player;
    private InputAction jumpAction;
    private InputAction interactAction;
    private GUIStyle textStyle;
    private GUIStyle arrowStyle;
    private string page;
    private float typed;

    public static void Show(params string[] texts)
    {
        if (instance == null)
            instance = new GameObject(nameof(TextBox)).AddComponent<TextBox>();

        foreach (string text in texts)
        {
            if (!string.IsNullOrWhiteSpace(text))
                instance.texts.Enqueue(text.Replace("\r", "").Trim());
        }

        if (instance.texts.Count > 0 && instance.player != null)
            instance.player.CanMove = false;
    }

    private void Awake()
    {
        player = FindAnyObjectByType<FirstPersonController>();
        jumpAction = InputSystem.actions.FindAction(
            "Player/Jump",
            throwIfNotFound: true
        );
        interactAction = InputSystem.actions.FindAction(
            "Player/Interact",
            throwIfNotFound: true
        );
    }

    private void Update()
    {
        if (page == null)
            return;

        bool pressed = jumpAction.WasPressedThisFrame() || interactAction.WasPressedThisFrame();
        if (typed < page.Length)
            typed = pressed ? page.Length : typed + lettersPerSecond * Time.unscaledDeltaTime;
        else if (pressed)
            NextPage();
    }

    private void OnGUI()
    {
        if (page == null && texts.Count == 0)
            return;

        float unit = Screen.height / 100f;
        textStyle ??= new GUIStyle(GUI.skin.label)
        {
            wordWrap = false,
            richText = false,
            clipping = TextClipping.Overflow,
            padding = new RectOffset()
        };
        textStyle.fontSize = Mathf.RoundToInt(unit * 3.6f);

        float width = Mathf.Min(Screen.width - unit * 6f, unit * 130f);
        float height = textStyle.lineHeight * linesPerPage + unit * 6f;
        Rect box = new Rect((Screen.width - width) / 2f, Screen.height - height - unit * 4f, width, height);
        Rect area = new Rect(box.x + unit * 4.5f, box.y + unit * 3f, box.width - unit * 12f, box.height - unit * 6f);

        while (texts.Count > 0)
            Paginate(texts.Dequeue(), area.width);
        if (page == null)
            NextPage();

        float inset = unit * 1.2f;
        DrawPanel(box, fillColor, 0f, unit * 1.8f);
        DrawPanel(box, borderColor, unit * 0.6f, unit * 1.8f);
        DrawPanel(new Rect(box.x + inset, box.y + inset, box.width - inset * 2f, box.height - inset * 2f), frameColor, unit * 0.3f, unit * 0.8f);

        string shown = page.Substring(0, Mathf.Min((int)typed, page.Length));
        float shadow = unit * 0.3f;
        textStyle.normal.textColor = shadowColor;
        GUI.Label(new Rect(area.x + shadow, area.y + shadow, area.width, area.height), shown, textStyle);
        textStyle.normal.textColor = textColor;
        GUI.Label(area, shown, textStyle);

        if (typed < page.Length)
            return;

        arrowStyle ??= new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
        arrowStyle.fontSize = Mathf.RoundToInt(unit * 2.4f);
        arrowStyle.normal.textColor = arrowColor;
        float bounce = Mathf.PingPong(Time.unscaledTime * 2f, 1f) * unit * 0.6f;
        GUI.Label(new Rect(box.xMax - unit * 6f, box.yMax - unit * 5.5f + bounce, unit * 3f, unit * 3f), "\u25BC", arrowStyle);
    }

    private void NextPage()
    {
        typed = 0f;
        page = pages.Count > 0 ? pages.Dequeue() : null;
        if (page == null && texts.Count == 0 && player != null)
            player.CanMove = true;
    }

    private void Paginate(string text, float width)
    {
        List<string> lines = new List<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' '))
            {
                string longer = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && textStyle.CalcSize(new GUIContent(longer)).x > width)
                {
                    lines.Add(line);
                    line = word;
                }
                else
                {
                    line = longer;
                }
            }
            lines.Add(line);
        }

        for (int i = 0; i < lines.Count; i += linesPerPage)
            pages.Enqueue(string.Join("\n", lines.GetRange(i, Mathf.Min(linesPerPage, lines.Count - i))));
    }

    private static void DrawPanel(Rect rect, Color color, float border, float radius)
    {
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, border, radius);
    }
}
