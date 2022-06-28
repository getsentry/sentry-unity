using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sentry.Unity.Editor.Android;
using Sentry.Unity.Tests.Stubs;
using UnityEditor;

namespace Sentry.Unity.Editor.Tests.Android
{
    public class ProguardSetupTests
    {
        private string _fakeProjectPath = null!;
        private string _gradleProjectPath = null!;
        private string _outputPath = null!;

        [SetUp]
        public void SetUp()
        {
            _fakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _gradleProjectPath = Path.Combine(_fakeProjectPath, "GradleProject");
            _outputPath = Path.Combine(_gradleProjectPath, "unityLibrary");
            DebugSymbolUploadTests.SetupFakeProject(_fakeProjectPath);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_fakeProjectPath, true);

        private ProguardSetup GetSut() => new(new UnityLogger(new SentryOptions()), _gradleProjectPath);

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void RemovesRuleFile(bool existsBefore)
        {
            var ruleFile = Path.Combine(_outputPath, ProguardSetup.RuleFileName);

            var sut = GetSut();

            if (existsBefore)
            {
                File.WriteAllText(ruleFile, "");
            }
            Assert.True(File.Exists(ruleFile) == existsBefore);

            sut.RemoveFromGradleProject();

            Assert.False(File.Exists(ruleFile));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void AddsRuleFile(bool existsBefore)
        {
            var ruleFile = Path.Combine(_outputPath, ProguardSetup.RuleFileName);

            var sut = GetSut();

            if (existsBefore)
            {
                File.WriteAllText(ruleFile, "");
            }
            Assert.True(File.Exists(ruleFile) == existsBefore);

            sut.AddToGradleProject();

            Assert.True(File.Exists(ruleFile));
            Assert.GreaterOrEqual(File.ReadAllText(ruleFile).Length, 1);
        }

        [Test]
        public void AddsRuleToGradleScript()
        {
            var gradleScript = Path.Combine(_outputPath, "build.gradle");
            var regex = new Regex($"consumerProguardFiles .*, '{ProguardSetup.RuleFileName}'");

            var sut = GetSut();

            Assert.False(regex.Match(File.ReadAllText(gradleScript)).Success);

            sut.AddToGradleProject();

            Assert.True(regex.Match(File.ReadAllText(gradleScript)).Success);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void RemovesRuleFromGradleScript(bool existsBefore)
        {
            var gradleScript = Path.Combine(_outputPath, "build.gradle");

            var expectedFinalScript = @"
android {
    defaultConfig {
        consumerProguardFiles 'proguard-unity.txt', 'proguard-user.txt', 'other-proguard.txt'
    }
}
";

            File.WriteAllText(gradleScript, existsBefore ? @"
android {
    defaultConfig {
        consumerProguardFiles 'proguard-unity.txt', 'proguard-user.txt', '" + ProguardSetup.RuleFileName + @"', 'other-proguard.txt'
    }
}
" : expectedFinalScript);

            var sut = GetSut();

            sut.RemoveFromGradleProject();

            Assert.AreEqual(File.ReadAllText(gradleScript), expectedFinalScript);
        }
    }
}
