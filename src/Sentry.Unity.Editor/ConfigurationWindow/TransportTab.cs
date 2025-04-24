using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class TransportTab
{
    internal static void Display(ScriptableSentryUnityOptions options)
    {
        {
            options.EnableOfflineCaching = EditorGUILayout.BeginToggleGroup(
            new GUIContent("Enable Offline Caching", "Whether the Sentry SDK will persist events locally " +
                        "\nbefore sending them to Sentry. This helps in cases where internet connectivity is limited."),
            options.EnableOfflineCaching);
            EditorGUI.indentLevel++;

            options.MaxCacheItems = EditorGUILayout.IntField(
                new GUIContent("Max Cache Items", "The maximum number of files to keep in the disk cache. " +
                                                  "The SDK deletes the oldest when the limit is reached.\nDefault: 30"),
                options.MaxCacheItems);
            options.MaxCacheItems = Math.Max(0, options.MaxCacheItems);

            options.InitCacheFlushTimeout = EditorGUILayout.IntField(
                new GUIContent("Init Flush Timeout [ms]", "The timeout that limits how long the SDK " +
                                                          "will attempt to flush existing cache during initialization, " +
                                                          "potentially slowing down app start up to the specified time." +
                                                          "\nThis features allows capturing errors that happen during " +
                                                          "game startup and would not be captured because the process " +
                                                          "would be killed before Sentry had a chance to capture the event."),
                options.InitCacheFlushTimeout);
            options.InitCacheFlushTimeout = Math.Max(0, options.InitCacheFlushTimeout);

            EditorGUILayout.EndToggleGroup();

            options.ShutdownTimeout = EditorGUILayout.IntField(
                new GUIContent("Shutdown Timeout [ms]", "How many milliseconds to wait before shutting down to " +
                                                         "give Sentry time to send events from the background queue."),
                options.ShutdownTimeout);
            options.ShutdownTimeout = Mathf.Clamp(options.ShutdownTimeout, 0, int.MaxValue);

            options.MaxQueueItems = EditorGUILayout.IntField(
                new GUIContent("Max Queue Items", "The maximum number of events to keep in memory while " +
                                                  "the worker attempts to send them."),
                options.MaxQueueItems
            );
            options.MaxQueueItems = Math.Max(0, options.MaxQueueItems);
        }

        EditorGUI.indentLevel--;
        // Options.RequestBodyCompressionLevel = (CompressionLevelWithAuto)EditorGUILayout.EnumPopup(
        //     new GUIContent("Compress Payload", "The level of which to compress the Sentry event " +
        //                                        "before sending to Sentry."),
        //     Options.RequestBodyCompressionLevel);
    }
}
