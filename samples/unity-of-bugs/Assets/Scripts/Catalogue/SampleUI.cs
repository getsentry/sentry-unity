using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sentry.Unity.Samples
{
    /// <summary>
    /// Builds the samples' uGUI from code, with Unity's built-in font: the sample ships as scenes and scripts only.
    /// </summary>
    public static class SampleUI
    {
        // Sentry's colours
        public static readonly Color Background = new Color32(0x15, 0x0F, 0x22, 255);
        public static readonly Color Panel = new Color32(0x18, 0x12, 0x25, 255);
        public static readonly Color HeaderColor = new Color32(0x1F, 0x16, 0x33, 255);
        public static readonly Color Row = new Color32(0x2B, 0x22, 0x40, 255);
        public static readonly Color Accent = new Color32(0x75, 0x53, 0xFF, 255);
        public static readonly Color Danger = new Color32(0xFF, 0x45, 0xA8, 255);
        public static readonly Color TextColor = new Color32(0xF5, 0xF0, 0xFF, 255);
        public static readonly Color Muted = new Color32(0xC9, 0xBE, 0xE8, 255);
        public static readonly Color Warning = new Color32(0xFD, 0xB8, 0x1B, 255);

        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
#if UNITY_2022_2_OR_NEWER
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                }

                return _font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Text(string name, Transform parent, string text, int size, Color color,
            FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var label = Rect(name, parent).gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(string name, Transform parent, string label, int fontSize, Color color, UnityAction onClick)
        {
            var image = Image(name, parent, Color.white);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Tint(button, color);
            button.onClick.AddListener(onClick);

            if (label != null)
            {
                var text = Text("Label", image.transform, label, fontSize, TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform);
            }

            return button;
        }

        public static void Tint(Button button, Color color)
        {
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.selectedColor = color;
            colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
            colors.disabledColor = Color.Lerp(color, Background, 0.6f);
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
        }

        /// <summary>A square button with an ✕ drawn from two bars, as the built-in font has no ✕.</summary>
        public static Button CloseButton(Transform parent, float size, UnityAction onClick)
        {
            var button = Button("CloseButton", parent, null, 0, new Color(1, 1, 1, 0.08f), onClick);
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(size, size);

            foreach (var angle in new[] { 45f, -45f })
            {
                var bar = Image("Bar", rect, TextColor);
                bar.raycastTarget = false;
                bar.rectTransform.sizeDelta = new Vector2(size * 0.55f, 2.5f);
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }

            return button;
        }

        /// <summary>A scroll view filling its parent. Returns the content, which a layout group sizes.</summary>
        public static RectTransform Scroll(string name, Transform parent, bool vertical, out ScrollRect scroll)
        {
            var root = Image(name, parent, new Color(0, 0, 0, 0));
            Stretch(root.rectTransform);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = !vertical;
            scroll.vertical = vertical;
            // Bounces softly at the ends, and a flick stops soon after the finger leaves.
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.decelerationRate = 0.1f;
            scroll.scrollSensitivity = 30;
            scroll.inertia = true;

            var viewport = Rect("Viewport", root.transform);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            var content = Rect("Content", viewport);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            if (vertical)
            {
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(0.5f, 1);
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(0, 1);
                content.pivot = new Vector2(0, 0.5f);
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            scroll.content = content;
            return content;
        }

        public static T Layout<T>(Component target, int left, int right, int top, int bottom, float spacing)
            where T : HorizontalOrVerticalLayoutGroup
        {
            var group = target.gameObject.AddComponent<T>();
            group.padding = new RectOffset(left, right, top, bottom);
            group.spacing = spacing;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        public static LayoutElement Size(Component target, float width = -1, float height = -1, float flexibleWidth = -1)
        {
            // Not ??: a missing component is a Unity null, which ?? doesn't see.
            var element = target.gameObject.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.gameObject.AddComponent<LayoutElement>();
            }

            element.minWidth = width;
            element.preferredWidth = width;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
            return element;
        }

        public static void DestroyChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                // Out of the layout at once: Destroy waits for the end of the frame.
                child.SetActive(false);
                child.transform.SetParent(null, false);
                UnityEngine.Object.Destroy(child);
            }
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
