using UnityEngine;

namespace MyFutureCity
{
    /// <summary>The player's house: shows city progress and saves.</summary>
    public class HomeInteractable : Interactable
    {
        public override void Interact(GameSceneController scene)
        {
            scene.Ctx.SaveNow();
            scene.OpenCityPanel();
            scene.Hud.Toast(Strings.Saved);
        }
    }
}
