using System;
using System.Collections;
using MyFutureCity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MyFutureCity
{
    public class DialogOption
    {
        public string Label;
        public Action OnClick;
        public DialogOption(string label, Action onClick) { Label = label; OnClick = onClick; }
    }

    /// <summary>In-game overlay: coins, mission tracker, joystick, interact button, toasts and dialogs.</summary>
    public class HudController : MonoBehaviour
    {
        private GameSceneController scene;
        private Text coinsText, levelText, missionTitle, missionObjective, promptText, toastText;
        private GameObject interactButton, dialogRoot;
        private Text dialogTitle, dialogBody;
        private RectTransform dialogButtons;
        private CanvasGroup toastGroup;
        private Coroutine toastRoutine;
        private string lastPrompt = "\u0001";

        public bool IsModalOpen { get { return dialogRoot != null && dialogRoot.activeSelf; } }

        public static HudController Create(GameSceneController owner)
        {
            var canvas = UIKit.CreateCanvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<HudController>();
            hud.scene = owner;
            hud.Build(canvas.transform);
            return hud;
        }

        private void Build(Transform root)
        {
            var white = Color.white;
            var panelColor = new Color(0f, 0f, 0f, 0.35f);

            // Coins + level (top right)
            var top = UIKit.Box(root, "StatusPanel", panelColor, false);
            UIKit.Place(top.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(330, 84));
            coinsText = UIKit.Label(top.transform, "", 30, TextAnchor.MiddleRight, new Color(1f, 0.9f, 0.4f), 0);
            UIKit.Place(coinsText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -6), new Vector2(300, 38));
            levelText = UIKit.Label(top.transform, "", 24, TextAnchor.MiddleRight, white, 0);
            UIKit.Place(levelText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 6), new Vector2(300, 34));

            // Mission tracker
            var mp = UIKit.Box(root, "MissionPanel", panelColor, false);
            UIKit.Place(mp.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -112), new Vector2(460, 118));
            missionTitle = UIKit.Label(mp.transform, "", 28, TextAnchor.UpperRight, new Color(0.6f, 0.95f, 1f), 0);
            UIKit.Place(missionTitle.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -8), new Vector2(430, 36));
            missionObjective = UIKit.Label(mp.transform, "", 22, TextAnchor.UpperRight, white, 36);
            UIKit.Place(missionObjective.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -48), new Vector2(430, 66));

            // Pause button (top left)
            var pause = UIKit.MakeButton(root, Strings.Menu, 30, new Color(0.15f, 0.2f, 0.3f, 0.85f), () => scene.OpenPauseMenu());
            UIKit.Place((RectTransform)pause.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(120, 70));

            // Joystick (bottom left)
            var baseImg = UIKit.Box(root, "JoystickBase", new Color(1f, 1f, 1f, 0.28f), true);
            baseImg.sprite = UIKit.CircleSprite;
            UIKit.Place(baseImg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(50, 50), new Vector2(200, 200));
            var knobImg = UIKit.Box(baseImg.transform, "Knob", new Color(1f, 1f, 1f, 0.7f), false);
            knobImg.sprite = UIKit.CircleSprite;
            UIKit.Place(knobImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
            var joy = baseImg.gameObject.AddComponent<JoystickView>();
            joy.baseRect = baseImg.rectTransform;
            joy.knob = knobImg.rectTransform;
            joy.radius = 80f;

            // Interact button (bottom right) + prompt
            var ib = UIKit.MakeButton(root, Strings.Interact, 30, new Color(0.15f, 0.65f, 0.35f, 0.92f), () => GameInput.RequestInteract());
            ib.GetComponent<Image>().sprite = UIKit.CircleSprite;
            UIKit.Place((RectTransform)ib.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 60), new Vector2(160, 160));
            interactButton = ib.gameObject;
            interactButton.SetActive(false);
            promptText = UIKit.Label(root, "", 28, TextAnchor.LowerRight, white, 0);
            UIKit.Place(promptText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 235), new Vector2(560, 44));

            // Toast
            var toastBox = UIKit.Box(root, "Toast", new Color(0f, 0f, 0f, 0.6f), false);
            UIKit.Place(toastBox.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(760, 64));
            toastGroup = toastBox.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastText = UIKit.Label(toastBox.transform, "", 28, TextAnchor.MiddleCenter, white, 0);
            UIKit.Stretch(toastText.rectTransform, new Vector2(10, 0), new Vector2(-10, 0));
            toastText.resizeTextForBestFit = true;
            toastText.resizeTextMinSize = 14;
            toastText.resizeTextMaxSize = 28;

            BuildDialog(root);
        }

        private void BuildDialog(Transform root)
        {
            var dim = UIKit.Box(root, "Dialog", new Color(0f, 0f, 0f, 0.55f), true);
            UIKit.Stretch(dim.rectTransform, Vector2.zero, Vector2.zero);
            dialogRoot = dim.gameObject;

            var panel = UIKit.Box(dim.transform, "Panel", new Color(0.11f, 0.17f, 0.27f, 0.97f), true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 560));

            dialogTitle = UIKit.Label(panel.transform, "", 42, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), 0);
            UIKit.Place(dialogTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(920, 64));

            dialogBody = UIKit.Label(panel.transform, "", 28, TextAnchor.UpperRight, Color.white, 0);
            UIKit.Stretch(dialogBody.rectTransform, new Vector2(50, 130), new Vector2(-50, -100));

            var row = UIKit.NewRect("Buttons", panel.transform);
            UIKit.Place(row, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(920, 80));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.reverseArrangement = true;     // RTL: first option on the right
            dialogButtons = row;
            dialogRoot.SetActive(false);
        }

        private void Update()
        {
            if (scene == null || scene.Interactor == null) return;
            var cur = scene.Interactor.Current;
            bool show = !IsModalOpen && cur != null;
            if (interactButton.activeSelf != show) interactButton.SetActive(show);
            string p = show ? cur.GetPrompt(scene.Ctx) : "";
            if (p != lastPrompt)
            {
                lastPrompt = p;
                UIKit.SetText(promptText, p, 0);
            }
        }

        public void Refresh()
        {
            var ctx = scene.Ctx;
            UIKit.SetText(coinsText, ctx.Wallet.Coins + " " + Strings.Coins, 0);
            UIKit.SetText(levelText, Strings.Level + " " + ctx.Wallet.Level, 0);

            var active = ctx.Missions.ActiveMission;
            if (active == null)
            {
                UIKit.SetText(missionTitle, Strings.NoActiveMission, 0);
                UIKit.SetText(missionObjective, Strings.TalkToPeople, 36);
                return;
            }
            UIKit.SetText(missionTitle, active.nameFa, 0);
            int idx = ctx.Missions.CurrentObjectiveIndex;
            if (idx < 0) { UIKit.SetText(missionObjective, "", 0); return; }
            var obj = active.objectives[idx];
            string line = obj.textFa;
            if (obj.RequiredCount > 1)
                line += " (" + ctx.Missions.GetObjectiveCount(active.id, idx) + " از " + obj.RequiredCount + ")";
            UIKit.SetText(missionObjective, line, 36);
        }

        public void Toast(string logical)
        {
            UIKit.SetText(toastText, logical, 0);
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            toastGroup.alpha = 1f;
            float t = 0f;
            while (t < 2.4f) { t += Time.unscaledDeltaTime; yield return null; }
            while (toastGroup.alpha > 0f)
            {
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, Time.unscaledDeltaTime * 2.5f);
                yield return null;
            }
            toastRoutine = null;
        }

        public void ShowDialog(string title, string body, params DialogOption[] options)
        {
            UIKit.SetText(dialogTitle, title, 0);
            UIKit.SetText(dialogBody, body, 62);

            for (int i = dialogButtons.childCount - 1; i >= 0; i--)
                Destroy(dialogButtons.GetChild(i).gameObject);

            if (options == null || options.Length == 0)
                options = new[] { new DialogOption(Strings.GotIt, null) };

            for (int i = 0; i < options.Length; i++)
            {
                var opt = options[i];
                var btn = UIKit.MakeButton(dialogButtons, opt.Label, 28,
                    i == 0 ? new Color(0.18f, 0.6f, 0.4f) : new Color(0.3f, 0.4f, 0.55f), () =>
                    {
                        CloseDialog();
                        if (opt.OnClick != null) opt.OnClick();
                    });
                btn.name = "Option" + i;
            }

            dialogRoot.SetActive(true);
            GameInput.Blocked = true;
            GameInput.Move = Vector2.zero;
            GameInput.ConsumeInteract();
        }

        public void CloseDialog()
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
            GameInput.Blocked = false;
            Time.timeScale = 1f;
        }
    }
}
