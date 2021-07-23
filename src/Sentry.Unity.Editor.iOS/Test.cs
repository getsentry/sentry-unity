using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public static class Test
    {
        [MenuItem("Tools/Test")]
        public static void DoTest()
        {
            using StreamWriter sw = File.CreateText(Path.Combine("Assets/Plugins/Sentry", "SentryOptions.txt"));
            var templateLines = File.ReadAllLines("Assets/Plugins/Sentry/Template.txt");
            for (var i = 0; i < templateLines.Length; i++)
            {
                Debug.Log($"{templateLines[i]}");

                if (templateLines[i].Contains("dsn"))
                {
                    sw.WriteLine(templateLines[i].Replace("#", "options.Dsn"));
                    continue;
                }

                if (templateLines[i].Contains("enableAutoSessionTracking"))
                {
                    sw.WriteLine(templateLines[i].Replace("#", "NO"));
                    continue;
                }

                if (templateLines[i].Contains("debug"))
                {
                    sw.WriteLine(templateLines[i].Replace("#", "YES"));
                    continue;
                }

                sw.WriteLine(templateLines[i]);
            }
        }
    }
}
