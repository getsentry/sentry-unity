Remove-Item "package-release" -Recurse -ErrorAction SilentlyContinue
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

if ($IsLinux -or $IsMacOS) {
    # Use cp -R to preserve XCFramework signatures and symlinks
    bash -c 'for item in package-dev/*; do cp -R "$item" package-release/; done'
} else {
    # On Windows (local dev only), signatures will be broken anyway - use simple copy
    Copy-Item "package-dev/*" "package-release/" -Exclude $exclude -Recurse
}

# Override with package (e.g. custom .meta files)
Copy-Item "package/*" -Destination "package-release/" -Recurse -Force

Copy-Item "CHANGELOG.md" -Destination "package-release/CHANGELOG.md"
Copy-Item "LICENSE.md" -Destination "package-release/LICENSE.md"

# Copy samples
Copy-Item "samples/unity-of-bugs/Assets/Scenes*" -Destination "package-release/Samples~/unity-of-bugs/" -Recurse
Copy-Item "samples/unity-of-bugs/Assets/Scripts*" -Destination "package-release/Samples~/unity-of-bugs/" -Recurse

# Create zip - use tar on Unix to preserve XCFramework signatures
if ($IsLinux -or $IsMacOS) {
    bash -c 'cd package-release && tar -czf ../package-release.zip * --xattrs --xattrs-include="*"'
} else {
    Compress-Archive "package-release/*" -DestinationPath "package-release.zip" -Force
}
