using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal class Wizard
    {
        // can't be static
        private int ProjectSelected = 0;
        private int OrgSelected = 0;
        internal WizardStep2Response? Response { get; set; }
        private WizardConfiguration? WizardConfiguration { get; set; }
        private IDiagnosticLogger _logger;
        private WizardTask? _task;

        public Wizard(IDiagnosticLogger logger)
        {
            _logger = logger;
        }

        public WizardConfiguration? Show()
        {
            if (WizardConfiguration is null)
            {
                if (Response is not null)
                {
                    var firstEntry = new string('-', 60);
                    var orgsAndProjects = Response.Projects.GroupBy(k => k.Organization!.Name, v => v).ToArray();
                    if (orgsAndProjects.Length == 1)
                    {
                        OrgSelected = 1;
                        ProjectSelected = EditorGUILayout.Popup("Select the Sentry project", ProjectSelected, Response.Projects.Select(p => p.Slug)
                            .Prepend(firstEntry).ToArray());
                    }
                    else
                    {
                        if (OrgSelected == 0)
                        {
                            OrgSelected = EditorGUILayout.Popup("Select Sentry Organization", OrgSelected, orgsAndProjects.Select(k => k.Key)
                                .Prepend(firstEntry).ToArray());
                        }
                        else
                        {
                            ProjectSelected = EditorGUILayout.Popup("Select Sentry Project", ProjectSelected, orgsAndProjects[OrgSelected - 1].Select(v => $"{v.Name} - ({v.Slug})").ToArray()
                                .Prepend(firstEntry).ToArray());
                        }
                    }

                    if (ProjectSelected != 0)
                    {
                        var proj = orgsAndProjects[OrgSelected - 1].ToArray()[ProjectSelected - 1];
                        WizardConfiguration = new WizardConfiguration
                        {
                            Token = Response.ApiKeys!.Token,
                            Dsn = proj.Keys.First().Dsn!.Public,
                            OrgSlug = proj.Organization!.Slug,
                            ProjectSlug = proj.Slug
                        };
                        return WizardConfiguration;
                    }
                }
                else if (GUILayout.Button("Start setup wizard for sentry.io") && _task is null)
                {
                    _task = new WizardTask(this, _logger);
                    Task.Run(_task.Run);
                }
            }

            return WizardConfiguration;
        }

        // called multiple times per second to update status on the UI thread.
        internal void Update()
        {
            if (_task is null)
            {
                return;
            }

            if (_task._done || _task._cancelled)
            {
                _task = null;
                EditorUtility.ClearProgressBar();
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
            // Verify that the URL is a valid HTTP/HTTPS - we don't want to launch just about any process ()
            var parsedUri = new Uri(url);
            if (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new Exception($"Can't open given URL - only HTTP/HTTPS scheme is allowed, but got: {url}");
            }

            Application.OpenURL(parsedUri.ToString());
        }

        internal class WizardStep1Response
        {
            public string? Hash { get; set; }
        }
        internal class WizardStep2Response
        {
            public ApiKeys? ApiKeys { get; set; }
            public IList<Project> Projects { get; set; } = new List<Project>(0);
        }

        internal class ApiKeys
        {
            public string? Token { get; set; }
        }

        internal class Project
        {
            public Organization? Organization { get; set; }
            public string? Slug { get; set; }
            public string? Name { get; set; }
            public IEnumerable<Key> Keys { get; set; } = Enumerable.Empty<Key>();
        }

        internal class Key
        {
            public Dsn? Dsn { get; set; }
        }

        internal class Dsn
        {
            public string? Public { get; set; }
        }

        internal class Organization
        {
            public string? Name { get; set; }
            public string? Slug { get; set; }
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

    internal class WizardTask
    {
        private Wizard _wizard;
        private IDiagnosticLogger _logger;
        internal float _progress = 0.0f;
        internal string _progressText = "";
        internal bool _done = false;
        internal bool _cancelled = false;
        internal Action? _uiAction = null;
        private const int StepCount = 5;

        private readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public WizardTask(Wizard wizard, IDiagnosticLogger logger)
        {
            _wizard = wizard;
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

        internal async void Run()
        {
            try
            {
                Progress("Started", 1);

                Progress("Connecting to sentry.io settings wizard...", 2);
                var http = new HttpClient();
                var resp = await http.GetAsync("https://sentry.io/api/0/wizard/").ConfigureAwait(false);
                var wizardHashResponse = await DeserializeJson<Wizard.WizardStep1Response>(resp);

                Progress("Opening sentry.io in the default browser...", 3);
                await RunOnUiThread(() => Wizard.OpenUrl($"https://sentry.io/account/settings/wizard/{wizardHashResponse.Hash}/"));

                // Poll https://sentry.io/api/0/wizard/hash/
                var pollingUrl = $"https://sentry.io/api/0/wizard/{wizardHashResponse.Hash}/";

                Progress("Waiting for the the response from the browser session...", 4);

                while (!_cancelled)
                {
                    try
                    {
                        resp = await http.GetAsync(pollingUrl).ConfigureAwait(false);
                        if (resp.StatusCode != HttpStatusCode.BadRequest) // not ready yet
                        {
                            var response = await DeserializeJson<Wizard.WizardStep2Response>(resp).ConfigureAwait(false);
                            // Set on UI thread to make sure it's serialized properly with other UI updates.
                            await RunOnUiThread(() => _wizard.Response = response);
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
                Done("Failed");
            }
            finally
            {
                _done = true;
            }
        }

        private async Task<T> DeserializeJson<T>(HttpResponseMessage response)
        {
            var content = await response.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(content, _serializeOptions)!;
        }

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
}
