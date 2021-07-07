param([string] $newVersion)

$utf8NoBomEncoding = New-Object System.Text.UTF8Encoding($False) 
function Replace-TextInFile {
    param([string] $filePath, [string] $pattern, [string] $replacement)

    $content = [IO.File]::ReadAllText($filePath)
    $content = [Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)
    [IO.File]::WriteAllText($filePath, $content, $utf8NoBomEncoding)
}

# Version of .NET assemblies:
Replace-TextInFile "$PSScriptRoot/../Directory.Build.props" '(?<=<Version>)(.*?)(?=</Version>)' $newVersion
# Version of the UPM package
Replace-TextInFile "$PSScriptRoot/../package/package.json" '(?<="version": ")(.*?)(?=")' $newVersion
# Bump the version on the repository README and the UPM's README:
Replace-TextInFile "$PSScriptRoot/../package/README.md" '(?<=git#)(.+)' $newVersion
Replace-TextInFile "$PSScriptRoot/../README.md" '(?<=git#)(.+)' $newVersion
