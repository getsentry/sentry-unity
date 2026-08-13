using System;
using System.Reflection;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Editor;

/// <summary>
/// Provides access to Unity's 'VMUnityLinkerAdditionalArgs' diagnostic switch. Unity feeds its value into the
/// UnityLinker's additional arguments and, unlike the 'UNITYLINKER_ADDITIONAL_ARGS' environment variable, it is part
/// of the Bee build graph's inputs. That means changing it invalidates the cached graph the same way the additional
/// IL2CPP arguments do - without it the linker keeps running with whatever arguments the cached graph was built with.
/// The switch is internal to Unity, so we have to go through reflection to get to it.
/// </summary>
internal static class UnityLinkerDiagnosticSwitch
{
    internal const string SwitchName = "VMUnityLinkerAdditionalArgs";

    public static string? GetValue(IDiagnosticLogger? logger = null)
    {
        var diagnosticSwitch = GetSwitch(logger);
        if (diagnosticSwitch is null)
        {
            return null;
        }

        try
        {
            return diagnosticSwitch.GetType().GetProperty("value")?.GetValue(diagnosticSwitch) as string;
        }
        catch (Exception e)
        {
            logger?.LogWarning("Failed to read the '{0}' diagnostic switch. Reason: {1}", SwitchName, e.Message);
            return null;
        }
    }

    public static bool SetValue(string value, IDiagnosticLogger? logger = null)
    {
        var diagnosticSwitch = GetSwitch(logger);
        if (diagnosticSwitch is null)
        {
            return false;
        }

        try
        {
            var property = diagnosticSwitch.GetType().GetProperty("value");
            if (property is null)
            {
                logger?.LogWarning("Failed to resolve the value of the '{0}' diagnostic switch.", SwitchName);
                return false;
            }

            property.SetValue(diagnosticSwitch, value);

            // Reading the value straight back so we can tell a failed write apart from one that Unity does not pick up.
            logger?.LogDebug("Set '{0}' to '{1}'. It now reads back as '{2}'.",
                SwitchName, value, property.GetValue(diagnosticSwitch));

            return true;
        }
        catch (Exception e)
        {
            logger?.LogWarning("Failed to set the '{0}' diagnostic switch. Reason: {1}", SwitchName, e.Message);
            return false;
        }
    }

    private static object? GetSwitch(IDiagnosticLogger? logger)
    {
        try
        {
            var method = typeof(Debug).GetMethod("GetDiagnosticSwitch", BindingFlags.Static | BindingFlags.NonPublic);
            if (method is null)
            {
                logger?.LogWarning("Failed to resolve 'Debug.GetDiagnosticSwitch'.");
                return null;
            }

            var diagnosticSwitch = method.Invoke(null, new object[] { SwitchName });
            if (diagnosticSwitch is null)
            {
                logger?.LogWarning("The diagnostic switch '{0}' does not exist.", SwitchName);
            }

            return diagnosticSwitch;
        }
        catch (Exception e)
        {
            logger?.LogWarning("Failed to access the '{0}' diagnostic switch. Reason: {1}", SwitchName, e.Message);
            return null;
        }
    }
}
