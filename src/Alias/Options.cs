using CommandLine;

public class Options
{
    [Option('t', "target-directory", Required = false)]
    public string? TargetDirectory { get; set; }

    [Option('a', "assemblies-to-alias", Required = true)]
    public string AssembliesToAlias { get; set; } = null!;

    [Option('k', "key", Required = false)]
    public string? Key { get; set; }
}