using System.IO;
using Mono.Cecil;

namespace Sentry.Unity.Editor
{

    public class SentryAssemblyResolver : BaseAssemblyResolver
    {
        private readonly DefaultAssemblyResolver _defaultResolver;

        public SentryAssemblyResolver()
        {
            _defaultResolver = new DefaultAssemblyResolver();
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition assembly;
            try
            {
                assembly = _defaultResolver.Resolve(name);
            }
            catch (AssemblyResolutionException e)
            {
                var staging = "../game/Temp/StagingArea/Data/Managed/";
                var path = Path.GetFullPath(staging + e.AssemblyReference.Name + ".dll");
                if (File.Exists(path))
                {
                    return assembly = AssemblyDefinition.ReadAssembly(path);
                }

                throw;
            }

            return assembly;
        }
    }
}
