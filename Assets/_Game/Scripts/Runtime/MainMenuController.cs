using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyFutureCity
{
    /// <summary>Main menu, character selection and settings, all built from code.</summary>
    public class MainMenuController : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform page;

        private void Start()
        {
            var ctx = GameContext.Instance;
            Application.targetFrameRate = ctx.Save.settings.targetFps;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Time.timeScale = 1f;
            GameInput.Reset();
            UIKit.EnsureEventSystem();
            canvas = UIKit.CreateCanvas("MenuCanvas", 0);
            ShowMain();
        }

        private void NewPage()
        {
            if (page != null) Destroy(page.gameObject);
            page = UIKit.NewRect("Page", canvas.transform);
            UIKit.Stretch(page, Vector2.zero, Vector2.zero);
        }

        private Button AddButton(string label, float y, UnityEngine.Events.UnityAction onClick, bool enabled)
        {
            var b = UIKit.MakeButton(page, label, 36, enabled ? new Color(0.16f, 0.5f, 0.8f, 0.95f) : new Color(0.4f, 0.4f, 0.4f, 0.6f), onClick);
            b.interactable = enabled;
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(460, 76));
            return b;
        }

        private void Title(string logical, int size, float y)
        {
            var t = UIKit.Label(page, logical, size, TextAnchor.MiddleCenter, Color.white, 0);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(1000, size + 30));
        }

        private void ShowMain()
        {
            NewPage();
            Title(Strings.GameTitle, 96, 230);
            Title("My Future City", 34, 160);
            var ctx = GameContext.Instance;
            AddButton(Strings.Continue, 60, LoadGame, ctx.HasProgress);
            AddButton(Strings.NewGame, -30, OnNewGame, true);
            AddButton(Strings.MyReport, -120, () => ReportScreen.Show(GameContext.Instance, null), ctx.HasProgress);
            AddButton(Strings.Settings, -210, ShowSettings, true);
            AddButton(Strings.Exit, -300, Application.Quit, true);
        }

        private void LoadGame()
        {
            SceneManager.LoadScene(SceneNames.Game);
        }

        private void OnNewGame()
        {
            if (!GameContext.Instance.HasProgress) { ShowCharacterSelect(); return; }
            NewPage();
            Title(Strings.ConfirmNewGame, 38, 120);
            AddButton(Strings.Yes, -20, ShowCharacterSelect, true);
            AddButton(Strings.No, -125, ShowMain, true);
        }

        private void ShowCharacterSelect()
        {
            NewPage();
            Title(Strings.ChooseCharacter, 56, 220);
            AddCard(Strings.Girl, new Color(0.9f, 0.35f, 0.6f), -230f, "girl");
            AddCard(Strings.Boy, new Color(0.2f, 0.45f, 0.85f), 230f, "boy");
            var back = UIKit.MakeButton(page, Strings.Back, 30, new Color(0.3f, 0.35f, 0.45f, 0.95f), ShowMain);
            UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -280), new Vector2(300, 70));
        }

        private void AddCard(string label, Color color, float x, string id)
        {
            var btn = UIKit.MakeButton(page, "", 30, new Color(1f, 1f, 1f, 0.18f), () =>
            {
                GameContext.StartNewGame(id);
                SceneManager.LoadScene(SceneNames.Game);
            });
            UIKit.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -10), new Vector2(380, 380));
            var circle = UIKit.Box(btn.transform, "Preview", color, false);
            circle.sprite = UIKit.CircleSprite;
            UIKit.Place(circle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(220, 220));
            var t = UIKit.Label(btn.transform, label, 52, TextAnchor.MiddleCenter, Color.white, 0);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(340, 70));
        }

        private void ShowSettings()
        {
            NewPage();
            var st = GameContext.Instance.Save.settings;
            Title(Strings.Settings, 64, 230);
            Title("حساسیت دوربین: " + st.cameraSensitivity.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), 38, 120);
            AddButton(Strings.SensitivityLess, 20, () => { st.cameraSensitivity = Mathf.Max(0.4f, st.cameraSensitivity - 0.2f); GameContext.Instance.SaveNow(); ShowSettings(); }, true);
            AddButton(Strings.SensitivityMore, -85, () => { st.cameraSensitivity = Mathf.Min(2.4f, st.cameraSensitivity + 0.2f); GameContext.Instance.SaveNow(); ShowSettings(); }, true);
            AddButton(st.targetFps == 30 ? Strings.Fps60 : Strings.Fps30, -190, () =>
            {
                st.targetFps = st.targetFps == 30 ? 60 : 30;
                Application.targetFrameRate = st.targetFps;
                GameContext.Instance.SaveNow();
                ShowSettings();
            }, true);
            AddButton(Strings.Back, -295, ShowMain, true);
        }
    }
}
