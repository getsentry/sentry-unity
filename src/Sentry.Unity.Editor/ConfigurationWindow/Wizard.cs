using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Unity.Editor.WizardApi;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal class Wizard : EditorWindow
{
    private int _projectSelected = 0;
    private int _orgSelected = 0;

    private static Wizard? Instance;
    internal WizardStep2Response? Response { get; set; }
    private IDiagnosticLogger _logger = null!;
    private WizardLoader? _task;

    public static void Start(IDiagnosticLogger logger)
    {
        if (Instance is null)
        {
            Instance = CreateInstance<Wizard>();
            Instance._logger = logger;

            SentryWindow.SetTitle(Instance, description: "Setup wizard");

            Instance.ShowUtility();
            Instance.minSize = new Vector2(600, 200);
            Instance.StartLoader();
        }
    }

    private void StartLoader()
    {
        _task = new WizardLoader(_logger);
        Task.Run(async () => Response = await _task.Load()).ContinueWith(t =>
        {
            if (t.Exception is not null)
            {
                _logger.Log(SentryLevel.Warning, "Wizard loader failed", t.Exception);
            }
        });
    }

    public static bool InProgress => Instance is not null;

    private void OnGUI()
    {
        if (Response is null)
        {
            return;
        }

        WizardConfiguration? wizardConfiguration = null;

        EditorGUILayout.Space();

        if (Response.projects.Count == 0)
        {
            wizardConfiguration = new WizardConfiguration
            {
                Token = Response.apiKeys!.token
            };

            EditorGUILayout.LabelField("There don't seem to be any projects in your sentry.io account.");
        }
        else
        {
            EditorGUILayout.LabelField("Please select the organization and project you'd like to use.");
            EditorGUILayout.Space();

            var blankEntry = new string(' ', 60);

            // sort "unity" projects first
            Response.projects.Sort((a, b) =>
            {
                if (a.IsUnity == b.IsUnity)
                {
                    return (a.name ?? "").CompareTo(b.name ?? "");
                }
                else if (a.IsUnity)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });

            var orgsAndProjects = Response.projects.GroupBy(k => k.organization!.name, v => v);
            var orgs = orgsAndProjects.Select(k => k.Key).ToArray();
            if (orgs.Length > 1)
            {
                orgs = orgs.Prepend(blankEntry).ToArray();
            }

            _orgSelected = EditorGUILayout.Popup("Organization", _orgSelected, orgs);

            if (orgs.Length == 1 || _orgSelected > 0)
            {
                var projects = orgsAndProjects.Where(k => k.Key == orgs[_orgSelected]).SelectMany(p => p).ToArray();
                var projectNames = projects.Select(v => v.name).ToArray();
                if (projectNames.Length > 1)
                {
                    projectNames = projectNames.Prepend(blankEntry).ToArray();
                }

                _projectSelected = EditorGUILayout.Popup("Project", _projectSelected, projectNames);

                if (projects.Length == 1 || _projectSelected > 0)
                {
                    var project = projects.Where(p => p.name == projectNames[_projectSelected]).ToArray()[0];
                    wizardConfiguration = new WizardConfiguration
                    {
                        Token = Response.apiKeys!.token,
                        Dsn = project.keys.First().dsn!.@public,
                        OrgSlug = project.organization!.slug,
                        ProjectSlug = project.slug,
                    };
                }
            }
        }

        if (wizardConfiguration != null)
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("We have updated the default options with your selection.");

            EditorGUILayout.HelpBox(
                "Sentry options persist in two assets in your project directory:\n" +
                "  Resources/Sentry/SentryOptions.asset contains the main configuration,\n" +
                "  Plugins/Sentry/SentryCliOptions.asset contains the settings for debug symbol upload.",
                MessageType.Info);
            EditorGUILayout.HelpBox(
                "Make sure to keep the SentryCliOptions.asset private because it contains an API authentication token linked to your account.",
                MessageType.Warning);

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Would you like to inspect the settings or leave at defaults for now? ");
            EditorGUILayout.LabelField("You can always make changes later in the Tools/Sentry menu.");
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("I want to see all the options!", GUILayout.ExpandWidth(false)))
            {
                SentryWindow.SaveWizardResult(wizardConfiguration);
                Close();
                SentryWindow.OpenSentryWindow();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("I'm fine with the defaults, take me back to Unity!", GUILayout.ExpandWidth(false)))
            {
                SentryWindow.SaveWizardResult(wizardConfiguration);
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }

    void OnDestroy()
    {
        Instance = null;
    }

    // called multiple times per second to update status on the UI thread.
    internal void Update()
    {
        if (_task is null)
        {
            return;
        }

        if (_task._cancelled)
        {
            EditorUtility.ClearProgressBar();
            Close();
            return;
        }

        if (_task._done)
        {
            EditorUtility.ClearProgressBar();
            if (_task._exception is not null && EditorUtility.DisplayDialog("Wizard error",
                    $"Couldn't launch the wizard. Would you like to try again? The error was:\n\n{_task._exception.Message}",
                    "Retry wizard", "I'll set it up manually"))
            {
                StartLoader();
            }
            return;
        }

        if (_task._progress > 0.0f)
        {
            _task._cancelled = EditorUtility.DisplayCancelableProgressBar(
                "Sentry setup wizard", _task._progressText, _task._progress);
        }

        _task._uiAction?.Invoke();
    }

    internal static void OpenUrl(string url)
    {
        // Verify that the URL is a valid HTTPS - we don't want to launch just about any process (say... malware.exe)
        var parsedUri = new Uri(url);
        if (parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new Exception($"Can't open given URL - only HTTPS scheme is allowed, but got: {url}");
        }

        Application.OpenURL(parsedUri.ToString());
    }
}

