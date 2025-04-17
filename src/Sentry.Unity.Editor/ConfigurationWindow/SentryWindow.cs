using System;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

public class SentryWindow : EditorWindow
{
    private const string LinkXmlPath = "Assets/Plugins/Sentry/link.xml";

    public const string EditorMenuPath = "Tools -> Sentry";

    [MenuItem("Tools/Sentry")]
    public static void OnMenuClick()
    {
        if (Wizard.InProgress)
        {
            Debug.Log("Wizard in progress, ignoring Tools/Sentry menu click");
            return;
        }
        if (Instance is null && IsFirstLoad && EditorUtility.DisplayDialog("Start a setup wizard?",
                "It looks like you're setting up Sentry for the first time in this project.\n\n" +
                "Would you like to start a setup wizard to connect to sentry.io?", "Start wizard", "I'll set it up manually"))
        {
            Wizard.Start(CreateLogger());
        }
        else
        {
            OpenSentryWindow();
        }
    }

    public static void OpenSentryWindow()
    {
        Instance = GetWindow<SentryWindow>();
        Instance.minSize = new Vector2(800, 600);
    }

    public static SentryWindow? Instance;

    protected static string SentryOptionsAssetName { get; set; } = ScriptableSentryUnityOptions.ConfigName;
    protected static string SentryCliAssetName { get; } = SentryCliOptions.ConfigName;

    public ScriptableSentryUnityOptions Options { get; private set; } = null!; // Set by OnEnable()
    public SentryCliOptions CliOptions { get; private set; } = null!; // Set by OnEnable()
    public static Texture2D? ErrorIcon { get; private set; }

    public event Action<ValidationError> OnValidationError = _ => { };

    private int _currentTab = 0;
    private readonly string[] _tabs =
    {
        "Core",
        "Logging",
        "Enrichment",
        "Transport",
        "Advanced",
        "Options Config",
        "Debug Symbols"
    };

    private IDiagnosticLogger _logger;

    private static string OptionsPath => ScriptableSentryUnityOptions.GetConfigPath(SentryOptionsAssetName);
    private static string CliOptionsPath => SentryCliOptions.GetConfigPath(SentryCliAssetName);

    public SentryWindow()
    {
        _logger = CreateLogger();
    }

    private static IDiagnosticLogger CreateLogger() =>
        new UnityLogger(new SentryOptions() { Debug = SentryPackageInfo.IsDevPackage });

    void OnDestroy()
    {
        Instance = null;
    }

    private void OnEnable()
    {
        // Note: these are not allowed to be called in constructors
        SetTitle(this);
        Options = SentryScriptableObject.CreateOrLoad<ScriptableSentryUnityOptions>(OptionsPath);
        CliOptions = SentryScriptableObject.CreateOrLoad<SentryCliOptions>(CliOptionsPath);
        ErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
    }

    internal static void SaveWizardResult(WizardConfiguration config)
    {
        var options = SentryScriptableObject.CreateOrLoad<ScriptableSentryUnityOptions>(OptionsPath);
        var cliOptions = SentryScriptableObject.CreateOrLoad<SentryCliOptions>(CliOptionsPath);
        options.Dsn = config.Dsn;
        cliOptions.UploadSymbols = !string.IsNullOrWhiteSpace(config.Token);
        cliOptions.Auth = config.Token;
        cliOptions.Organization = config.OrgSlug;
        cliOptions.Project = config.ProjectSlug;

        EditorUtility.SetDirty(options);
        EditorUtility.SetDirty(cliOptions);
        AssetDatabase.SaveAssets();
    }

    private static bool IsFirstLoad =>
        !File.Exists(ScriptableSentryUnityOptions.GetConfigPath(SentryOptionsAssetName)) &&
        !File.Exists(SentryCliOptions.GetConfigPath(SentryCliAssetName));

    // ReSharper disable once UnusedMember.Local
    private void OnGUI()
    {
        EditorGUILayout.Space();
        GUILayout.Label("SDK Options", EditorStyles.boldLabel);

        Options.Enabled = EditorGUILayout.ToggleLeft(
            new GUIContent("Enable Sentry", "Controls if the SDK should initialize itself or not."),
            Options.Enabled);

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        var selectedTab = GUILayout.Toolbar(_currentTab, _tabs);
        if (selectedTab != _currentTab)
        {
            // Edge-case: Lose focus so currently selected fields don't "bleed" through like DSN -> Override Release
            GUI.FocusControl(null);
            _currentTab = selectedTab;
        }

        EditorGUI.BeginDisabledGroup(!Options.Enabled);
        EditorGUILayout.Space();

        switch (_currentTab)
        {
            case 0:
                CoreTab.Display(Options);
                break;
            case 1:
                LoggingTab.Display(Options);
                break;
            case 2:
                EnrichmentTab.Display(Options);
                break;
            case 3:
                TransportTab.Display(Options);
                break;
            case 4:
                AdvancedTab.Display(Options, CliOptions);
                break;
            case 5:
                OptionsConfigurationTab.Display(Options);
                break;
            case 6:
                DebugSymbolsTab.Display(CliOptions);
                break;
            default:
                break;
        }

        EditorGUI.EndDisabledGroup();
    }

    private void OnLostFocus()
    {
        // Make sure the actual config asset exists before validating/saving. Crashes the editor otherwise.
        if (!File.Exists(ScriptableSentryUnityOptions.GetConfigPath(SentryOptionsAssetName)))
        {
            _logger.LogWarning("Options could not been saved. The configuration asset is missing.");
            return;
        }

        Validate();

        EditorUtility.SetDirty(Options);
        EditorUtility.SetDirty(CliOptions);
        AssetDatabase.SaveAssets();
    }

    private void Validate()
    {
        if (!Options.Enabled)
        {
            return;
        }

        ValidateDsn();
    }

    internal void ValidateDsn()
    {
        if (string.IsNullOrWhiteSpace(Options.Dsn))
        {
            return;
        }

        if (Uri.IsWellFormedUriString(Options.Dsn, UriKind.Absolute))
        {
            return;
        }

        var fullFieldName = $"{nameof(Options)}.{nameof(Options.Dsn)}";
        var validationError = new ValidationError(fullFieldName, "Invalid DSN format. Expected a URL.");
        OnValidationError(validationError);

        _logger.LogWarning(validationError.ToString());
    }

    internal static void SetTitle(EditorWindow window, string title = "Sentry", string description = "Sentry SDK options")
    {
        var isDarkMode = EditorGUIUtility.isProSkin;
        var texture = new Texture2D(16, 16);
        using var memStream = new MemoryStream();
        using var stream = window.GetType().Assembly
            .GetManifestResourceStream(
                $"Sentry.Unity.Editor.Resources.SentryLogo{(isDarkMode ? "Light" : "Dark")}.png");
        stream.CopyTo(memStream);
        stream.Flush();
        memStream.Position = 0;
        texture.LoadImage(memStream.ToArray());

        window.titleContent = new GUIContent(title, texture, description);
    }
}

public readonly struct ValidationError
{
    public readonly string PropertyName;

    public readonly string Reason;

    public ValidationError(string propertyName, string reason)
    {
        PropertyName = propertyName;
        Reason = reason;
    }

    public override string ToString()
        => $"[{PropertyName}] Reason: {Reason}";
}
