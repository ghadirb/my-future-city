using UnityEngine;

namespace MyFutureCity
{
    public class Interactor : MonoBehaviour
    {
        public GameSceneController scene;
        public Interactable Current { get; private set; }

        private void Update()
        {
            if (scene == null || scene.Ctx == null) return;

            bool pressed = GameInput.ConsumeInteract() || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
            if (GameInput.Blocked)
            {
                Current = null;
                return;
            }

            Current = Interactable.FindBest(transform.position, transform.forward, scene.Ctx);
            if (pressed && Current != null) Current.Interact(scene);
        }
    }
}
