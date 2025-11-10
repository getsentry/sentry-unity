using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Sentry.Extensibility;
using Sentry.Unity.Editor.ConfigurationWindow;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Sentry.Unity.Editor.Android;

// https://github.com/getsentry/sentry-java/blob/d3764bfc97eed22564a1e23ba96fa73ad2685498/sentry-android-core/src/main/java/io/sentry/android/core/ManifestMetadataReader.java#L83-L217
public class PostGenerateGradleAndroidProject : IPostGenerateGradleAndroidProject
{
    public int callbackOrder
    {
        get
        {
            var result = 1;

            var options = SentryScriptableObject.LoadOptions();
            if (options != null)
            {
                result = options.PostGenerateGradleProjectCallbackOrder;
            }

            return result;
        }
    }

    public void OnPostGenerateGradleAndroidProject(string basePath)
    {
        var androidManifestConfiguration = new AndroidManifestConfiguration();
        androidManifestConfiguration.OnPostGenerateGradleAndroidProject(basePath);
    }
}

public class AndroidManifestConfiguration
{
    private readonly SentryUnityOptions? _options;
    private readonly SentryCliOptions? _sentryCliOptions;
    private readonly IDiagnosticLogger _logger;

    private readonly bool _isDevelopmentBuild;
    private readonly ScriptingImplementation _scriptingImplementation;

    public AndroidManifestConfiguration()
        : this(
            SentryScriptableObject.LoadOptions,
            SentryScriptableObject.LoadCliOptions,
            isDevelopmentBuild: EditorUserBuildSettings.development,
#pragma warning disable CS0618
            scriptingImplementation: PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android))
