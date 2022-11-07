using System.IO;
using Mono.Cecil;

namespace Sentry.Unity.Editor
{
    public class SentryAssemblyResolver : BaseAssemblyResolver
    {
        private readonly string _workingDirectory;
        private readonly DefaultAssemblyResolver _defaultResolver = new();

        public SentryAssemblyResolver(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            try
            {
                return _defaultResolver.Resolve(name);
            }
            catch (AssemblyResolutionException e)
            {
                var path = Path.GetFullPath(Path.Combine(_workingDirectory, e.AssemblyReference.Name + ".dll"));
                if (File.Exists(path))
                {
                    return AssemblyDefinition.ReadAssembly(path);
                }

                throw;
            }
        }
    }
}
