using UnityEngine;

namespace MyFutureCity
{
    /// <summary>
    /// A big bobbing yellow marker floating above whatever the child should go to next
    /// (the nearest mission-giver when idle, otherwise the current objective). It grows with
    /// distance so it can be spotted from across the neighbourhood.
    /// </summary>
    public class GuideMarker : MonoBehaviour
    {
        private GameSceneController scene;
        private Transform marker;

        public static void Create(GameSceneController owner)
        {
            var go = new GameObject("GuideMarker");
            var gm = go.AddComponent<GuideMarker>();
            gm.scene = owner;
            gm.Build();
        }

        private void Build()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Marker";
            var col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);
            sphere.transform.SetParent(transform, false);
            Shader sh = Shader.Find("Unlit/Color");
            var r = sphere.GetComponent<Renderer>();
            if (sh != null)
            {
                var m = new Material(sh);
                m.color = new Color(1f, 0.85f, 0.1f);
                r.material = m;
            }
            marker = sphere.transform;
            marker.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (scene == null || scene.Ctx == null || scene.player == null || marker == null) return;
            var target = Interactable.FindObjectiveTarget(scene.player.transform.position, scene.Ctx);
            if (target == null) { if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false); return; }

            Vector3 pos = target.transform.position;
            float dist = (pos - scene.player.transform.position).magnitude;
            if (dist < 3f) { if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false); return; }

            if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
            float size = Mathf.Clamp(dist * 0.035f, 0.7f, 2.6f);
            marker.localScale = new Vector3(size, size, size);
            marker.position = pos + new Vector3(0f, 3.4f + size * 0.6f + Mathf.Sin(Time.time * 3f) * 0.25f, 0f);
        }
    }
}