#pragma warning restore CS0618
    { }

    // Testing
    internal AndroidManifestConfiguration(
        Func<bool, SentryUnityOptions?> getOptions,
        Func<SentryCliOptions?> getCliOptions,
        bool isDevelopmentBuild,
        ScriptingImplementation scriptingImplementation,
        ILogger? logger = null)
    {
        _options = getOptions(true);
        _sentryCliOptions = getCliOptions();
        _logger = _options?.DiagnosticLogger ?? new UnityLogger(_options ?? new SentryUnityOptions(), logger);

        _isDevelopmentBuild = isDevelopmentBuild;
        _scriptingImplementation = scriptingImplementation;
    }

    public void OnPostGenerateGradleAndroidProject(string basePath)
    {
        if (_options is null)
        {
            _logger.LogWarning("Android native support disabled because Sentry has not been configured. " +
                              "You can do that through the editor: {0}", SentryWindow.EditorMenuPath);
            return;
        }

        if (_scriptingImplementation != ScriptingImplementation.IL2CPP)
        {
            if (_options is { AndroidNativeSupportEnabled: true })
            {
                _logger.LogWarning("Android native support requires IL2CPP scripting backend and is currently unsupported on Mono.");
            }

            return;
        }

        ModifyManifest(basePath);

        var unityProjectPath = Directory.GetParent(Application.dataPath).FullName;
        var gradleProjectPath = Directory.GetParent(basePath).FullName;

        CopyAndroidSdkToGradleProject(unityProjectPath, gradleProjectPath);
        AddAndroidSdkDependencies(gradleProjectPath);

        if (_sentryCliOptions?.IgnoreCliErrors is true)
        {
            _logger.LogWarning("Sentry CLI errors will be ignored during build. BE AWARE you might have " +
                               "unminified/unsymbolicated crashes in production if the debug symbol upload fails. " +
                               "When using this flag, you should store built sourcemaps and debug files, to re-run the " +
                               "upload symbols command at a later point.");
        }

        SetupSymbolsUpload(unityProjectPath, gradleProjectPath);
        SetupProguard(gradleProjectPath);
    }

    internal void ModifyManifest(string basePath)
    {
        var manifestPath = GetManifestPath(basePath);
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Can't configure native Android SDK nor set auto-init:false.",
                manifestPath);
        }

        var enableNativeSupport = true;
        if (_options is null)
        {
            _logger.LogWarning("Android native support disabled because Sentry has not been configured. " +
                               "You can do that through the editor: {0}", SentryWindow.EditorMenuPath);
            enableNativeSupport = false;
        }
        else if (!_options.IsValid())
        {
            _logger.LogDebug("Android native support disabled.");
            enableNativeSupport = false;
        }
        else if (!_options.AndroidNativeSupportEnabled)
        {
            _logger.LogDebug("Android native support disabled through the options.");
            enableNativeSupport = false;
        }

        var androidManifest = new AndroidManifest(manifestPath, _logger);
        androidManifest.RemovePreviousConfigurations();

        if (!enableNativeSupport)
        {
            return;
        }

        androidManifest.AddDisclaimerComment();

        if (_options?.AndroidNativeInitializationType is NativeInitializationType.Runtime)
        {
            _logger.LogDebug("Setting 'auto-init' to 'false'. The Android SDK will be initialized at runtime.");
            androidManifest.SetAutoInit(false);
            _ = androidManifest.Save();

            return;
        }

        _logger.LogInfo("Adding Sentry options to the AndroidManifest.");
        _logger.LogDebug("Modifying AndroidManifest: {0}", basePath);

        androidManifest.SetSDK("sentry.java.android.unity");
        _logger.LogDebug("Setting DSN: {0}", _options!.Dsn);
        androidManifest.SetDsn(_options.Dsn!);

        if (_options.Debug)
        {
            _logger.LogDebug("Setting Debug: {0}", _options.Debug);
            androidManifest.SetDebug(_options.Debug);
        }

        if (_options.Release is not null)
        {
            _logger.LogDebug("Setting Release: {0}", _options.Release);
            androidManifest.SetRelease(_options.Release);
        }

        if (_options.Environment is not null)
        {
            _logger.LogDebug("Setting Environment: {0}", _options.Environment);
            androidManifest.SetEnvironment(_options.Environment);
        }

        _logger.LogDebug("Setting DiagnosticLevel: {0}", _options.DiagnosticLevel);
        androidManifest.SetLevel(_options.DiagnosticLevel);

        if (_options.SampleRate.HasValue)
        {
            // To keep the logs in line with what the SDK writes to the AndroidManifest we're formatting here too
            _logger.LogDebug("Setting SampleRate: {0}", ((float)_options.SampleRate).ToString("F", CultureInfo.InvariantCulture));
            androidManifest.SetSampleRate(_options.SampleRate.Value);
        }

        // TODO: Missing on AndroidManifest
        // _logger.LogDebug("Setting MaxBreadcrumbs: {0}", options.MaxBreadcrumbs);
        // androidManifest.SetMaxBreadcrumbs(options.MaxBreadcrumbs);
        // _logger.LogDebug("Setting MaxCacheItems: {0}", options.MaxCacheItems);
        // androidManifest.SetMaxCacheItems(options.MaxCacheItems);
        // _logger.LogDebug("Setting SendDefaultPii: {0}", options.SendDefaultPii);
        // // androidManifest.SetSendDefaultPii(options.SendDefaultPii);

        // Note: doesn't work - produces a blank (white) screenshot
        // _logger.LogDebug("Setting AttachScreenshot: {0}", _options.AttachScreenshot);
        // androidManifest.SetAttachScreenshot(_options.AttachScreenshot);
        androidManifest.SetAttachScreenshot(false);

        // Disabling the native in favor of the C# layer for now
        androidManifest.SetNdkEnabled(_options.NdkIntegrationEnabled);
        androidManifest.SetNdkScopeSync(_options.NdkScopeSyncEnabled);
        androidManifest.SetAutoTraceIdGeneration(false);
        androidManifest.SetAutoSessionTracking(false);
        androidManifest.SetAutoAppLifecycleBreadcrumbs(false);
        androidManifest.SetAnr(false);
        androidManifest.SetPersistentScopeObserver(false);
        // Disable user interaction tracking to prevent conflicts with VR platforms (e.g., Oculus InputHooks)
        androidManifest.SetEnableUserInteractionBreadcrumbs(false);
        androidManifest.SetEnableUserInteractionTracing(false);

        // TODO: All SentryOptions and create specific Android options

        _ = androidManifest.Save();
    }

    internal void CopyAndroidSdkToGradleProject(string unityProjectPath, string gradlePath)
    {
        var androidSdkPath = Path.Combine(unityProjectPath, "Packages", SentryPackageInfo.GetName(), "Plugins", "Android", "Sentry~");
        var targetPath = Path.Combine(gradlePath, "unityLibrary", "libs");

        if (_options is { Enabled: true, AndroidNativeSupportEnabled: true })
        {
            if (!Directory.Exists(androidSdkPath))
            {
                throw new DirectoryNotFoundException($"Failed to find the Android SDK at '{androidSdkPath}'.");
            }

            Directory.CreateDirectory(targetPath);

            _logger.LogInfo("Copying the Android SDK to '{0}'.", gradlePath);
            foreach (var sourceFileName in Directory.GetFiles(androidSdkPath))
            {
                var fileName = Path.GetFileName(sourceFileName);
                var destFileName = Path.Combine(targetPath, fileName);
                _logger.LogDebug("Copying '{0}' to '{1}'", fileName, destFileName);

                File.Copy(sourceFileName, destFileName, overwrite: true);
            }
        }
        else
        {
            _logger.LogInfo("Removing the Android SDK from the output project.");
            foreach (var file in Directory.GetFiles(androidSdkPath))
            {
                var fileToDelete = Path.Combine(targetPath, Path.GetFileName(file));
                if (File.Exists(fileToDelete))
                {
                    File.Delete(fileToDelete);
                }
            }
        }
    }

    internal void AddAndroidSdkDependencies(string gradleProjectPath)
    {
        var tool = new GradleSetup(_logger, gradleProjectPath);
        var nativeSupportEnabled = _options is { Enabled: true, AndroidNativeSupportEnabled: true };

        try
        {
            if (nativeSupportEnabled)
            {
                tool.UpdateGradleProject();
            }
            else
            {
                tool.ClearGradleProject();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to {(nativeSupportEnabled ? "add" : "remove")} Android Dependencies in the gradle project");
        }
    }

    internal void SetupSymbolsUpload(string unityProjectPath, string gradleProjectPath)
    {
        var disableSymbolsUpload = false;
        var isExporting = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        _logger.LogInfo("The project is exporting: '{0}'", isExporting);

        var symbolsUpload = new DebugSymbolUpload(_logger, _sentryCliOptions, unityProjectPath, gradleProjectPath, isExporting, AndroidUtils.ShouldUploadMapping());

        if (_options is not { Enabled: true, AndroidNativeSupportEnabled: true })
        {
            disableSymbolsUpload = true;
        }

        if (_sentryCliOptions is null)
        {
            _logger.LogWarning("Failed to load sentry-cli options.");
            disableSymbolsUpload = true;
        }
        else if (!_sentryCliOptions.IsValid(_logger, _isDevelopmentBuild))
        {
            disableSymbolsUpload = true;
        }

        if (disableSymbolsUpload)
        {
            symbolsUpload.RemoveUploadFromGradleFile();

            if (_options is { Enabled: true, Il2CppLineNumberSupportEnabled: true })
            {
                _logger.LogWarning("The IL2CPP line number support requires the debug symbol upload to be enabled.");
            }

            return;
        }

        try
        {
            _logger.LogInfo("Adding automated debug symbols upload to the gradle project.");

            // TODO this currently copies the CLI for the current platform, thus making the exported project only
            // build properly on the same platform as it was exported from (Linux->Linux, Windows->Windows, etc.).
            // In practice, users should be able to build the project on any platform, regardless of where Unity
            // ran. In that case, we would either need to include all CLI binaries and switch in Gradle, or let
            // gradle download CLI on demand (relevant code could be taken from sentry-java repo?).
            var launcherDirectory = Path.Combine(gradleProjectPath, "launcher");
            var sentryCliPath = SentryCli.SetupSentryCli(isExporting ? launcherDirectory : null);

            SentryCli.CreateSentryProperties(launcherDirectory, _sentryCliOptions!, _options!);

            symbolsUpload.TryCopySymbolsToGradleProject();
            // We need to remove the old upload task first as the project persists across consecutive builds and the
            // cli options might have changed
            symbolsUpload.RemoveUploadFromGradleFile();
            symbolsUpload.AppendUploadToGradleFile(sentryCliPath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add the automatic symbols upload to the gradle project");
        }
    }

    private void SetupProguard(string gradleProjectPath)
    {
        var tool = new ProguardSetup(_logger, gradleProjectPath);
        var nativeSupportEnabled = _options is { Enabled: true, AndroidNativeSupportEnabled: true };

        try
        {
            if (nativeSupportEnabled)
            {
                tool.AddToGradleProject();
            }
            else
            {
                tool.RemoveFromGradleProject();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to {(nativeSupportEnabled ? "add" : "remove")} Proguard rules in the gradle project");
        }
    }

    internal static string GetManifestPath(string basePath) =>
        new StringBuilder(basePath)
            .Append(Path.DirectorySeparatorChar)
            .Append("src")
            .Append(Path.DirectorySeparatorChar)
            .Append("main")
            .Append(Path.DirectorySeparatorChar)
            .Append("AndroidManifest.xml")
            .ToString();


}

internal class AndroidXmlDocument : XmlDocument
{
    private readonly string _path;
    protected const string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
    protected const string AndroidNsPrefix = "android";

    protected AndroidXmlDocument(string path)
    {
        _path = path;
        using (var reader = new XmlTextReader(_path))
        {
            _ = reader.Read();
            Load(reader);
        }

        var nsManager = new XmlNamespaceManager(NameTable);
        nsManager.AddNamespace("android", AndroidXmlNamespace);
    }

    public string Save() => SaveAs(_path);

    private string SaveAs(string path)
    {
        using var writer = new XmlTextWriter(path, new UTF8Encoding(false)) { Formatting = Formatting.Indented };
        Save(writer);
        return path;
    }
}

internal class AndroidManifest : AndroidXmlDocument
{
    private const string SentryPrefix = "io.sentry";
    private const string Disclaimer = "GENERATED BY SENTRY. Changes to the Sentry options will be lost!";

    private readonly XmlElement _applicationElement;
    private readonly IDiagnosticLogger _logger;

    public AndroidManifest(string path, IDiagnosticLogger logger) : base(path)
    {
        _applicationElement = (XmlElement)SelectSingleNode("/manifest/application");
        _logger = logger;
    }

    internal void RemovePreviousConfigurations()
    {
        var nodesToRemove = new List<XmlNode>();

        foreach (XmlNode node in _applicationElement.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Comment && node.Value.Equals(Disclaimer))
            {
                nodesToRemove.Add(node);
                continue;
            }

            if (node.Name.Equals("meta-data"))
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    if (attr.Prefix.Equals(AndroidNsPrefix) && attr.LocalName.Equals("name") &&
                        attr.Value.StartsWith(SentryPrefix))
                    {
                        _logger.LogDebug("Removing AndroidManifest meta-data '{0}'", attr.Value);
                        nodesToRemove.Add(node);
                        break;
                    }
                }
            }
        }

        foreach (var node in nodesToRemove)
        {
            _ = node.ParentNode.RemoveChild(node);
        }
    }

    public void AddDisclaimerComment() =>
        _applicationElement.AppendChild(_applicationElement.OwnerDocument.CreateComment(Disclaimer));

    internal void SetAutoInit(bool enableAutoInit)
        => SetMetaData($"{SentryPrefix}.auto-init", enableAutoInit.ToString());

    internal void SetDsn(string dsn) => SetMetaData($"{SentryPrefix}.dsn", dsn);

    internal void SetSampleRate(float sampleRate) =>
        // Keeping the sample-rate as float: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
        SetMetaData($"{SentryPrefix}.sample-rate", sampleRate.ToString("F", CultureInfo.InvariantCulture));

    internal void SetRelease(string release) => SetMetaData($"{SentryPrefix}.release", release);

    internal void SetAttachScreenshot(bool value) => SetMetaData($"{SentryPrefix}.attach-screenshot", value.ToString());

    internal void SetEnvironment(string environment) => SetMetaData($"{SentryPrefix}.environment", environment);

    internal void SetSDK(string name) => SetMetaData($"{SentryPrefix}.sdk.name", name);

    internal void SetAutoSessionTracking(bool enableAutoSessionTracking)
        => SetMetaData($"{SentryPrefix}.auto-session-tracking.enable", enableAutoSessionTracking.ToString());

    public void SetAutoAppLifecycleBreadcrumbs(bool enableAutoAppLifeCycleBreadcrumbs)
        => SetMetaData($"{SentryPrefix}.breadcrumbs.app-lifecycle", enableAutoAppLifeCycleBreadcrumbs.ToString());

    internal void SetAnr(bool enableAnr)
        => SetMetaData($"{SentryPrefix}.anr.enable", enableAnr.ToString());

    internal void SetPersistentScopeObserver(bool enableScopePersistence)
        => SetMetaData($"{SentryPrefix}.enable-scope-persistence", enableScopePersistence.ToString());

    internal void SetNdkEnabled(bool enableNdk)
        => SetMetaData($"{SentryPrefix}.ndk.enable", enableNdk.ToString());

    internal void SetNdkScopeSync(bool enableNdkScopeSync)
        => SetMetaData($"{SentryPrefix}.ndk.scope-sync.enable", enableNdkScopeSync.ToString());

    internal void SetAutoTraceIdGeneration(bool enableAutoTraceIdGeneration)
        => SetMetaData($"{SentryPrefix}.traces.enable-auto-id-generation", enableAutoTraceIdGeneration.ToString());

    internal void SetEnableUserInteractionBreadcrumbs(bool enableUserInteractionBreadcrumbs)
        => SetMetaData($"{SentryPrefix}.breadcrumbs.user-interaction", enableUserInteractionBreadcrumbs.ToString());

    internal void SetEnableUserInteractionTracing(bool enableUserInteractionTracing)
        => SetMetaData($"{SentryPrefix}.traces.user-interaction.enable", enableUserInteractionTracing.ToString());

    internal void SetDebug(bool debug) => SetMetaData($"{SentryPrefix}.debug", debug ? "true" : "false");

    // https://github.com/getsentry/sentry-java/blob/db4dfc92f202b1cefc48d019fdabe24d487db923/sentry/src/main/java/io/sentry/SentryLevel.java#L4-L9
    internal void SetLevel(SentryLevel level) =>
        SetMetaData($"{SentryPrefix}.debug.level", level switch
        {
            SentryLevel.Debug => "debug",
            SentryLevel.Error => "error",
            SentryLevel.Fatal => "fatal",
            SentryLevel.Info => "info",
            SentryLevel.Warning => "warning",
            _ => "debug"
        });

    private void SetMetaData(string key, string value)
    {
        var element = _applicationElement.AppendChild(_applicationElement.OwnerDocument
            .CreateElement("meta-data"));
        _ = element.Attributes.Append(CreateAndroidAttribute("name", key));
        _ = element.Attributes.Append(CreateAndroidAttribute("value", value));
    }

    private XmlAttribute CreateAndroidAttribute(string key, string value)
    {
        var attr = CreateAttribute(AndroidNsPrefix, key, AndroidXmlNamespace);
        attr.Value = value;
        return attr;
    }
}
