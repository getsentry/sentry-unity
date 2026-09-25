using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace Sentry.Unity.Editor;

/// <summary>
/// On Mono, `HttpWebRequest` initializes the configuration system, which creates its host and the `machine.config`
/// section types via reflection. The UnityLinker can't see that and strips them on higher stripping levels, breaking
/// the HTTP transport. Unity ignores `link.xml` files inside packages, so we provide the rules at build time instead.
/// </summary>
internal class MonoLinkerProcessor : IUnityLinkerProcessor
{
    internal const string LinkXml =
        """
        <linker>
          <assembly fullname="System.Configuration">
            <type fullname="System.Configuration.ExeConfigurationHost" preserve="all"/>
          </assembly>
          <assembly fullname="System">
            <namespace fullname="System.Net.Configuration" preserve="all"/>
          </assembly>
        </linker>
        """;

    public int callbackOrder => 0;

    public string? GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
    {
        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
        if (PlayerSettings.GetScriptingBackend(namedBuildTarget) != ScriptingImplementation.Mono2x)
        {
            return null;
        }

        var path = Path.Combine(data.inputDirectory, "Sentry.Unity.link.xml");
        File.WriteAllText(path, LinkXml);
        return path;
    }
}
