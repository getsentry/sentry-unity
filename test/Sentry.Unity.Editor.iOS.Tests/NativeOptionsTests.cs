using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class NativeOptionsTests
    {
        [Test]
        public void GenerateOptions_NewSentryOptions_Compiles()
        {
            const string testOptionsFileName = "testOptions.m";
            var sentryNativeOptions = new SentryNativeOptions();
            var nativeOptions = sentryNativeOptions.GenerateOptions(new SentryOptions());
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
            var sentryNativeOptions = new SentryNativeOptions();
            var nativeOptions = sentryNativeOptions.GenerateOptions(new SentryOptions());
            nativeOptions += "AppendedTextToFailCompilation";

            File.WriteAllText(testOptionsFileName, nativeOptions);

            var process = Process.Start("clang", $"-fsyntax-only -framework Foundation {testOptionsFileName}");
            process.WaitForExit();

            Assert.AreEqual(1, process.ExitCode);

            File.Delete(testOptionsFileName);
        }

        [Test]
        public void CreateOptionsFile_NewSentryOptions_FileCreated()
        {
            const string testOptionsFileName = "testOptions.m";
            var sentryNativeOptions = new SentryNativeOptions();

            sentryNativeOptions.CreateOptionsFile(new SentryOptions(), testOptionsFileName);

            Assert.IsTrue(File.Exists(testOptionsFileName));

            File.Delete(testOptionsFileName);
        }
    }
}
