using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Sentry.Unity.Editor
{
    internal class SentryPlayerReaderWriter
    {
        private const string PlayerAssemblyName = "Assembly-CSharp.dll";
        private const string SentryUnityAssemblyName = "Sentry.Unity.dll";

        private readonly string _playerAssemblyPath;
        private readonly string _sentryUnityAssemblyPath;

        private bool _playerModuleHasSymbols;
        private ModuleDefinition _playerModule = null!;         // Set when reading the assemblies
        private ModuleDefinition _sentryUnityModule = null!;    // Set when reading the assemblies

        private readonly SentryAssemblyResolver _assemblyResolver;

        internal SentryPlayerReaderWriter(
            string workingDirectory,
            string playerAssemblyPath,
            string sentryUnityAssemblyPath)
        {
            _playerAssemblyPath = playerAssemblyPath;
            _sentryUnityAssemblyPath = sentryUnityAssemblyPath;

            _assemblyResolver = new SentryAssemblyResolver(workingDirectory);
        }

        public static SentryPlayerReaderWriter ReadAssemblies(string workingDirectory)
        {
            if (!Directory.Exists(workingDirectory))
            {
                throw new DirectoryNotFoundException($"Failed to find the working directory at '{workingDirectory}'");
            }

            var playerAssemblyPath = Path.Combine(workingDirectory, PlayerAssemblyName);
            if (!File.Exists(playerAssemblyPath))
            {
                throw new FileNotFoundException($"Failed to find '{PlayerAssemblyName}' at '{workingDirectory}'.");
            }

            var sentryUnityAssemblyPath = Path.Combine(workingDirectory, SentryUnityAssemblyName);
            if (!File.Exists(playerAssemblyPath))
            {
                throw new FileNotFoundException($"Failed to find '{SentryUnityAssemblyName}' at '{workingDirectory}'.");
            }

            var moduleReaderWriter = new SentryPlayerReaderWriter(workingDirectory, playerAssemblyPath, sentryUnityAssemblyPath);
            moduleReaderWriter.ReadFromWorkingDirectory();

            return moduleReaderWriter;
        }

        public void Write()
        {
            var parameters = new WriterParameters { WriteSymbols = _playerModuleHasSymbols };
            _playerModule?.Write(_playerAssemblyPath, parameters);
        }

        public MethodReference ImportSentryMonoBehaviourMethod(string methodName, Type[]? methodParameters = null)
        {
            var typeDefinition = GetTypeDefinition(_sentryUnityModule, typeof(SentryMonoBehaviour));
            var methodDefinition = GetMethodDefinition(typeDefinition, methodName, methodParameters);

            var reference = _playerModule.ImportReference(methodDefinition);
            if (reference is null)
            {
                throw new ArgumentException($"Failed to import requested reference in '{PlayerAssemblyName}'", methodDefinition.FullName);
            }

            return reference;
        }

        public TypeReference ImportType(Type type)
        {
            var reference = _playerModule.ImportReference(type);
            if (reference is null)
            {
                throw new ArgumentException($"Failed to import requested type in '{PlayerAssemblyName}'", type.FullName);
            }

            return reference;
        }

        public IEnumerable<TypeDefinition> GetTypes()
        {
            return _playerModule.GetTypes();
        }

        internal void ReadFromWorkingDirectory()
        {
            (_playerModule, _playerModuleHasSymbols) = Read(_playerAssemblyPath);
            (_sentryUnityModule, _) = Read(_sentryUnityAssemblyPath);
        }

        internal (ModuleDefinition module, bool hasSymbols) Read(string file)
        {
            try
            {
                var parameters = new ReaderParameters
                {
                    InMemory = true,
                    AssemblyResolver = _assemblyResolver,
                };

                var module = ModuleDefinition.ReadModule(file, parameters);
                var hasSymbols = TryReadSymbols(module);

                return (module, hasSymbols);
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Failed to read: '{file}'", exception);
            }
        }

        internal bool TryReadSymbols(ModuleDefinition module)
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

        internal TypeDefinition GetTypeDefinition(ModuleDefinition module, Type type)
        {
            var typeDefinition = module.GetType(type.FullName);
            if (typeDefinition is null)
            {
                throw new ArgumentException($"Failed to get requested type definition in {module.Name}", type.FullName);
            }

            return typeDefinition;
        }

        internal MethodDefinition GetMethodDefinition(TypeDefinition typeDefinition, string name, Type[]? requiredParameters = null)
        {
            foreach (var method in typeDefinition.Methods)
            {
                if (method?.Name == name)
                {
                    switch (requiredParameters)
                    {
                        case null when !method.HasParameters:
                            return method;
                        case null:
                            continue;
                    }

                    if (method.Parameters.Count != requiredParameters.Length)
                    {
                        continue;
                    }

                    // We go over all the required parameters and compare them to the parameters found in the method.
                    var hasMatchingParameters = true;
                    foreach (var parameter in requiredParameters)
                    {
                        var parameterDefinitions = method.Parameters.Where(p =>
                            string.Equals(
                                p.ParameterType.FullName,
                                parameter.FullName,
                                StringComparison.Ordinal))
                            .ToList();

                        if (parameterDefinitions.Count == 0)
                        {
                            hasMatchingParameters = false;
                            break;
                        }
                    }

                    if (hasMatchingParameters)
                    {
                        return method;
                    }
                }
            }

            throw new Exception(
                $"Failed to find method '{name}' " +
                $"in '{typeDefinition.FullName}' " +
                $"with parameters: '{(requiredParameters is not null ? string.Join(",", requiredParameters.AsEnumerable()) : "none")}'");
        }
    }
}