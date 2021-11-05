using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using StrongNameKeyPair = Mono.Cecil.StrongNameKeyPair;

public static class Program
{
    static void Main(string[] args)
    {
#pragma warning disable IDE0058 // Expression value is never used
        CommandRunner.RunCommand(
            (targetDirectory, assemblyNamesToAliases, keyFile) =>
            {
                Console.WriteLine($"TargetDirectory: {targetDirectory}");
                Console.WriteLine($"AssembliesToAlias: {assemblyNamesToAliases}");
                Console.WriteLine($"KeyFile: {keyFile}");

                Inner(targetDirectory, assemblyNamesToAliases.Split(';'), keyFile);
            },
            args);
#pragma warning restore IDE0058 // Expression value is never used
    }

    public static void Inner(string targetDirectory, IEnumerable<string> assemblyNamesToAliases, string? keyFile)
    {
        if (!Directory.Exists(targetDirectory))
        {
            throw new Error($"Target directory does not exist: {targetDirectory}");
        }

        StrongNameKeyPair? keyPair = null;
        var publicKey = Array.Empty<byte>();
        if (keyFile != null)
        {
            if (!File.Exists(keyFile))
            {
                throw new Error($"KeyFile directory does not exist: {keyFile}");
            }

            var fileBytes = File.ReadAllBytes(keyFile);

            keyPair = new(fileBytes);
            publicKey = keyPair.PublicKey;
        }

        var allFiles = Directory.GetFiles(targetDirectory, "*.dll").ToList();

        var assembliesToPatch = allFiles
            .Select(x => new FileAssembly(Path.GetFileNameWithoutExtension(x), x))
            .ToList();

        var assembliesToAlias = new List<AssemblyAlias>();

        foreach (var assemblyToAlias in assemblyNamesToAliases)
        {
            if (string.IsNullOrWhiteSpace(assemblyToAlias))
            {
                throw new Error("Empty string in assembliesToAliasString");
            }

            static void ProcessItem(List<FileAssembly> fileAssemblies, FileAssembly item, List<AssemblyAlias> assemblyAliases)
            {
#pragma warning disable IDE0058 // Expression value is never used
                fileAssemblies.Remove(item);
#pragma warning restore IDE0058 // Expression value is never used
                assemblyAliases.Add(new(item.Name, item.Path, item.Name + "_Alias", item.Path.Replace(".dll", "_Alias.dll")));
            }

            if (assemblyToAlias.EndsWith("*"))
            {
                var match = assemblyToAlias.TrimEnd('*');
                foreach (var item in assembliesToPatch.Where(x => x.Name.StartsWith(match)).ToList())
                {
                    ProcessItem(assembliesToPatch, item, assembliesToAlias);
                }
            }
            else
            {
                var item = assembliesToPatch.SingleOrDefault(x => x.Name == assemblyToAlias);
                if (item == null)
                {
                    throw new Error($"Could not find {assemblyToAlias} in {targetDirectory}.");
                }

                ProcessItem(assembliesToPatch, item, assembliesToAlias);
            }
        }

        using var resolver = new AssemblyResolver();
        {
            foreach (var assembly in assembliesToAlias)
            {
                var assemblyToPath = assembly.ToPath;
                File.Delete(assemblyToPath);
                var (module, hasSymbols) = ModuleReaderWriter.Read(assembly.FromPath, resolver);

                var name = module.Assembly.Name;
                name.Name += "_Alias";
                FixKey(keyPair, name);
                Redirect(module, assembliesToAlias, publicKey);
                ModuleReaderWriter.Write(keyPair, hasSymbols, module, assemblyToPath);
                module.Dispose();
            }

            foreach (var assembly in assembliesToPatch)
            {
                var assemblyPath = assembly.Path;
                var (module, hasSymbols) = ModuleReaderWriter.Read(assemblyPath, resolver);

                FixKey(keyPair, module.Assembly.Name);
                Redirect(module, assembliesToAlias, publicKey);
                ModuleReaderWriter.Write(keyPair, hasSymbols, module, assemblyPath);
                module.Dispose();
            }
        }
        foreach (var assembly in assembliesToAlias)
        {
            File.Delete(assembly.FromPath);
        }
    }

    static void FixKey(StrongNameKeyPair? key, AssemblyNameDefinition name)
    {
        if (key == null)
        {
            name.Hash = Array.Empty<byte>();
            name.PublicKey = Array.Empty<byte>();
            name.PublicKeyToken = Array.Empty<byte>();
        }
        else
        {
            name.PublicKey = key.PublicKey;
        }
    }

    static void Redirect(ModuleDefinition targetModule, List<AssemblyAlias> aliases, byte[] publicKey)
    {
        var assemblyReferences = targetModule.AssemblyReferences;
        foreach (var alias in aliases)
        {
            var toChange = assemblyReferences.SingleOrDefault(x => x.Name == alias.FromName);
            if (toChange != null)
            {
                toChange.Name = alias.ToName;
                toChange.PublicKey = publicKey;
            }
        }
    }
}
