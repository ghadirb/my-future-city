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
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(460, 70));
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
            Title(Strings.GameTitle, 92, 215);
            Title("My Future City", 32, 145);
            var ctx = GameContext.Instance;
            AddButton(Strings.Continue, 62, LoadGame, ctx.HasProgress);
            AddButton(Strings.NewGame, -16, OnNewGame, true);
            AddButton(Strings.MyReport, -94, () => ReportScreen.Show(GameContext.Instance, null), ctx.HasProgress);
            AddButton(Strings.Settings, -172, ShowSettings, true);
            AddButton(Strings.Exit, -250, Application.Quit, true);
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
            AddButton(Strings.Yes, -10, ShowCharacterSelect, true);
            AddButton(Strings.No, -95, ShowMain, true);
        }

        // ---------- character selection (all drawn from code: no image assets needed) ----------

        private void ShowCharacterSelect()
        {
            NewPage();
            BuildSelectBackdrop();

            var title = UIKit.Label(page, Strings.ChooseCharacter, 58, TextAnchor.MiddleCenter, Color.white, 0);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 262), new Vector2(1000, 80));
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.25f, 0.5f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -4f);
            var sub = UIKit.Label(page, "با چه شخصیتی شهرت را آباد می‌کنی؟", 28, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.92f), 0);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 205), new Vector2(1000, 44));

            AddCard(Strings.Girl, new Color(0.93f, 0.36f, 0.62f), -215f, "girl", 0f);
            AddCard(Strings.Boy, new Color(0.22f, 0.5f, 0.92f), 215f, "boy", 1.7f);

            var back = UIKit.MakeButton(page, Strings.Back, 30, new Color(0.2f, 0.3f, 0.5f, 0.95f), ShowMain);
            UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -275), new Vector2(260, 60));
        }

        /// <summary>Sky gradient, sun, clouds, a skyline and rolling hills.</summary>
        private void BuildSelectBackdrop()
        {
            var sky = UIKit.Box(page, "Sky", Color.white, false);
            sky.sprite = UIKit.VerticalGradient(new Color(0.33f, 0.64f, 0.96f), new Color(0.82f, 0.93f, 1f));
            UIKit.Stretch(sky.rectTransform, Vector2.zero, Vector2.zero);

            UIKit.Disc(page, "SunGlow", new Color(1f, 0.95f, 0.6f, 0.30f), new Vector2(-470, 225), new Vector2(300, 300));
            UIKit.Disc(page, "Sun", new Color(1f, 0.87f, 0.3f), new Vector2(-470, 225), new Vector2(140, 140));

            Cloud(-210, 255, 1.0f);
            Cloud(330, 215, 0.8f);
            Cloud(520, 285, 0.6f);

            // skyline: soft pastel towers standing behind the hills
            int[] heights = { 150, 220, 120, 260, 180, 130, 240, 170, 110, 210, 150, 250, 140, 190, 120 };
            Color[] tints =
            {
                new Color(0.55f, 0.7f, 0.92f, 0.85f), new Color(0.96f, 0.66f, 0.76f, 0.85f),
                new Color(0.98f, 0.82f, 0.45f, 0.85f), new Color(0.58f, 0.85f, 0.75f, 0.85f)
            };
            for (int i = 0; i < heights.Length; i++)
            {
                float x = -700f + i * 100f;
                var tower = UIKit.Rounded(page, "Tower" + i, tints[i % tints.Length], false);
                UIKit.Place(tower.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 70f), new Vector2(84f, heights[i]));
                int rows = Mathf.Max(1, heights[i] / 60);
                for (int r = 0; r < rows; r++)
                {
                    var win = UIKit.Box(tower.transform, "Win", new Color(1f, 0.95f, 0.65f, 0.9f), false);
                    UIKit.Place(win.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f - r * 40f), new Vector2(38f, 18f));
                }
            }

            var far = UIKit.Disc(page, "HillFar", new Color(0.42f, 0.78f, 0.42f), Vector2.zero, new Vector2(1500, 420));
            UIKit.Place(far.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-330, 10), new Vector2(1500, 420));
            var near = UIKit.Disc(page, "HillNear", new Color(0.3f, 0.68f, 0.34f), Vector2.zero, new Vector2(1400, 380));
            UIKit.Place(near.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(430, -20), new Vector2(1400, 380));
        }

        private void Cloud(float x, float y, float scale)
        {
            var white = new Color(1f, 1f, 1f, 0.88f);
            var root = UIKit.NewRect("Cloud", page);
            UIKit.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(10, 10));
            root.localScale = Vector3.one * scale;
            UIKit.Disc(root, "A", white, new Vector2(-48, 0), new Vector2(82, 70));
            UIKit.Disc(root, "B", white, new Vector2(0, 18), new Vector2(104, 92));
            UIKit.Disc(root, "C", white, new Vector2(52, 0), new Vector2(86, 72));
            UIKit.Block(root, "Base", white, new Vector2(2, -14), new Vector2(170, 44));
        }

        private void AddCard(string label, Color color, float x, string id, float phase)
        {
            // The whole card is the button; everything drawn inside ignores raycasts so any tap on it counts.
            var btn = UIKit.MakeButton(page, "", 30, new Color(1f, 1f, 1f, 0.92f), () =>
            {
                GameContext.StartNewGame(id);
                SceneManager.LoadScene(SceneNames.Game);
            });
            var cardRt = (RectTransform)btn.transform;
            UIKit.Place(cardRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -22), new Vector2(340, 390));
            var colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.92f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.92f, 1f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            // soft colour tint at the top of the card
            var band = UIKit.Rounded(cardRt, "Band", new Color(color.r, color.g, color.b, 0.16f), false);
            UIKit.Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(316, 250));

            var avatar = UIKit.NewRect("Avatar", cardRt);
            UIKit.Place(avatar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 38), new Vector2(200, 260));
            BuildAvatar(avatar, id == "girl", color);
            var bob = avatar.gameObject.AddComponent<UIBob>();
            bob.amplitude = 6f; bob.speed = 2.1f; bob.phase = phase;

            var pill = UIKit.Rounded(cardRt, "NamePill", color, false);
            UIKit.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(230, 66));
            var t = UIKit.Label(pill.transform, label, 44, TextAnchor.MiddleCenter, Color.white, 0);
            UIKit.Stretch(t.rectTransform, Vector2.zero, Vector2.zero);
        }

        /// <summary>A friendly cartoon child built from rounded shapes (centre of the 200x260 area is 0,0).</summary>
        private static void BuildAvatar(RectTransform root, bool girl, Color accent)
        {
            var skin = new Color(0.99f, 0.8f, 0.66f);
            var hair = girl ? new Color(0.33f, 0.18f, 0.12f) : new Color(0.2f, 0.13f, 0.1f);
            var shirt = accent;
            var shirtDark = new Color(accent.r * 0.8f, accent.g * 0.8f, accent.b * 0.8f);
            var legs = girl ? new Color(0.45f, 0.32f, 0.62f) : new Color(0.2f, 0.27f, 0.45f);
            var shoe = new Color(0.22f, 0.2f, 0.26f);
            var ink = new Color(0.16f, 0.1f, 0.12f);

            UIKit.Disc(root, "Shadow", new Color(0f, 0f, 0f, 0.16f), new Vector2(0, -122), new Vector2(150, 24));

            if (girl) UIKit.Block(root, "HairBack", hair, new Vector2(0, 74), new Vector2(120, 136));

            UIKit.Block(root, "LegL", legs, new Vector2(-21, -88), new Vector2(32, 66));
            UIKit.Block(root, "LegR", legs, new Vector2(21, -88), new Vector2(32, 66));
            UIKit.Block(root, "ShoeL", shoe, new Vector2(-24, -120), new Vector2(42, 18));
            UIKit.Block(root, "ShoeR", shoe, new Vector2(24, -120), new Vector2(42, 18));

            UIKit.Block(root, "ArmL", shirt, new Vector2(-64, -20), new Vector2(26, 88), 9f);
            UIKit.Block(root, "ArmR", shirt, new Vector2(64, -20), new Vector2(26, 88), -9f);
            UIKit.Disc(root, "HandL", skin, new Vector2(-70, -68), new Vector2(26, 26));
            UIKit.Disc(root, "HandR", skin, new Vector2(70, -68), new Vector2(26, 26));

            UIKit.Block(root, "Body", shirt, new Vector2(0, -22), new Vector2(100, 98));
            if (girl) UIKit.Block(root, "Skirt", shirtDark, new Vector2(0, -64), new Vector2(124, 44));
            else UIKit.Block(root, "Belt", shirtDark, new Vector2(0, -62), new Vector2(100, 12));

            UIKit.Block(root, "Neck", skin, new Vector2(0, 28), new Vector2(30, 26));
            UIKit.Disc(root, "Head", skin, new Vector2(0, 72), new Vector2(106, 106));

            if (girl)
            {
                UIKit.Block(root, "Fringe", hair, new Vector2(0, 108), new Vector2(108, 40));
                UIKit.Disc(root, "TailL", hair, new Vector2(-62, 82), new Vector2(34, 34));
                UIKit.Disc(root, "TailR", hair, new Vector2(62, 82), new Vector2(34, 34));
                UIKit.Disc(root, "BowL", new Color(1f, 0.85f, 0.25f), new Vector2(-56, 106), new Vector2(22, 22));
                UIKit.Disc(root, "BowR", new Color(1f, 0.85f, 0.25f), new Vector2(56, 106), new Vector2(22, 22));
            }
            else
            {
                UIKit.Block(root, "Hair", hair, new Vector2(0, 110), new Vector2(110, 46));
                UIKit.Block(root, "Tuft", hair, new Vector2(-30, 124), new Vector2(34, 22), 12f);
            }

            UIKit.Disc(root, "EyeL", ink, new Vector2(-21, 70), new Vector2(13, 17));
            UIKit.Disc(root, "EyeR", ink, new Vector2(21, 70), new Vector2(13, 17));
            UIKit.Disc(root, "GlintL", Color.white, new Vector2(-18, 75), new Vector2(5, 5));
            UIKit.Disc(root, "GlintR", Color.white, new Vector2(24, 75), new Vector2(5, 5));
            UIKit.Disc(root, "CheekL", new Color(1f, 0.55f, 0.6f, 0.5f), new Vector2(-34, 54), new Vector2(20, 14));
            UIKit.Disc(root, "CheekR", new Color(1f, 0.55f, 0.6f, 0.5f), new Vector2(34, 54), new Vector2(20, 14));
            UIKit.Block(root, "Smile", new Color(0.82f, 0.3f, 0.36f), new Vector2(0, 50), new Vector2(24, 8));
        }

        private void ShowSettings()
        {
            NewPage();
            var st = GameContext.Instance.Save.settings;
            Title(Strings.Settings, 60, 225);
            Title("حساسیت دوربین: " + st.cameraSensitivity.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), 36, 140);
            AddButton(Strings.SensitivityLess, 60, () => { st.cameraSensitivity = Mathf.Max(0.4f, st.cameraSensitivity - 0.2f); GameContext.Instance.SaveNow(); ShowSettings(); }, true);
            AddButton(Strings.SensitivityMore, -20, () => { st.cameraSensitivity = Mathf.Min(2.4f, st.cameraSensitivity + 0.2f); GameContext.Instance.SaveNow(); ShowSettings(); }, true);
            AddButton(st.targetFps == 30 ? Strings.Fps60 : Strings.Fps30, -100, () =>
            {
                st.targetFps = st.targetFps == 30 ? 60 : 30;
                Application.targetFrameRate = st.targetFps;
                GameContext.Instance.SaveNow();
                ShowSettings();
            }, true);
            AddButton(Strings.Back, -180, ShowMain, true);
        }
    }
}
