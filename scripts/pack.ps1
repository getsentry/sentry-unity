Remove-Item "package-release" -Recurse  -ErrorAction SilentlyContinue
New-Item "package-release" -ItemType Directory

$exclude = @(
    "README.md",
    "package.json",
    "Tests",
    "Tests.meta",
    "*.asmdef",
    "*.asmdef.meta",
    "SentryOptions.json*",
    "alias-references.*",
    "assembly-alias.*"
)

Copy-Item "package-dev/*" "package-release/" -Exclude $exclude -Recurse

# Override with package (e.g. custom .meta files)
Copy-Item "package/*" -Destination "package-release/" -Recurse -Force

Copy-Item "CHANGELOG.md" -Destination "package-release/CHANGELOG.md"
Copy-Item "LICENSE.md" -Destination "package-release/LICENSE.md"

# Copy samples
Copy-Item "samples/unity-of-bugs/Assets/Scenes" -Destination "package-release/Samples~/unity-of-bugs/Scenes" -Recurse
Copy-Item "samples/unity-of-bugs/Assets/Scripts" -Destination "package-release/Samples~/unity-of-bugs/Scripts" -Recurse `
    -Exclude "SmokeTestOptions.cs*"

# Create zip
Compress-Archive "package-release/*" -DestinationPath "package-release.zip" -Force
