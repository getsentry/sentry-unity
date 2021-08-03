using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class NativeMainTests
    {
        [Test]
        public void DoesMainExist_MainExists_ReturnsTrue()
        {
            var testPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestFiles", "main_2019_4.txt");
            var nativeMain = new NativeMain();

            var doesExist = nativeMain.DoesMainExist(testPath);

            Assert.IsTrue(doesExist);
        }

        [Test]
        public void DoesMainExist_MainDoesNotExist_ReturnsFalse()
        {
            var testPath = "nonexistent/path";
            var nativeMain = new NativeMain();

            var doesExist = nativeMain.DoesMainExist(testPath);

            Assert.IsFalse(doesExist);
        }

        [Test]
        public void ContainsSentry_SentryAlreadyAdded_ReturnsTrue()
        {
            var main = GetFileContents("main_2019_4_expected.txt");
            var nativeMain = new NativeMain();

            var containsSentry = nativeMain.ContainsSentry(main);

            Assert.IsTrue(containsSentry);
        }

        [Test]
        public void ContainsSentry_SentryNotAdded_ReturnsFalse()
        {
            var main = GetFileContents("main_2019_4.txt");
            var nativeMain = new NativeMain();

            var containsSentry = nativeMain.ContainsSentry(main);

            Assert.IsFalse(containsSentry);
        }

        [Test]
        public void AddSentryToMain_SentryNotAddedTo_2019_4_MatchesExceptedOutput()
        {
            var main = GetFileContents("main_2019_4.txt");
            var expectedMain = GetFileContents("main_2019_4_expected.txt");
            var nativeMain = new NativeMain();

            var actualMain = nativeMain.AddSentryToMain(main);

            Assert.AreEqual(expectedMain, actualMain);
        }

        [Test]
        public void MainModifier_AddSentryToMain_SentryAdded()
        {
            var expectedMain = GetFileContents("main_2019_4_expected.txt");
            var workingMainPath = "temp.txt";
            File.WriteAllText(workingMainPath, GetFileContents("main_2019_4.txt"));
            var nativeMain = new NativeMain();

            nativeMain.AddSentry(workingMainPath);
            var actualMain = File.ReadAllText(workingMainPath);

            Assert.AreEqual(expectedMain, actualMain);

            File.Delete(workingMainPath);
        }

        private string GetFileContents(string fileName)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var mainPath = Path.Combine(assemblyPath, "TestFiles", fileName);

            return File.ReadAllText(mainPath);
        }
    }
}
