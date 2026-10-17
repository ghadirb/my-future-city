using System;
using MyFutureCity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MyFutureCity
{
    /// <summary>
    /// Addendum section 8: a plain, descriptive report for a parent or coach. Opened from the child's
    /// report (which already pauses the game), so it only adds its own canvas on top. Everything shown is
    /// built from the real play log by ParentReportBuilder; nothing is sent anywhere.
    /// </summary>
    public static class ParentReportScreen
    {
        private const float PanelW = 1100f, PanelH = 660f;
        private const int WrapChars = 58;

        public static void Show(GameContext ctx, Action onClose)
        {
            var report = ParentReportBuilder.Build(ctx.Database, ctx.Save);
            var canvas = UIKit.CreateCanvas("ParentReportCanvas", 41);
            UIKit.EnsureEventSystem();

            var dim = UIKit.Box(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.8f), true);
            UIKit.Stretch(dim.rectTransform, Vector2.zero, Vector2.zero);

            float screenH = ((RectTransform)canvas.transform).rect.height;
            float panelH = screenH > 200f ? Mathf.Min(PanelH, screenH - 20f) : PanelH;
            var panel = UIKit.Box(dim.transform, "Panel", new Color(0.12f, 0.16f, 0.24f, 0.99f), true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelW, panelH));

            var title = UIKit.Label(panel.transform, Strings.ParentReport, 40, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), 0);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(PanelW - 80, 56));

            var viewport = UIKit.Box(panel.transform, "Viewport", new Color(0f, 0f, 0f, 0f), true);
            UIKit.Stretch(viewport.rectTransform, new Vector2(0, 96), new Vector2(0, -80));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UIKit.NewRect("Content", viewport.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            float y = -8f;
            foreach (var section in report.Sections)
            {
                var head = UIKit.Label(content, section.TitleFa, 28, TextAnchor.MiddleRight, new Color(0.55f, 0.85f, 1f), 0);
                UIKit.Place(head.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, y), new Vector2(PanelW - 80, 40));
                y -= 44f;
                foreach (var line in section.Lines)
                {
                    float h = LineHeight(line);
                    var t = UIKit.Label(content, line, 24, TextAnchor.UpperRight, Color.white, WrapChars);
                    UIKit.Place(t.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-56, y), new Vector2(PanelW - 112, h));
                    y -= h + 6f;
                }
                y -= 14f;
            }
            float noteH = LineHeight(report.NoteFa);
            var note = UIKit.Label(content, report.NoteFa, 21, TextAnchor.UpperRight, new Color(1f, 0.9f, 0.6f), WrapChars + 8);
            UIKit.Place(note.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, y), new Vector2(PanelW - 80, noteH));
            y -= noteH + 24f;
            content.sizeDelta = new Vector2(0f, -y);

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var close = UIKit.MakeButton(panel.transform, Strings.Close, 32, new Color(0.18f, 0.6f, 0.4f), () =>
            {
                UnityEngine.Object.Destroy(canvas.gameObject);
                if (onClose != null) onClose();
            });
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(260, 64));
        }

        // Persian wrapping is approximate, so give every line room for one more row than the estimate.
        private static float LineHeight(string text)
        {
            int rows = (text == null ? 0 : text.Length) / WrapChars + 2;
            return rows * 32f;
        }
    }
}
