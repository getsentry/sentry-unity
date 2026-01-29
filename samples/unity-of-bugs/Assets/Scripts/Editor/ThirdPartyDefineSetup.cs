#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Editor
{
    [InitializeOnLoad]
    public static class ThirdPartyDefineSetup
    {
        private const string HasUniTask = "SENTRY_HAS_UNITASK";
        private const string HasDoTween = "SENTRY_HAS_DOTWEEN";

        static ThirdPartyDefineSetup()
        {
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;

            var currentDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(target));
            var defines = currentDefines.Split(';').ToList();

            var currentAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            var hasUniTask = currentAssemblies.Any(assembly => assembly.GetName().Name == "UniTask");
            var hasDoTween = currentAssemblies.Any(assembly => assembly.GetName().Name.Contains("DOTween"));

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
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(target), newDefines);

        }
    }
}
#endif
