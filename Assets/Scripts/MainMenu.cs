using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string title = "Light Pollution";
    private string prompt = "Click to start experience";
    private string gameScene = "Environment";
    private float fadeTime = 1.5f;

    private GUIStyle titleStyle;
    private GUIStyle promptStyle;
    private bool starting;
    private float fade;

    private void Update()
    {
        if (!starting && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            StartCoroutine(StartGame());
    }

    private IEnumerator StartGame()
    {
        starting = true;
        for (float time = 0f; time < fadeTime; time += Time.deltaTime)
        {
            fade = time / fadeTime;
            yield return null;
        }
        fade = 1f;
        SceneManager.LoadScene(gameScene);
    }

    private void OnGUI()
    {
        float unit = Screen.height / 100f;

        if (title.Length > 0)
        {
            titleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            titleStyle.fontSize = Mathf.RoundToInt(unit * 8f);
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0f, unit * 30f, Screen.width, unit * 12f), title, titleStyle);
        }

        promptStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        promptStyle.fontSize = Mathf.RoundToInt(unit * 3f);
        promptStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f + 0.6f * Mathf.PingPong(Time.time, 1f));
        GUI.Label(new Rect(0f, unit * 60f, Screen.width, unit * 6f), prompt, promptStyle);

        if (fade > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, fade);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}