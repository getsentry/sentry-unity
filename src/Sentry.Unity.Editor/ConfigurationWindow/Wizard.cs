using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal class Wizard
    {
        // can't be static
        private int ProjectSelected = 0;
        private int OrgSelected = 0;
        public WizardStep2Response? Response { get; set; }
        public WizardConfiguration? WizardConfiguration { get; set; }

        public async Task<WizardConfiguration?> Show()
        {
            if (WizardConfiguration is not null)
            {
                // We're done with the wizard flow
                return WizardConfiguration;
            }

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
                }
            }
            else if (GUILayout.Button("Start Wizard Process"))
            {
                var serializeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
                };
                var http = new HttpClient();
                var resp = await http.GetAsync("https://sentry.io/api/0/wizard/").ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                var wizardHashResponse = await JsonSerializer.DeserializeAsync<WizardStep1Response>(
                    await resp.Content.ReadAsStreamAsync(), serializeOptions).ConfigureAwait(false);

                var urlToOpenOnBrowser = $"https://sentry.io/account/settings/wizard/{wizardHashResponse!.Hash}/";
                // TODO: On Windows it's 'start' instead of 'open':
                try
                {
                    var proc = Process.Start(
                        new ProcessStartInfo("open", urlToOpenOnBrowser) { UseShellExecute = true });
                }
                catch
                {
                    // TODO: Log here but continue, user can browse manually the URL (see below):
                }

                // TODO: Print URL we just tried to load (can we show a clickable link?) and tell the user to open manually if somehow the
                // browser didn't open

                // Poll https://sentry.io/api/0/wizard/hash/
                var pollingUrl = $"https://sentry.io/api/0/wizard/{wizardHashResponse.Hash}/";
                var i = 0;
                do
                {
                    try
                    {
                        resp = await http.GetAsync(pollingUrl).ConfigureAwait(false);
                        resp.EnsureSuccessStatusCode();
                        var completedWizardResponse = await JsonSerializer.DeserializeAsync<WizardStep2Response>(
                            await resp.Content.ReadAsStreamAsync().ConfigureAwait(false), serializeOptions).ConfigureAwait(false);

                        Response = completedWizardResponse;

                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                } while (i++ < 10);

                // We're done, so fire and forget delete the wizard token and dispose the client at the end
                var deleteTask = http.DeleteAsync(pollingUrl);
                _ = deleteTask.ContinueWith(_ => http.Dispose());
            }

            return null;
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
}
