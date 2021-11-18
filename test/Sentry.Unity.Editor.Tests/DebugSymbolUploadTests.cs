using System.IO;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Editor.Tests
{
    public class DebugSymbolUploadTests
    {
        private class Fixture
        {
            public string FakeProjectPath { get; set; }
            public string UnityProjectPath { get; set; }
            public string GradleProjectPath { get; set; }

            public Fixture()
            {
                FakeProjectPath = Path.GetRandomFileName();
                SetupFakeProject(FakeProjectPath);

                UnityProjectPath = Path.Combine(FakeProjectPath, "UnityProject");
                GradleProjectPath = Path.Combine(FakeProjectPath, "GradleProject");
            }
        }

        [SetUp]
        public void SetUp() => _fixture = new Fixture();
        private Fixture _fixture = new();

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_fixture.FakeProjectPath, true);
        }

        [Test]
        public void GetSymbolsPath_TODO()
        {

        }

        [Test]
        public void AppendUploadToGradleFile_TODO()
        {

        }

        public static void SetupFakeProject(string fakeProjectPath)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectTemplatePath= Path.Combine(assemblyPath, "TestFiles", "SymbolsUploadProject");

            foreach (string dirPath in Directory.GetDirectories(projectTemplatePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(projectTemplatePath, fakeProjectPath));
            }

            foreach (string newPath in Directory.GetFiles(projectTemplatePath, "*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(projectTemplatePath, fakeProjectPath), true);
            }
        }
    }
}
