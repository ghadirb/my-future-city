using System.Text;
using MyFutureCity.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyFutureCity
{
    /// <summary>Entry point of the gameplay scene: wires services, HUD and global feedback.</summary>
    public class GameSceneController : MonoBehaviour
    {
        public PlayerController player;
        public Interactor interactor;
        public CharacterAppearance appearance;

        public GameContext Ctx { get; private set; }
        public HudController Hud { get; private set; }
        public Interactor Interactor { get { return interactor; } }

        private void Awake()
        {
            Ctx = GameContext.Instance;
            Application.targetFrameRate = Ctx.Save.settings.targetFps;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Time.timeScale = 1f;
            GameInput.Reset();
            UIKit.EnsureEventSystem();
            if (interactor != null) interactor.scene = this;
        }

        private void Start()
        {
            if (appearance != null) appearance.Apply(Ctx.Save.characterId);
            Hud = HudController.Create(this);

            Ctx.Wallet.Changed += Hud.Refresh;
            Ctx.Missions.Changed += Hud.Refresh;
            Ctx.City.Changed += Hud.Refresh;
            Ctx.Missions.Started += OnMissionStarted;
            Ctx.Missions.ObjectiveAdvanced += OnObjectiveAdvanced;
            Ctx.Missions.Completed += OnMissionCompleted;
            Hud.Refresh();

            Ctx.Save.lastScene = SceneNames.Game;
            if (!Ctx.Save.tutorialDone)
            {
                Hud.ShowDialog(Strings.GameTitle, Strings.Tutorial,
                    new DialogOption(Strings.GotIt, () => { Ctx.Save.tutorialDone = true; Ctx.SaveNow(); }));
            }
        }

        private void OnDestroy()
        {
            if (Ctx == null || Hud == null) return;
            Ctx.Wallet.Changed -= Hud.Refresh;
            Ctx.Missions.Changed -= Hud.Refresh;
            Ctx.City.Changed -= Hud.Refresh;
            Ctx.Missions.Started -= OnMissionStarted;
            Ctx.Missions.ObjectiveAdvanced -= OnObjectiveAdvanced;
            Ctx.Missions.Completed -= OnMissionCompleted;
        }

        private void Update()
        {
            // Real play time of the running mission feeds the interest report (not counted while a dialog is open).
            if (Ctx != null && Hud != null && !Hud.IsModalOpen) Ctx.Missions.Tick(Time.deltaTime);

            // Android back button / Escape
            if (Input.GetKeyDown(KeyCode.Escape) && Hud != null)
            {
                if (Hud.IsModalOpen) Hud.CloseDialog();
                else OpenPauseMenu();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && Ctx != null) Ctx.SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (Ctx != null) Ctx.SaveNow();
        }

        // ---------- global feedback ----------

        private void OnMissionStarted(MissionDefinition m)
        {
            Hud.Toast(Strings.MissionStarted + m.nameFa);
        }

        private void OnObjectiveAdvanced(MissionDefinition m, int index)
        {
            var o = m.objectives[index];
            int count = Ctx.Missions.GetObjectiveCount(m.id, index);
            string msg = o.textFa;
            if (o.RequiredCount > 1) msg += " (" + count + " از " + o.RequiredCount + ")";
            Hud.Toast(msg);
        }

        private void OnMissionCompleted(MissionCompletion c)
        {
            var sb = new StringBuilder();
            sb.Append(c.Mission.completeTextFa).Append("\n\n");
            if (c.Coins > 0 || c.Xp > 0)
            {
                sb.Append(Strings.Reward);
                if (c.Coins > 0) sb.Append(c.Coins).Append(' ').Append(Strings.Coins);
                if (c.Coins > 0 && c.Xp > 0) sb.Append("\u060C ");
                if (c.Xp > 0) sb.Append(c.Xp).Append(' ').Append(Strings.Xp);
                sb.Append('\n');
            }
            if (!string.IsNullOrEmpty(c.BadgeId))
            {
                string badgeName = c.BadgeId;
                foreach (var j in Ctx.Database.jobs)
                    if (j.badgeId == c.BadgeId) badgeName = j.badgeNameFa;
                sb.Append(badgeName).Append('\n');
            }
            if (!string.IsNullOrEmpty(c.BuildingId))
            {
                var b = Ctx.Database.FindBuilding(c.BuildingId);
                if (b != null && Ctx.City.GetState(c.BuildingId) == BuildingState.Restored)
                    sb.Append(b.nameFa).Append(" دوباره زنده شد!");
            }
            Hud.ShowDialog(Strings.MissionDone, sb.ToString().TrimEnd());
        }

        // ---------- menus ----------

        public void OpenPauseMenu()
        {
            Hud.ShowDialog(Strings.Menu, "",
                new DialogOption(Strings.Resume, null),
                new DialogOption(Strings.Missions, OpenMissionsPanel),
                new DialogOption(Strings.MyCity, OpenCityPanel),
                new DialogOption(Strings.Settings, OpenSettings),
                new DialogOption(Strings.MainMenu, GoToMainMenu));
            Time.timeScale = 0f;
        }

        public void OpenMissionsPanel()
        {
            var sb = new StringBuilder();
            foreach (var job in Ctx.Database.jobs)
            {
                sb.Append(job.nameFa).Append(": ")
                  .Append(Ctx.Missions.CompletedCountForJob(job.id)).Append(" از ")
                  .Append(Ctx.Missions.MissionCountForJob(job.id)).Append('\n');
                foreach (var m in Ctx.Database.missions)
                {
                    if (m.jobId != job.id) continue;
                    var st = Ctx.Missions.GetStatus(m.id);
                    sb.Append("\u2022 ").Append(m.nameFa).Append(" \u2014 ").Append(Strings.StatusFa(st));
                    if (st == MissionStatus.Active)
                    {
                        var o = Ctx.Missions.CurrentObjective;
                        if (o != null) sb.Append(" (").Append(o.textFa).Append(')');
                    }
                    sb.Append('\n');
                }
            }
            sb.Append('\n');
            if (Ctx.Save.badges.Count == 0) sb.Append(Strings.NoBadges);
            else
            {
                sb.Append(Strings.Badges);
                for (int i = 0; i < Ctx.Save.badges.Count; i++)
                {
                    string name = Ctx.Save.badges[i];
                    foreach (var j in Ctx.Database.jobs) if (j.badgeId == name) name = j.badgeNameFa;
                    if (i > 0) sb.Append("\u060C ");
                    sb.Append(name);
                }
            }

            var report = new DialogOption(Strings.MyReport, OpenReport);
            if (Ctx.Missions.ActiveMission != null)
                Hud.ShowDialog(Strings.Missions, sb.ToString(),
                    new DialogOption(Strings.Back, OpenPauseMenu),
                    report,
                    new DialogOption(Strings.AbandonMission, () => { Ctx.Missions.AbandonActive(); Ctx.SaveNow(); OpenPauseMenu(); }));
            else
                Hud.ShowDialog(Strings.Missions, sb.ToString(), new DialogOption(Strings.Back, OpenPauseMenu), report);
        }

        public void OpenReport()
        {
            ReportScreen.Show(Ctx, OpenMissionsPanel);
        }

        public void OpenCityPanel()
        {
            var sb = new StringBuilder();
            int pct = Mathf.RoundToInt(Ctx.City.OverallPercent() * 100f);
            sb.Append(Strings.CityProgress).Append(pct).Append(Strings.Percent).Append("\n\n");
            foreach (var b in Ctx.Database.buildings)
                sb.Append(b.nameFa).Append(": ").Append(Strings.BuildingStateFa(Ctx.City.GetState(b.id))).Append('\n');

            var s = Ctx.Recommender.Suggest(Ctx.DismissedSuggestions);
            DialogOption second = null;
            if (s != null)
            {
                var m = Ctx.Database.FindMission(s.MissionId);
                if (m != null)
                {
                    sb.Append('\n').Append(s.ReasonFa).Append('\n').Append(Strings.Suggestion).Append(m.nameFa);
                    string id = s.MissionId;
                    second = new DialogOption(Strings.DismissSuggestion, () => { Ctx.DismissedSuggestions.Add(id); OpenCityPanel(); });
                }
            }

            if (second != null)
                Hud.ShowDialog(Strings.MyCity, sb.ToString(), new DialogOption(Strings.Back, OpenPauseMenu), second);
            else
                Hud.ShowDialog(Strings.MyCity, sb.ToString(), new DialogOption(Strings.Back, OpenPauseMenu));
        }

        public void OpenSettings()
        {
            var st = Ctx.Save.settings;
            string body = "حساسیت دوربین: " + st.cameraSensitivity.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                        + "\nنرخ فریم: " + st.targetFps;
            Hud.ShowDialog(Strings.Settings, body,
                new DialogOption(Strings.Back, OpenPauseMenu),
                new DialogOption(Strings.SensitivityLess, () => { st.cameraSensitivity = Mathf.Max(0.4f, st.cameraSensitivity - 0.2f); Ctx.SaveNow(); OpenSettings(); }),
                new DialogOption(Strings.SensitivityMore, () => { st.cameraSensitivity = Mathf.Min(2.4f, st.cameraSensitivity + 0.2f); Ctx.SaveNow(); OpenSettings(); }),
                new DialogOption(st.targetFps == 30 ? Strings.Fps60 : Strings.Fps30, () =>
                {
                    st.targetFps = st.targetFps == 30 ? 60 : 30;
                    Application.targetFrameRate = st.targetFps;
                    Ctx.SaveNow();
                    OpenSettings();
                }));
        }

        public void GoToMainMenu()
        {
            Ctx.SaveNow();
            Time.timeScale = 1f;
            GameInput.Reset();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
