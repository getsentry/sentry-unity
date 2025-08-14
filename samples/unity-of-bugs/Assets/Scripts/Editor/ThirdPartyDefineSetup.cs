#if UNITY_EDITOR
using UnityEditor;
using System.Linq;

[InitializeOnLoad]
public static class ThirdPartyDefineSetup
{
    private const string HasUniTask = "SENTRY_HAS_UNITASK";
    private const string HasDoTween = "SENTRY_HAS_DOTWEEN";

    static ThirdPartyDefineSetup()
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
#pragma warning disable CS0618
        var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
#pragma warning restore CS0618
        var defines = currentDefines.Split(';').ToList();

        var hasUniTask = System.AppDomain.CurrentDomain.GetAssemblies()
            .Any(assembly => assembly.GetName().Name == "UniTask");

        var hasDoTween = System.AppDomain.CurrentDomain.GetAssemblies()
            .Any(assembly => assembly.GetName().Name.Contains("DOTween"));

        var modified = false;

        switch (hasUniTask)
        {
            case true when !defines.Contains(HasUniTask):
                defines.Add(HasUniTask);
                modified = true;
                break;
            case false when defines.Contains(HasUniTask):
                defines.Remove(HasUniTask);
                modified = true;
                break;
        }

        switch (hasDoTween)
        {
            case true when !defines.Contains(HasDoTween):
                defines.Add(HasDoTween);
                modified = true;
                break;
            case false when defines.Contains(HasDoTween):
                defines.Remove(HasDoTween);
                modified = true;
                break;
        }

        if (!modified)
        {
            return;
        }

        var newDefines = string.Join(";", defines.Where(d => !string.IsNullOrEmpty(d)));
        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, newDefines);
    }
}
#endif
