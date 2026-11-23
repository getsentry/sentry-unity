using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sentry.Unity.Samples
{
    /// <summary>
    /// The scene the scene samples load. On its own it shows a header with a way back to the menu; loaded on top of
    /// the menu it shows a panel that unloads it.
    /// </summary>
    public class ExtraScene : MonoBehaviour
    {
        public const string Name = "ExtraScene";
        private const string MenuScene = "BugFarm";
        private const float ReferenceShortSide = 440;

        /// <summary>Set by the sample that loads the scene, before it loads.</summary>
        public static bool LoadedAdditively { get; set; }

        [SerializeField] private Canvas _canvas;
        [SerializeField] private Camera _camera;
        [SerializeField] private GameObject _eventSystem;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => LoadedAdditively = false;

        private void Awake()
        {
            // Scaled by the short side, as the menu is.
            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceShortSide, ReferenceShortSide);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var header = _canvas.transform.Find("Header");
            if (LoadedAdditively)
            {
                // The menu's camera, event system and header stay in charge.
                _camera.gameObject.SetActive(false);
                _eventSystem.SetActive(false);
                if (header != null)
                {
                    header.gameObject.SetActive(false);
                }

                // Above the menu, below the feedback form
                _canvas.sortingOrder = 100;
                BuildOverlay();
            }
            else
            {
                BuildHeader((RectTransform)header);
            }

            LoadedAdditively = false;
        }

        private void BuildHeader(RectTransform header)
        {
            var background = SampleUI.Image("Background", _canvas.transform, SampleUI.Background);
            background.raycastTarget = false;
            SampleUI.Stretch(background.rectTransform);
            background.transform.SetAsFirstSibling();

            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            header.anchoredPosition = new Vector2(0, -SafeTopInset());
            header.sizeDelta = new Vector2(0, 44);

            var headerBackground = SampleUI.Image("Background", header, SampleUI.HeaderColor);
            headerBackground.raycastTarget = false;
            SampleUI.Stretch(headerBackground.rectTransform);
            headerBackground.transform.SetAsFirstSibling();

            var back = (RectTransform)SampleUI.Button("Back", header, "Back", 13, SampleUI.Accent,
                () => SceneManager.LoadScene(MenuScene)).transform;
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0, 0.5f);
            back.anchoredPosition = new Vector2(8, 0);
            back.sizeDelta = new Vector2(60, 28);

            var title = SampleUI.Text("Title", header, "Extra Scene", 19, SampleUI.TextColor, FontStyle.Bold,
                TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = Vector2.zero;
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(80, 0);
            title.rectTransform.offsetMax = new Vector2(-52, 0);

            var note = SampleUI.Text("Note", _canvas.transform,
                "Loaded on its own. The load arrives in Sentry as a transaction.", 13, SampleUI.Muted,
                FontStyle.Normal, TextAnchor.MiddleCenter);
            SampleUI.Stretch(note.rectTransform);
            note.rectTransform.offsetMin = new Vector2(24, 0);
            note.rectTransform.offsetMax = new Vector2(-24, 0);
        }

        private void BuildOverlay()
        {
            var dim = SampleUI.Image("Dim", _canvas.transform, new Color(0, 0, 0, 0.6f));
            SampleUI.Stretch(dim.rectTransform);

            var panel = SampleUI.Image("Panel", _canvas.transform, SampleUI.Panel);
            panel.rectTransform.sizeDelta = new Vector2(260, 0);
            SampleUI.Layout<VerticalLayoutGroup>(panel, 16, 16, 14, 16, 10).childForceExpandWidth = true;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SampleUI.Text("Title", panel.transform, "Extra Scene", 17, SampleUI.TextColor, FontStyle.Bold,
                TextAnchor.MiddleCenter);
            SampleUI.Text("Note", panel.transform, "Loaded on top of the menu.", 12, SampleUI.Muted,
                FontStyle.Normal, TextAnchor.MiddleCenter);
            var close = SampleUI.Button("Close", panel.transform, "Close", 13, SampleUI.Accent,
                () => SceneManager.UnloadSceneAsync(gameObject.scene));
            SampleUI.Size(close, height: 30);
        }

        private static float SafeTopInset()
        {
            var scale = Mathf.Min(Screen.width, Screen.height) / ReferenceShortSide;
            return (Screen.height - Screen.safeArea.yMax) / scale;
        }
    }
}
