using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

public class AssemblyResolver : IAssemblyResolver
{
    Dictionary<string, string> _referenceDictionary;
    Dictionary<string, AssemblyDefinition> _assemblyDefinitionCache = new(StringComparer.InvariantCultureIgnoreCase);

    public AssemblyResolver()
    {
        var assemblyLocation = typeof(AssemblyResolver).Assembly.Location;
        var directory = Path.GetDirectoryName(assemblyLocation)!;
        _referenceDictionary = new()
        {
            ["netstandard"] = Path.Combine(directory, "netstandard.dll")
        };
    }
    
    AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
    {
        if (_assemblyDefinitionCache.TryGetValue(file, out var assembly))
        {
            return assembly;
        }

        parameters.AssemblyResolver ??= this;
        try
        {
            return _assemblyDefinitionCache[file] = AssemblyDefinition.ReadAssembly(file, parameters);
        }
        catch (Exception exception)
        {
            throw new($"Could not read '{file}'.", exception);
        }
    }

    public AssemblyDefinition? Resolve(AssemblyNameReference assemblyNameReference)
    {
        return Resolve(assemblyNameReference, new());
    }

    public AssemblyDefinition? Resolve(AssemblyNameReference assemblyNameReference, ReaderParameters? parameters)
    {
        parameters ??= new();

        if (_referenceDictionary.TryGetValue(assemblyNameReference.Name, out var fileFromDerivedReferences))
        {
            return GetAssembly(fileFromDerivedReferences, parameters);
        }

        return null;
    }

    public void Dispose()
    {
        foreach (var value in _assemblyDefinitionCache.Values)
        {
            value.Dispose();
        }
    }
}
