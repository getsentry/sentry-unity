using System;
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
            if (Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                Assert.Inconclusive("Skipping: Non MacOSX");
            }

            const string testOptionsFileName = "testOptions.m";
            var nativeOptions = new NativeOptions();
            var nativeOptionsString = nativeOptions.Generate(new SentryOptions());
            File.WriteAllText(testOptionsFileName, nativeOptionsString);

            var process = Process.Start("clang", $"-fsyntax-only {testOptionsFileName}");
            process.WaitForExit();

            Assert.AreEqual(0, process.ExitCode);

            File.Delete(testOptionsFileName);
        }

        [Test]
        public void GenerateOptions_NewSentryOptionsGarbageAppended_FailsToCompile()
        {
            if (Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                Assert.Inconclusive("Skipping: Non MacOSX");
            }

            const string testOptionsFileName = "testOptions.m";
            var nativeOptions = new NativeOptions();
            var nativeOptionsString = nativeOptions.Generate(new SentryOptions());
            nativeOptionsString += "AppendedTextToFailCompilation";

            File.WriteAllText(testOptionsFileName, nativeOptionsString);

            var process = Process.Start("clang", $"-fsyntax-only -framework Foundation {testOptionsFileName}");
            process.WaitForExit();

            Assert.AreEqual(1, process.ExitCode);

            File.Delete(testOptionsFileName);
        }

        [Test]
        public void CreateOptionsFile_NewSentryOptions_FileCreated()
        {
            const string testOptionsFileName = "testOptions.m";
            var nativeOptions = new NativeOptions();

            nativeOptions.CreateFile(testOptionsFileName, new SentryOptions());

            Assert.IsTrue(File.Exists(testOptionsFileName));

            File.Delete(testOptionsFileName);
        }
    }
}
