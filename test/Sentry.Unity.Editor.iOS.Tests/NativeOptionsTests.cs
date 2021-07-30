using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Debug = UnityEngine.Debug;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class NativeOptionsTests
    {
        [Test]
        public void GenerateOptions_NewSentryOptions_Compiles()
        {
            const string testOptionsFileName = "testOptions.m";
            var nativeOptions = SentryNativeOptions.GenerateOptions(new SentryOptions());
            File.WriteAllText(testOptionsFileName, nativeOptions);

            var process = Process.Start("clang", $"-fsyntax-only {testOptionsFileName}");
            process.WaitForExit();

            Assert.AreEqual(0, process.ExitCode);

            File.Delete(testOptionsFileName);
        }

        [Test]
        public void GenerateOptions_NewSentryOptionsGarbageAppended_FailsToCompile()
        {
            const string testOptionsFileName = "testOptions.m";
            var nativeOptions = SentryNativeOptions.GenerateOptions(new SentryOptions());
            nativeOptions += "AppendedTextToFailCompilation";

            File.WriteAllText(testOptionsFileName, nativeOptions);

            var process = Process.Start("clang", $"-fsyntax-only -framework Foundation {testOptionsFileName}");
            process.WaitForExit();

            Assert.AreEqual(1, process.ExitCode);

            File.Delete(testOptionsFileName);
        }
    }
}
