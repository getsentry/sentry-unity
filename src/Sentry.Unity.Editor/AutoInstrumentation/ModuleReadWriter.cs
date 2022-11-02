using System;
using System.Reflection;
using Mono.Cecil;

namespace Sentry.Unity.Editor
{
    static class ModuleReaderWriter
    {
        public static (ModuleDefinition module, bool hasSymbols) Read(string file)
        {
            try
            {
                return InnerRead(file);
            }
            catch (Exception exception)
            {
                throw new($"Failed to read: {file}", exception);
            }
        }

        private static (ModuleDefinition module, bool hasSymbols) InnerRead(string file)
        {
            var parameters = new ReaderParameters
            {
                InMemory = true,
                AssemblyResolver = new SentryAssemblyResolver(),
            };

            var module = ModuleDefinition.ReadModule(file, parameters);
            var hasSymbols = TryReadSymbols(module);

            return (module, hasSymbols);
        }

        private static bool TryReadSymbols(this ModuleDefinition module)
        {
            try
            {
                module.ReadSymbols();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Write(StrongNameKeyPair? key, bool hasSymbols, ModuleDefinition module, string file)
        {
            var parameters = new WriterParameters
            {
                WriteSymbols = hasSymbols,
                StrongNameKeyPair = key
            };

            module.Write(file, parameters);
        }
    }
}
