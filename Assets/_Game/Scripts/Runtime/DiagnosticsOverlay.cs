using UnityEngine;

namespace MyFutureCity
{
    /// <summary>
    /// Shows the most recent runtime error/exception on screen (red text at the top).
    /// Invisible during normal play. Helps diagnose blank/white screens on a real phone without adb.
    /// </summary>
    public class DiagnosticsOverlay : MonoBehaviour
    {
        private string lastError = "";
        private float shownUntil;
        private GUIStyle style;

        [RuntimeInitializeOnLoadMethod]
        private static void Boot()
        {
            var go = new GameObject("DiagnosticsOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<DiagnosticsOverlay>();
        }

        private void OnEnable() { Application.logMessageReceived += OnLog; }
        private void OnDisable() { Application.logMessageReceived -= OnLog; }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string text = condition ?? "";
            if (text.Length > 300) text = text.Substring(0, 300);
            lastError = text;
            shownUntil = Time.unscaledTime + 20f;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(lastError) || Time.unscaledTime > shownUntil) return;
            if (style == null)
            {
                style = new GUIStyle();
                style.fontSize = Mathf.Max(14, Screen.height / 36);
                style.wordWrap = true;
                style.normal.textColor = new Color(1f, 0.2f, 0.2f);
            }
            GUI.Label(new Rect(8f, 8f, Screen.width - 16f, Screen.height * 0.4f), "ERR: " + lastError, style);
        }
    }
}
