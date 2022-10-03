using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
        };

        var module = ModuleDefinition.ReadModule(file, parameters);

        var hasSymbols = TryReadSymbols(module);

        return (module, hasSymbols);
    }

    private static bool TryReadSymbols(this ModuleDefinition module)
    {
        var hasSymbols = false;
        try
        {
            module.ReadSymbols();
            hasSymbols = true;
        }
        catch (SymbolsNotFoundException)
        {
        }

        return hasSymbols;
    }

    public static void Write(StrongNameKeyPair? key, bool hasSymbols, ModuleDefinition module, string file)
    {
        var parameters = new WriterParameters
        {
            WriteSymbols = hasSymbols
        };
        if (key != null)
        {
            parameters.StrongNameKeyPair = key;
        }

        module.Write(file, parameters);
    }
}
