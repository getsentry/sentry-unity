New-Item "package-release" -ItemType Directory

# Copy `package-dev` stuff
Copy-Item "package-dev/*" -Destination "package-release/" -Exclude "README.md", "package.json", "Tests", "Tests.meta", "*.asmdef", "*.asmdef.meta", "SentryOptions.json*" -Recurse

# Copy `package` stuff
Copy-Item "package/package.json" -Destination "package-release/package.json"
Copy-Item "package/README.md" -Destination "package-release/README.md"
Copy-Item "CHANGELOG.md" -Destination "package-release/CHANGELOG.md"
Copy-Item "package/CHANGELOG.md.meta" -Destination "package-release/CHANGELOG.md.meta"
Copy-Item "LICENSE.md" -Destination "package-release/LICENSE.md"
Copy-Item "package/LICENSE.md.meta" -Destination "package-release/LICENSE.md.meta"
New-Item -Type dir "package-release/Runtime/" -Force
Get-ChildItem "package/Runtime/" -Include "*.asmdef", "*.asmdef.meta" -Recurse | ForEach-Object { Copy-Item -Path $_.FullName -Destination "package-release/Runtime/" }
# Destination directory need to exist if we're copying a file instead of a directory
New-Item -Type dir "package-release/Documentation~/" -Force
Get-ChildItem "package/Documentation~/" -Include "*.md" -Recurse | ForEach-Object { Copy-Item -Path $_.FullName -Destination "package-release/Documentation~/" }

# Copy samples
Copy-Item "samples/unity-of-bugs/Assets/Scenes" -Destination "package-release/Samples~/unity-of-bugs/Assets/Scenes" -Recurse
Copy-Item "samples/unity-of-bugs/Assets/Scripts" -Destination "package-release/Samples~/unity-of-bugs/Assets/Scripts" -Recurse

# Create zip
Compress-Archive "package-release/*" -DestinationPath "package-release.zip" -Force