internal class WizardConfiguration
{
    public string? Token { get; set; }
    public string? Dsn { get; set; }
    public string? OrgSlug { get; set; }
    public string? ProjectSlug { get; set; }
}

internal class WizardCancelled : Exception
{
    internal WizardCancelled() : base() { }
    internal WizardCancelled(string message) : base(message) { }
    internal WizardCancelled(string message, Exception innerException) : base(message, innerException) { }
}

internal class WizardLoader
{
    private IDiagnosticLogger _logger;
    internal float _progress = 0.0f;
    internal string _progressText = "";
    internal bool _done = false;
    internal bool _cancelled = false;
    internal Exception? _exception = null;
    internal Action? _uiAction = null;
    private const int StepCount = 5;

    public WizardLoader(IDiagnosticLogger logger)
    {
        _logger = logger;
    }

    private void Progress(string status, int step) => Progress(status, (float)step / StepCount);
    private void Done(string status) => Progress(status, 1.0f);

    private void Progress(string status, float current)
    {
        if (_cancelled)
        {
            throw new WizardCancelled();
        }

        _logger.LogDebug("Wizard: {0,3} % - {1}", (int)Math.Floor(100 * current), status);
        _progressText = status;
        _progress = current;
    }

    internal async Task<WizardStep2Response?> Load()
    {
        WizardStep2Response? response = null;
        try
        {
            Progress("Started", 1);

            Progress("Connecting to sentry.io settings wizard...", 2);
            var http = new HttpClient();
            var resp = await http.GetAsync("https://sentry.io/api/0/wizard/").ConfigureAwait(false);
            var wizardHashResponse = await DeserializeJson<WizardStep1Response>(resp);

            Progress("Opening sentry.io in the default browser...", 3);
            await RunOnUiThread(() => Wizard.OpenUrl($"https://sentry.io/account/settings/wizard/{wizardHashResponse.hash}/"));

            // Poll https://sentry.io/api/0/wizard/hash/
            var pollingUrl = $"https://sentry.io/api/0/wizard/{wizardHashResponse.hash}/";

            Progress("Waiting for the the response from the browser session...", 4);

            while (!_cancelled)
            {
                try
                {
                    resp = await http.GetAsync(pollingUrl).ConfigureAwait(false);
                    if (resp.StatusCode != HttpStatusCode.BadRequest) // not ready yet
                    {
                        response = await DeserializeJson<WizardStep2Response>(resp).ConfigureAwait(false);
                        break;
                    }
                }
                catch (Exception e)
                {
                    _logger.Log(SentryLevel.Warning, "Wizard polling error", e);
                }
                await Task.Delay(1000).ConfigureAwait(false);
            }

            await http.DeleteAsync(pollingUrl);
            http.Dispose();
            Done("Finished");
        }
        catch (WizardCancelled)
        {
            _logger.Log(SentryLevel.Info, "Wizard cancelled");
        }
        catch (Exception e)
        {
            _logger.Log(SentryLevel.Warning, "Wizard failed", e);
            _exception = e;
            Done("Failed");
        }
        finally
        {
            _done = true;
        }
        return response;
    }

    private async Task<T> DeserializeJson<T>(HttpResponseMessage response)
    {
        var content = await response.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        return DeserializeJson<T>(System.Text.Encoding.UTF8.GetString(content));
    }

    internal T DeserializeJson<T>(string json) => JsonUtility.FromJson<T>(json);

    private Task RunOnUiThread(Action callback)
    {
        var tcs = new TaskCompletionSource<bool>();
        _uiAction = () =>
        {
            try
            {
                callback.Invoke();
                tcs.TrySetResult(true);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
            finally
            {
                _uiAction = null;
            }
        };
        return tcs.Task;
    }
}
