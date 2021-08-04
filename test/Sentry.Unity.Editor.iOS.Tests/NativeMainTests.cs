using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class NativeMainTests
    {
        [Test]
        public void ContainsSentry_SentryAlreadyAdded_ReturnsTrue()
        {
            var main = GetFileContents("main_2019_4_expected.txt");
            var nativeMain = new NativeMain();

            var containsSentry = nativeMain.ContainsSentry(main, null);

            Assert.IsTrue(containsSentry);
        }

        [Test]
        public void ContainsSentry_SentryNotAdded_ReturnsFalse()
        {
            var main = GetFileContents("main_2019_4.txt");
            var nativeMain = new NativeMain();

            var containsSentry = nativeMain.ContainsSentry(main, null);

            Assert.IsFalse(containsSentry);
        }

        [Test]
        public void AddSentryToMain_SentryNotAddedTo_2019_4_MatchesExpectedOutput()
        {
            var main = GetFileContents("main_2019_4.txt");
            var expectedMain = GetFileContents("main_2019_4_expected.txt");
            var nativeMain = new NativeMain();

            var actualMain = nativeMain.AddSentryToMain(main);

            Assert.AreEqual(expectedMain, actualMain);
        }

        [Test]
        public void AddSentryToMain_InvalidMain_ThrowsException()
        {
            var main = string.Empty;
            var nativeMain = new NativeMain();

            var x = Assert.Throws<ArgumentException>(() => nativeMain.AddSentryToMain(main));
            Assert.AreEqual("main", x.ParamName);
        }

        [Test]
        public void AddSentry_MainDoesNotExist_ThrowsFileNotFoundException()
        {
            var expectedMain = GetFileContents("main_2019_4_expected.txt");
            var workingMainPath = "temp.txt";
            File.WriteAllText(workingMainPath, GetFileContents("main_2019_4.txt"));
            var nativeMain = new NativeMain();

            nativeMain.AddSentry(workingMainPath, null);
            var actualMain = File.ReadAllText(workingMainPath);

            Assert.AreEqual(expectedMain, actualMain);

            File.Delete(workingMainPath);
        }

        [Test]
        public void AddSentry_CleanMain_2019_4_OutputMatchesExpected()
        {
            var expectedMain = GetFileContents("main_2019_4_expected.txt");
            var workingMainPath = "temp.txt";
            File.WriteAllText(workingMainPath, GetFileContents("main_2019_4.txt"));
            var nativeMain = new NativeMain();

            nativeMain.AddSentry(workingMainPath, null);
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
