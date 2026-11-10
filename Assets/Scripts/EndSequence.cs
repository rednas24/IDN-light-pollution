using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EndSequence : MonoBehaviour
{
    [SerializeField, TextArea(2, 4)] private string[] pages;

    private string prompt = "Click to end";
    private float fadeTime = 3f;
    private float textDelay = 1f;

    private FirstPersonController player;
    private GUIStyle promptStyle;
    private bool showPrompt;
    private float fade;

    public void Begin()
    {
        StartCoroutine(Run());
    }

    private void Awake()
    {
        player = FindAnyObjectByType<FirstPersonController>();
    }

    private IEnumerator Run()
    {
        showPrompt = true;
        yield return new WaitUntil(() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        showPrompt = false;

        player.CanMove = false;
        player.CanLook = false;

        for (float time = 0f; time < fadeTime; time += Time.deltaTime)
        {
            fade = time / fadeTime;
            yield return null;
        }
        fade = 1f;

        yield return new WaitForSeconds(textDelay);
        TextBox.Show(pages);
        yield return new WaitWhile(() => TextBox.IsOpen);
        SceneManager.LoadScene("MainMenu1");
    }

    private void OnGUI()
    {
        GUI.depth = 10;

        if (fade > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, fade);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (!showPrompt)
            return;

        float unit = Screen.height / 100f;
        promptStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        promptStyle.fontSize = Mathf.RoundToInt(unit * 3f);
        promptStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f + 0.6f * Mathf.PingPong(Time.time, 1f));
        GUI.Label(new Rect(0f, Screen.height - unit * 12f, Screen.width, unit * 6f), prompt, promptStyle);
    }
}