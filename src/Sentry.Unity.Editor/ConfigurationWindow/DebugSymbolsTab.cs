using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class DebugSymbolsTab
{
    /// <summary>
    /// sentry-cli needs an organization unless the auth token is an organization token or SENTRY_ORG is set.
    /// Only drives hints in this window; the upload itself leaves the decision to sentry-cli.
    /// </summary>
    internal static bool IsOrganizationMissing(SentryCliOptions cliOptions) =>
        string.IsNullOrWhiteSpace(cliOptions.Organization)
        && cliOptions.Auth?.Trim().StartsWith("sntrys_", StringComparison.Ordinal) is not true
        && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SENTRY_ORG"));

    internal static void Display(SentryCliOptions cliOptions)
    {
        cliOptions.UploadSymbols = EditorGUILayout.BeginToggleGroup(
            new GUIContent("Upload Symbols", "Whether debug symbols should be uploaded automatically " +
                                             "on release builds."),
            cliOptions.UploadSymbols);
        EditorGUI.indentLevel++;

        cliOptions.UploadDevelopmentSymbols = EditorGUILayout.Toggle(
            new GUIContent("Upload Dev Symbols", "Whether debug symbols should be uploaded automatically " +
                                                 "on development builds."),
            cliOptions.UploadDevelopmentSymbols);

        cliOptions.UploadSources = EditorGUILayout.Toggle(
            new GUIContent("Upload Sources", "Whether your source code should be uploaded to Sentry, so the stack trace in Sentry has the relevant code next to it."),
            cliOptions.UploadSources);

        cliOptions.IgnoreCliErrors = EditorGUILayout.Toggle(
            new GUIContent("Ignore CLI Errors", "Whether to ignore the Sentry CLI errors during the build. If this is ON, the errors will be logged, but the build will succeed."),
            cliOptions.IgnoreCliErrors);

        EditorGUILayout.EndToggleGroup();

        cliOptions.Auth = EditorGUILayout.TextField(
            new GUIContent(
                "Auth Token",
                cliOptions.UploadSymbols && string.IsNullOrWhiteSpace(cliOptions.Auth) ? SentryWindow.ErrorIcon : null,
                "The auth token sentry-cli uses to upload debug symbols, e.g. an organization token from your organization's settings in Sentry"),
            cliOptions.Auth);

        cliOptions.Organization = EditorGUILayout.TextField(
                new GUIContent(
                    "Org Slug",
                    cliOptions.UploadSymbols && IsOrganizationMissing(cliOptions) ? SentryWindow.ErrorIcon : null,
                    "The organization slug in Sentry. Required unless the Auth Token is an organization token."),
                cliOptions.Organization);

        cliOptions.Project = EditorGUILayout.TextField(
            new GUIContent(
                "Project Name",
                cliOptions.UploadSymbols && string.IsNullOrWhiteSpace(cliOptions.Project) ? SentryWindow.ErrorIcon : null,
                "The project name in Sentry"),
            cliOptions.Project);

        EditorGUILayout.Space();

        cliOptions.CliOptionsConfiguration = OptionsConfigurationItem.Display(
            cliOptions.CliOptionsConfiguration,
            "CLI Config Script",
            "SentryCliConfiguration",
            "A scriptable object that inherits from 'SentryCliOptionsConfiguration'." +
            "It allows you to programmatically modify the options used during debug symbols upload."
        );

        EditorGUI.indentLevel--;
    }
}
