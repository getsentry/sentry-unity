using System;
using System.IO;
using CommandLine;

static class CommandRunner
{
    public static int RunCommand(Invoke invoke, params string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                options => {
                    var targetDirectory = FindTargetDirectory(options.TargetDirectory);
                    try
                    {
                        invoke(targetDirectory, options.AssembliesToAlias, options.Key);
                    }
                    catch (Error e)
                    {
                        Console.WriteLine(e);
                        return 1;
                    }
                    return 0;
                },
                _ => 1);
    }


    static string FindTargetDirectory(string? targetDirectory)
    {
        if (targetDirectory == null)
        {
            return Environment.CurrentDirectory;
        }

        return Path.GetFullPath(targetDirectory);
    }
}
