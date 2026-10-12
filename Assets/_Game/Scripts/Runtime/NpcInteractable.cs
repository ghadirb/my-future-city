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

            // 2) A mission is running but needs something else first.
            var active = ms.ActiveMission;
            if (active != null)
            {
                var obj = ms.CurrentObjective;
                string hint = obj != null ? obj.textFa : active.descriptionFa;
                scene.Hud.ShowDialog(displayNameFa, Strings.MoreWork + hint);
                return;
            }

            // 3) Offer the next mission this NPC gives, otherwise a repeat run.
            MissionDefinition offer = null;
            bool isRepeat = false;
            foreach (var m in ctx.Database.missions)
            {
                if (m.startNpcId != npcId) continue;
                if (ms.GetStatus(m.id) == MissionStatus.Available) { offer = m; break; }
            }
            if (offer == null)
            {
                foreach (var m in ctx.Database.missions)
                {
                    if (m.startNpcId != npcId || !m.repeatable) continue;
                    if (ms.GetStatus(m.id) == MissionStatus.Completed) { offer = m; isRepeat = true; break; }
                }
            }

            if (offer != null)
            {
                string id = offer.id;
                string text = isRepeat ? Strings.RepeatOffer : offer.offerTextFa;
                scene.Hud.ShowDialog(displayNameFa, text,
                    new DialogOption(Strings.Accept, () => ms.Start(id)),
                    new DialogOption(Strings.Later, null));
                return;
            }

            scene.Hud.ShowDialog(displayNameFa, Strings.NpcIdle);
        }
    }
}
