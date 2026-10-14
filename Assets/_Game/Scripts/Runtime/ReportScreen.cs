using System;
using MyFutureCity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MyFutureCity
{
    /// <summary>
    /// "گزارش من": a full-screen, child-friendly summary of which jobs the child chose, repeated and
    /// finished most. Works in the main menu and in the game (own canvas, unscaled time).
    /// </summary>
    public static class ReportScreen
    {
        private const float PanelW = 1100f, PanelH = 660f;
        private const float BarW = 400f, RowH = 46f;

        public static void Show(GameContext ctx, Action onClose)
        {
            var report = InterestAnalyzer.Analyze(ctx.Database, ctx.Save);
            var canvas = UIKit.CreateCanvas("ReportCanvas", 40);
            bool wasBlocked = GameInput.Blocked;
            float prevScale = Time.timeScale;
            GameInput.Blocked = true;
            Time.timeScale = 0f;          // the game stands still (and no play time is counted) while the report is open
            UIKit.EnsureEventSystem();

            var dim = UIKit.Box(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.7f), true);
            UIKit.Stretch(dim.rectTransform, Vector2.zero, Vector2.zero);

            var panel = UIKit.Box(dim.transform, "Panel", new Color(0.11f, 0.17f, 0.27f, 0.98f), true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelW, PanelH));

            var title = UIKit.Label(panel.transform, Strings.MyReport, 44, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), 0);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(PanelW - 80, 60));

            float y = -86f;
            if (!report.HasData)
            {
                var none = UIKit.Label(panel.transform, InterestAnalyzer.TextNoData, 30, TextAnchor.MiddleCenter, Color.white, 48);
                UIKit.Place(none.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(PanelW - 160, 200));
            }
            else
            {
                foreach (var job in report.Jobs)
                {
                    BuildRow(panel.transform, job, job.JobId == report.TopInterestJobId, y);
                    y -= RowH;
                }

                // Short sentences under the bars (most important first).
                var sb = new System.Text.StringBuilder();
                int shown = 0;
                foreach (var line in report.LinesFa)
                {
                    if (shown >= 5) break;
                    if (shown > 0) sb.Append('\n');
                    sb.Append(line);
                    shown++;
                }
                var lines = UIKit.Label(panel.transform, sb.ToString(), 22, TextAnchor.UpperRight, new Color(0.85f, 0.95f, 1f), 78);
                UIKit.Place(lines.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, y - 14f), new Vector2(PanelW - 80, 150));
            }

            var close = UIKit.MakeButton(panel.transform, Strings.Close, 32, new Color(0.18f, 0.6f, 0.4f), () =>
            {
                UnityEngine.Object.Destroy(canvas.gameObject);
                GameInput.Blocked = wasBlocked;
                Time.timeScale = prevScale;
                if (onClose != null) onClose();
            });
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(260, 64));
        }

        private static void BuildRow(Transform parent, JobInsight job, bool isTop, float y)
        {
            // job name (right side, Persian reading order)
            var name = UIKit.Label(parent, job.NameFa, 26, TextAnchor.MiddleRight, isTop ? new Color(1f, 0.9f, 0.4f) : Color.white, 0);
            UIKit.Place(name.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, y), new Vector2(250, RowH - 6));

            // bar track + fill
            var track = UIKit.Box(parent, "Track", new Color(1f, 1f, 1f, 0.12f), false);
            UIKit.Place(track.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-300, y - 8f), new Vector2(BarW, RowH - 22f));
            if (job.InterestShare > 0.001f)
            {
                var fill = UIKit.Box(track.transform, "Fill", isTop ? new Color(1f, 0.78f, 0.2f) : new Color(0.3f, 0.65f, 0.95f), false);
                UIKit.Place(fill.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero,
                    new Vector2(Mathf.Max(8f, BarW * job.InterestShare), RowH - 22f));
            }

            // numbers (left side)
            string stats;
            if (job.Attempts == 0) stats = Strings.ReportNotTried;
            else
            {
                stats = job.InterestPercent + "% \u00B7 " + InterestAnalyzer.FormatMinutes(job.Seconds)
                        + " \u00B7 " + job.StagesDone + "/" + job.StagesTotal;
                if (job.Replays > 0) stats += " \u00B7 " + job.Replays + " " + Strings.TagAgain;
                if (job.ActivitiesDone > 0) stats += " \u00B7 " + job.ActivitiesDone + " " + Strings.TagActivities;
            }
            var st = UIKit.Label(parent, stats, 20, TextAnchor.MiddleLeft, new Color(0.85f, 0.9f, 1f), 0);
            UIKit.Place(st.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(340, RowH - 6));
        }
    }
}
