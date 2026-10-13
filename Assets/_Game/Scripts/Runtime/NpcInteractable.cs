using System.Collections.Generic;
using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    public class NpcInteractable : Interactable
    {
        public string npcId;
        public string displayNameFa;

        public override string GetPrompt(GameContext ctx)
        {
            return "گفت\u200Cوگو با " + displayNameFa;
        }

        public override bool MatchesCurrentObjective(GameContext ctx)
        {
            return ctx.Missions.IsObjectiveCurrent(ObjectiveType.Talk, npcId);
        }

        public override string GuideName { get { return displayNameFa; } }

        /// <summary>With no active mission, guide the child to the nearest NPC who has a mission to offer.</summary>
        public override bool IsGuideTarget(GameContext ctx)
        {
            var ms = ctx.Missions;
            if (ms.ActiveMission != null) return MatchesCurrentObjective(ctx);
            foreach (var m in ctx.Database.missions)
                if (m.startNpcId == npcId && ms.GetStatus(m.id) == MissionStatus.Available) return true;
            return false;
        }

        public override void Interact(GameSceneController scene)
        {
            var ctx = scene.Ctx;
            var ms = ctx.Missions;

            // 1) The active mission is waiting for the player to come back and talk.
            if (ms.IsObjectiveCurrent(ObjectiveType.Talk, npcId))
            {
                ms.Notify(ObjectiveType.Talk, npcId);
                var still = ms.CurrentObjective;
                if (ms.ActiveMission != null && still != null)
                    scene.Hud.ShowDialog(displayNameFa, Strings.MoreWork + still.textFa);
                return;          // completion UI is shown by GameSceneController
            }

            // 2) A mission is running but needs something else first. The child may also drop it
            //    and pick any other work this person offers (no penalty, it is only noted in the report).
            var active = ms.ActiveMission;
            if (active != null)
            {
                var obj = ms.CurrentObjective;
                string hint = obj != null ? obj.textFa : active.descriptionFa;
                if (ms.OfferableForNpc(npcId).Count > 0)
                    scene.Hud.ShowDialog(displayNameFa, Strings.MoreWork + hint,
                        new DialogOption(Strings.GotIt, null),
                        new DialogOption(Strings.SwitchJob, () => ShowChooser(scene, 0, true)));
                else
                    scene.Hud.ShowDialog(displayNameFa, Strings.MoreWork + hint);
                return;
            }

            // 3) Nothing running: offer what this person has - new stages first, then any stage the
            //    child finished and may want to do again.
            var offers = ms.OfferableForNpc(npcId);
            if (offers.Count == 1) { ShowOffer(scene, offers[0], false); return; }
            if (offers.Count > 1) { ShowChooser(scene, 0, false); return; }

            scene.Hud.ShowDialog(displayNameFa, Strings.NpcIdle);
        }

        private const int PageSize = 3;

        private void ShowOffer(GameSceneController scene, MissionDefinition def, bool switching)
        {
            var ms = scene.Ctx.Missions;
            bool isRepeat = ms.TimesCompleted(def.id) > 0;
            string id = def.id;
            string text = isRepeat ? Strings.RepeatOffer : def.offerTextFa;
            scene.Hud.ShowDialog(displayNameFa, text,
                new DialogOption(Strings.Accept, () => { if (switching) ms.SwitchTo(id); else ms.Start(id); }),
                new DialogOption(Strings.Later, null));
        }

        /// <summary>Lets the child pick any available or replayable stage (3 per page).</summary>
        private void ShowChooser(GameSceneController scene, int page, bool switching)
        {
            var ms = scene.Ctx.Missions;
            var offers = ms.OfferableForNpc(npcId);
            if (offers.Count == 0) { scene.Hud.ShowDialog(displayNameFa, Strings.NpcIdle); return; }
            int pages = (offers.Count + PageSize - 1) / PageSize;
            if (page >= pages) page = 0;

            var body = new System.Text.StringBuilder(Strings.ChooseWork);
            var options = new List<DialogOption>();
            for (int i = page * PageSize; i < offers.Count && i < (page + 1) * PageSize; i++)
            {
                var def = offers[i];
                int done = ms.TimesCompleted(def.id);
                string tag = done > 0 ? Strings.TagAgain : Strings.TagNew;
                body.Append("\n\u2022 ").Append(def.nameFa).Append(" \u2014 ").Append(tag);
                var chosen = def;
                options.Add(new DialogOption(def.nameFa, () => ShowOffer(scene, chosen, switching)));
            }
            if (pages > 1)
            {
                int next = page + 1;
                options.Add(new DialogOption(Strings.More, () => ShowChooser(scene, next, switching)));
            }
            options.Add(new DialogOption(Strings.Later, null));
            scene.Hud.ShowDialog(displayNameFa, body.ToString(), options.ToArray());
        }
    }
}
