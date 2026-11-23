using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sentry.Unity.Samples
{
    /// <summary>
    /// The sample menu: the catalogue by category, with a Run button for each sample and a status line.
    /// Builds its UI from code on the canvas it sits on, and lays it out again when the screen turns.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class SampleMenu : MonoBehaviour
    {
        // A phone's short side in points, so one canvas unit is about a point on a phone.
        private const float ReferenceShortSide = 440;

        private const float HeaderHeight = 44;
        private const float StatusHeight = 30;
        private const float SidebarWidth = 136;
        private const float TabsHeight = 38;

        // SentryUserFeedback's megaphone: a 30x30 button 10 units in from the bottom-right corner of a 270x480
        // canvas that expands to fit the screen.
        private const float FeedbackButtonReach = 30 + 10;
        private const float FeedbackWidth = 270;
        private const float FeedbackHeight = 480;

        private static int _selectedCategory;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _safeArea;
        private RectTransform _header;
        private RectTransform _nav;
        private RectTransform _body;
        private RectTransform _status;
        private Text _statusText;
        private ScrollRect _bodyScroll;

        private Vector2Int _laidOutScreen;
        private Rect _laidOutSafeArea;
        private bool _portrait;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null)
            {
                _scaler = gameObject.AddComponent<CanvasScaler>();
            }

            // Scaled by the short side, so the UI keeps its size when the screen turns.
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(ReferenceShortSide, ReferenceShortSide);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var background = SampleUI.Image("Background", transform, SampleUI.Background);
            background.raycastTarget = false;
            SampleUI.Stretch(background.rectTransform);
            background.transform.SetAsFirstSibling();

            _safeArea = SampleUI.Rect("SafeArea", transform);

            // The scene keeps the header: SpinningCube finds it by name and puts its cube in the top-right corner.
            var header = transform.Find("Header");
            _header = header != null ? (RectTransform)header : SampleUI.Rect("Header", transform);
            _header.SetParent(_safeArea, false);
            BuildHeader();

            _nav = SampleUI.Rect("Categories", _safeArea);
            _body = SampleUI.Rect("Samples", _safeArea);
            BuildStatus();

            SampleRunner.StatusChanged += ShowStatus;
            ShowStatus(SampleRunner.Status ?? LastRunText());

            Layout();
        }

        private void Start() => SampleRunner.Instance.RunFromLaunchArguments();

        private void OnDestroy() => SampleRunner.StatusChanged -= ShowStatus;

        private void Update()
        {
            if (_laidOutScreen.x != Screen.width || _laidOutScreen.y != Screen.height || _laidOutSafeArea != Screen.safeArea)
            {
                Layout();
            }
        }

        private static string LastRunText()
        {
            switch (SentrySdk.GetLastRunState())
            {
                case SentrySdk.CrashedLastRun.Crashed:
                    return "Last run crashed";
                case SentrySdk.CrashedLastRun.DidNotCrash:
                    return "Last run ended cleanly";
                default:
                    return "Last run: unknown";
            }
        }

        private void ShowStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        private void BuildHeader()
        {
            var background = SampleUI.Image("Background", _header, SampleUI.HeaderColor);
            background.raycastTarget = false;
            SampleUI.Stretch(background.rectTransform);
            background.transform.SetAsFirstSibling();

            var close = (RectTransform)SampleUI.CloseButton(_header, 32, SampleUI.Quit).transform;
            close.anchorMin = close.anchorMax = close.pivot = new Vector2(0, 0.5f);
            close.anchoredPosition = new Vector2(6, 0);

            var title = SampleUI.Text("Title", _header, "Unity of Bugs", 19, SampleUI.TextColor, FontStyle.Bold,
                TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0, 0);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            // From the ✕ to short of the cube's corner
            title.rectTransform.offsetMin = new Vector2(48, 0);
            title.rectTransform.offsetMax = new Vector2(-52, 0);
        }

        private void BuildStatus()
        {
            var background = SampleUI.Image("Status", _safeArea, SampleUI.Panel);
            background.raycastTarget = false;
            _status = background.rectTransform;
            _statusText = SampleUI.Text("Text", _status, "", 12, SampleUI.Muted, FontStyle.Normal, TextAnchor.MiddleLeft);
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statusText.verticalOverflow = VerticalWrapMode.Truncate;
            _statusText.resizeTextMaxSize = 12;
            _statusText.resizeTextMinSize = 9;
            _statusText.resizeTextForBestFit = true;
            SampleUI.Stretch(_statusText.rectTransform);
            _statusText.rectTransform.offsetMin = new Vector2(10, 0);
            _statusText.rectTransform.offsetMax = new Vector2(-6, 0);
        }

        private void Layout()
        {
            _laidOutScreen = new Vector2Int(Screen.width, Screen.height);
            _laidOutSafeArea = Screen.safeArea;
            var portraitBefore = _portrait;
            _portrait = Screen.height > Screen.width;

            var scale = Mathf.Min(Screen.width, Screen.height) / ReferenceShortSide;

            // The safe area in canvas units, as offsets from the screen's edges
            var safe = _laidOutSafeArea;
            _safeArea.anchorMin = Vector2.zero;
            _safeArea.anchorMax = Vector2.one;
            _safeArea.offsetMin = new Vector2(safe.xMin, safe.yMin) / scale;
            _safeArea.offsetMax = new Vector2(safe.xMax - Screen.width, safe.yMax - Screen.height) / scale;

            _header.anchorMin = new Vector2(0, 1);
            _header.anchorMax = new Vector2(1, 1);
            _header.pivot = new Vector2(0.5f, 1);
            _header.anchoredPosition = Vector2.zero;
            _header.sizeDelta = new Vector2(0, HeaderHeight);

            // The status line ends short of the megaphone, which sits in the screen's corner, outside the safe area.
            var feedbackScale = Mathf.Min(Screen.width / FeedbackWidth, Screen.height / FeedbackHeight);
            var megaphone = (FeedbackButtonReach + 6) * feedbackScale / scale;
            var rightInset = (Screen.width - safe.xMax) / scale;
            _status.anchorMin = new Vector2(0, 0);
            _status.anchorMax = new Vector2(1, 0);
            _status.pivot = new Vector2(0, 0);
            _status.offsetMin = new Vector2(8, 6);
            _status.offsetMax = new Vector2(-Mathf.Max(8, megaphone - rightInset), 6 + StatusHeight);

            var bottom = 6 + StatusHeight + 6;
            if (_portrait)
            {
                Place(_nav, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -HeaderHeight - TabsHeight), new Vector2(0, -HeaderHeight));
                Place(_body, Vector2.zero, Vector2.one, new Vector2(0, bottom), new Vector2(0, -HeaderHeight - TabsHeight));
            }
            else
            {
                Place(_nav, Vector2.zero, new Vector2(0, 1), new Vector2(0, bottom), new Vector2(SidebarWidth, -HeaderHeight));
                Place(_body, Vector2.zero, Vector2.one, new Vector2(SidebarWidth, bottom), new Vector2(0, -HeaderHeight));
            }

            if (_nav.childCount == 0 || portraitBefore != _portrait)
            {
                BuildNav();
                BuildBody();
            }
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void BuildNav()
        {
            SampleUI.DestroyChildren(_nav);

            var background = SampleUI.Image("Background", _nav, SampleUI.Panel);
            background.raycastTarget = false;
            SampleUI.Stretch(background.rectTransform);

            var content = SampleUI.Scroll("Scroll", _nav, !_portrait, out _);
            if (_portrait)
            {
                SampleUI.Layout<HorizontalLayoutGroup>(content, 6, 6, 5, 5, 4).childForceExpandHeight = true;
            }
            else
            {
                SampleUI.Layout<VerticalLayoutGroup>(content, 6, 6, 6, 6, 3).childForceExpandWidth = true;
            }

            var categories = SampleCatalogue.Categories;
            for (var i = 0; i < categories.Count; i++)
            {
                var index = i;
                var selected = i == _selectedCategory;
                var button = SampleUI.Button(categories[i].Name, content, categories[i].Name, 13,
                    selected ? SampleUI.Accent : new Color(1, 1, 1, 0.04f), () => SelectCategory(index));
                var label = button.GetComponentInChildren<Text>();
                label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                label.color = selected ? SampleUI.TextColor : SampleUI.Muted;
                if (_portrait)
                {
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    SampleUI.Size(button, label.preferredWidth + 20);
                }
                else
                {
                    label.alignment = TextAnchor.MiddleLeft;
                    label.rectTransform.offsetMin = new Vector2(10, 0);
                    SampleUI.Size(button, height: 30);
                }
            }
        }

        private void SelectCategory(int index)
        {
            _selectedCategory = index;
            BuildNav();
            BuildBody();
        }

        private void BuildBody()
        {
            SampleUI.DestroyChildren(_body);
            _selectedCategory = Mathf.Clamp(_selectedCategory, 0, SampleCatalogue.Categories.Count - 1);
            var category = SampleCatalogue.Categories[_selectedCategory];

            var content = SampleUI.Scroll("Scroll", _body, true, out _bodyScroll);
            SampleUI.Layout<VerticalLayoutGroup>(content, 8, 8, 8, 8, 6).childForceExpandWidth = true;

            if (category.Name == "Errors")
            {
                BuildRunOnSelector(content);
            }

            foreach (var entry in category.Entries)
            {
                BuildRow(content, entry);
            }

            _bodyScroll.verticalNormalizedPosition = 1;
        }

        private void BuildRunOnSelector(Transform parent)
        {
            var row = SampleUI.Image("RunOn", parent, SampleUI.Panel);
            row.raycastTarget = false;
            SampleUI.Layout<VerticalLayoutGroup>(row, 10, 10, 6, 8, 4).childForceExpandWidth = true;

            SampleUI.Text("Label", row.transform, "Run on (exceptions, messages and logs)", 12, SampleUI.Muted);

            var options = SampleUI.Rect("Options", row.transform);
            var optionsLayout = SampleUI.Layout<HorizontalLayoutGroup>(options, 0, 0, 0, 0, 4);
            optionsLayout.childForceExpandWidth = true;
            optionsLayout.childForceExpandHeight = true;
            SampleUI.Size(options, height: 24);

            foreach (RunOn runOn in Enum.GetValues(typeof(RunOn)))
            {
                var value = runOn;
                var selected = SampleCatalogue.RunOn == value;
                var button = SampleUI.Button(value.ToString(), options, RunOnLabel(value), 11,
                    selected ? SampleUI.Accent : SampleUI.Row, () =>
                    {
                        SampleCatalogue.RunOn = value;
                        BuildBody();
                    });
                var label = button.GetComponentInChildren<Text>();
                label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                label.resizeTextForBestFit = true;
                label.resizeTextMaxSize = 11;
                label.resizeTextMinSize = 8;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                SampleUI.Size(button, flexibleWidth: 1);
            }
        }

        private static string RunOnLabel(RunOn runOn)
        {
            switch (runOn)
            {
                case RunOn.MainThread: return "Main";
                case RunOn.AwaitedTask: return "await";
                default: return runOn.ToString();
            }
        }

        private void BuildRow(Transform parent, SampleEntry entry)
        {
            var reason = entry.UnmetReason;
            var row = SampleUI.Image(entry.Id, parent, SampleUI.Row);
            row.raycastTarget = false;
            var layout = SampleUI.Layout<HorizontalLayoutGroup>(row, 10, 8, 7, 7, 8);
            layout.childAlignment = TextAnchor.MiddleLeft;

            var texts = SampleUI.Rect("Texts", row.transform);
            SampleUI.Layout<VerticalLayoutGroup>(texts, 0, 0, 0, 0, 2).childForceExpandWidth = true;
            SampleUI.Size(texts, flexibleWidth: 1);

            var title = entry.Title + (entry.Crashes ? "  (crashes)" : "");
            SampleUI.Text("Title", texts, title, 14, entry.Crashes ? SampleUI.Danger : SampleUI.TextColor, FontStyle.Bold);
            SampleUI.Text("Description", texts, entry.Description, 11, SampleUI.Muted);
            if (reason != null)
            {
                SampleUI.Text("Reason", texts, reason, 11, SampleUI.Warning, FontStyle.Italic);
            }

            var run = SampleUI.Button("Run", row.transform, "Run", 12, entry.Crashes ? SampleUI.Danger : SampleUI.Accent,
                () => SampleRunner.Instance.Run(entry));
            SampleUI.Size(run, 52, 24);
            run.interactable = reason == null;

            if (reason != null)
            {
                row.gameObject.AddComponent<CanvasGroup>().alpha = 0.55f;
            }
        }
    }
}
