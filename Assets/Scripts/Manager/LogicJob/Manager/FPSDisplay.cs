using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    float deltaTime;

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        float fps = 1.0f / deltaTime;

        GUIStyle style = new GUIStyle();
        style.fontSize = 30;
        style.normal.textColor = Color.green;

        GUI.Label(
            new Rect(10, 10, 300, 50),
            $"FPS : {fps:F1}",
            style);
    }
}