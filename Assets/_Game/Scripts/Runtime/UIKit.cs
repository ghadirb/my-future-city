using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyFutureCity
{
    /// <summary>Code-built uGUI helpers. All text goes through PersianShaper.</summary>
    public static class UIKit
    {
        private static Font font;
        private static Sprite circle;
        private static Sprite rounded;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.Load<Font>("Fonts/Vazirmatn-Regular");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                return font;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circle != null) return circle;
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - r, dy = y + 0.5f - r;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(r - d);          // 1px soft edge
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return circle;
            }
        }

        /// <summary>A 9-sliced rounded rectangle (24 px corners) so panels and buttons look soft and child-friendly.</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64;
                const float r = 24f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float px = x + 0.5f, py = y + 0.5f;
                        float cx = Mathf.Clamp(px, r, size - r), cy = Mathf.Clamp(py, r, size - r);
                        float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f)));
                    }
                }
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return rounded;
            }
        }

        /// <summary>A vertical colour gradient (top to bottom), stretched over whatever rect it is put on.</summary>
        public static Sprite VerticalGradient(Color top, Color bottom)
        {
            const int h = 64;
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(h - 1));
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A rounded rectangle image (sliced). Child Images should use raycast=false inside buttons.</summary>
        public static Image Rounded(Transform parent, string name, Color color, bool raycast)
        {
            var img = Box(parent, name, color, raycast);
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>A circle/ellipse image of the given size at a position (centre anchored).</summary>
        public static Image Disc(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var img = Box(parent, name, color, false);
            img.sprite = CircleSprite;
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            return img;
        }

        /// <summary>A rounded block at a position (centre anchored), optionally tilted.</summary>
        public static Image Block(Transform parent, string name, Color color, Vector2 pos, Vector2 size, float tilt = 0f)
        {
            var img = Rounded(parent, name, color, false);
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            if (tilt != 0f) img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            return img;
        }

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static Image Box(Transform parent, string name, Color color, bool raycast)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(Transform parent, string logical, int size, TextAnchor anchor, Color color, int wrapChars)
        {
            var rt = NewRect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            SetText(t, logical, wrapChars);
            return t;
        }

        public static void SetText(Text t, string logical, int wrapChars)
        {
            t.text = Core.PersianShaper.Fix(logical, wrapChars);
        }

        public static Button MakeButton(Transform parent, string logical, int size, Color bg, UnityAction onClick, int wrapChars = 0)
        {
            var img = Rounded(parent, "Button", bg, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var label = Label(img.transform, logical, size, TextAnchor.MiddleCenter, Color.white, wrapChars);
            Stretch(label.rectTransform, new Vector2(12, 4), new Vector2(-12, -4));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = size;
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }
    }
}
