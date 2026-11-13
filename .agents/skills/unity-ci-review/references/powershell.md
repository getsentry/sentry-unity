# PowerShell Pitfalls

Read this when a hunk changes a `*.ps1` file or a `shell: pwsh` step. The items come from fixed bugs and false-positive reports in this repo.

## Error Handling

- `$ErrorActionPreference = 'Stop'` does not stop on a failing native command (`dotnet`, `docker`, `gh`, `unity`, `xcodebuild`) unless `$PSNativeCommandUseErrorActionPreference = $true`. Several scripts set it. A block that sets it to `$false` must check `$LASTEXITCODE` itself.
- `try { ... } catch { }` around a polling loop that swallows every exception keeps retrying errors that will never clear (wrong path, rejected request) until the timeout. It should rethrow those.
- A retry helper used by some wait functions but not by a new one gives that one a single-failure path.
- `exit` inside a dot-sourced script (`. ./common.ps1`) ends the caller too. `return` from a script does not set an exit code.

## Values And Strings

- Inside double quotes, only the variable expands: `"$obj.Prop"` is the object's string form followed by the literal `.Prop`. Property access needs `$()`. So `"$_.exe"` is correct when `$_` is a string (`"Windows-x86_64.exe"`); do not report it. It is a bug only when the code meant a property of an object.
- `Sort-Object Name` on version folders (`30.0.3`, `34.0.0`, `9.0.0`) sorts as text. Use `Sort-Object { [version]$_.Name }`.
- Comparison operators are case-insensitive by default (`-eq`, `-like`, `-match`). Use `-ceq` and friends when case matters, for example Unity plugin folder names on Linux.
- An empty pipeline result is `$null`, and a single result is a scalar, not an array. Code that indexes or iterates the result must handle both.
- Member access on a collection enumerates its items (`$matches.Groups`, `$files.Name`). Do not report that as a missing property.

## Paths And Working Directory

- `$PSScriptRoot` is the script's directory. Relative paths passed to `dotnet`, `git`, or `unity` resolve against the caller's current directory. Scripts that can run from anywhere must build paths from `$PSScriptRoot` or `Push-Location` first.
- `Test-Path` on a directory the script created before the tool ran is always true. It proves nothing about the tool's output unless the tool's exit code is checked too.

## Idempotency

- A download or setup step that creates its target directory before it finishes must remove it on failure, or later runs must validate the contents. An "already exists, skip" check otherwise turns one failed run into a permanent failure.
- "Skip if present" checks must also compare the version (`sentry-cli.properties`, `scripts/unity-versions.json`) when the version can change.
