using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sentry.Unity.Editor.Android;

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
        [TestCase("\n")]
        [TestCase("\r\n")]
        public void AddsRuleToGradleScript(string lineSeparator)
        {
            var gradleScript = Path.Combine(_outputPath, "build.gradle");

            // Update the file to have the expected separators for this test case.
            File.WriteAllText(gradleScript, Regex.Replace(File.ReadAllText(gradleScript), "\r?\n", lineSeparator));

            // Sanity check that the previous replacement worked.
            StringAssert.Contains(lineSeparator, File.ReadAllText(gradleScript));
            Assert.AreEqual(47, Regex.Matches(File.ReadAllText(gradleScript), lineSeparator).Count);

            var sut = GetSut();

            var regex = $"consumerProguardFiles [^\r\n]*, '{ProguardSetup.RuleFileName}'";
            StringAssert.DoesNotMatch(regex, File.ReadAllText(gradleScript));

            sut.AddToGradleProject();

            var newContent = File.ReadAllText(gradleScript);
            StringAssert.IsMatch(regex, newContent);

            // Doesn't add again on the second run.
            sut.AddToGradleProject();
            Assert.AreEqual(File.ReadAllText(gradleScript), newContent);
        }

        [Test]
        [TestCase("AddingProguard/build.gradle_test_1")]
        [TestCase("AddingProguard/build.gradle_test_2")]
        public void AddsRulesToGradleScript_ContainsConsumerProguardRules_MatchesExpectedOutput(string testCaseFileName)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testCasePath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + ".txt");
            var expectedPath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + "_expected.txt");

            var actualOutputPath = Path.Combine(_outputPath, "build.gradle");
            File.Copy(testCasePath, actualOutputPath, true);

            GetSut().AddToGradleProject();

            StringAssert.AreEqualIgnoringCase(File.ReadAllText(expectedPath), File.ReadAllText(actualOutputPath));
        }

        [Test]
        [TestCase("AddingProguard/build.gradle_test_1")]
        [TestCase("AddingProguard/build.gradle_test_2")]
        public void RemovesRuleFromGradleScript(string testCaseFileName)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testCasePath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + "_expected.txt");
            var expectedPath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + ".txt");

            var actualOutputPath = Path.Combine(_outputPath, "build.gradle");
            File.Copy(testCasePath, actualOutputPath, true);

            GetSut().RemoveFromGradleProject();

            StringAssert.AreEqualIgnoringCase(File.ReadAllText(expectedPath), File.ReadAllText(actualOutputPath));
        }
    }
}
