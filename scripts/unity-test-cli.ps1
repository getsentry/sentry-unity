. $PSScriptRoot/test-utils.ps1

function Test-UnityProject([string] $Path) {
    $manifestFile = Join-Path $Path "Packages/manifest.json"
    if (-not (Test-Path $manifestFile -PathType Leaf)) {
        throw "Unity project at $Path has no Packages/manifest.json."
    }

    $manifest = Get-Content $manifestFile -Raw | ConvertFrom-Json
    $packageSource = $manifest.dependencies."io.sentry.unity.dev"
    if (-not $packageSource -or -not $packageSource.StartsWith("file:")) {
        throw "Unity project at $Path must install io.sentry.unity.dev from this checkout's package-dev directory."
    }

    if ($packageSource.StartsWith("file://")) {
        $packagePath = ([Uri]$packageSource).LocalPath
    }
    else {
        $packagePath = [Uri]::UnescapeDataString($packageSource.Substring("file:".Length))
    }

    if (-not [IO.Path]::IsPathRooted($packagePath)) {
        $packagePath = Join-Path (Split-Path $manifestFile -Parent) $packagePath
    }

    if (-not (Test-Path $packagePath -PathType Container)) {
        throw "io.sentry.unity.dev points to missing package-dev directory $packagePath."
    }

    $expectedPackagePath = (Resolve-Path (Join-Path $repoRoot "package-dev")).Path
    $packagePath = (Resolve-Path $packagePath).Path
    if ($packagePath -ne $expectedPackagePath) {
        throw "io.sentry.unity.dev points to $packagePath. This harness requires $expectedPackagePath so it tests this checkout's SDK build."
    }
}

function Invoke-UnityCli([string[]] $Arguments) {
    $output = & unity @Arguments 2>&1
    $text = $output | Out-String
    $response = $null

    try {
        $response = $text | ConvertFrom-Json
    }
    catch {
        # Most CLI commands stream Unity logs rather than return JSON.
    }

    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output   = $text
        Response = $response
    }
}

function Get-UnityCliErrorCode([object] $Response) {
    if ($null -eq $Response) {
        return $null
    }

    $errorsProperty = $Response.PSObject.Properties["errors"]
    if ($null -eq $errorsProperty -or $null -eq $errorsProperty.Value) {
        return $null
    }

    foreach ($error in @($errorsProperty.Value)) {
        if ($error.code) {
            return $error.code
        }
    }

    return $null
}

function Test-UnityProjectIsLocked {
    $lockFile = Join-Path $ProjectPath "Temp/UnityLockfile"
    if (-not (Test-Path $lockFile -PathType Leaf)) {
        return $false
    }

    try {
        $stream = [System.IO.File]::Open(
            $lockFile,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
        $stream.Dispose()
        return $false
    }
    catch [System.IO.IOException] {
        return $true
    }
    catch [System.UnauthorizedAccessException] {
        return $true
    }
}

function Get-TestExecutionMode {
    $projectName = Split-Path $ProjectPath -Leaf
    $status = Invoke-UnityCli @("status", "--project", $projectName, "--format", "json")
    $successProperty = $null
    if ($status.Response) {
        $successProperty = $status.Response.PSObject.Properties["success"]
    }

    if ($status.ExitCode -eq 0 -and $successProperty -and $successProperty.Value) {
        return "Pipeline"
    }

    $errorCode = Get-UnityCliErrorCode $status.Response
    if ($errorCode -eq "STATUS_NO_INSTANCES") {
        if (Test-UnityProjectIsLocked) {
            return "Pipeline"
        }

        return "Headless"
    }

    if ($errorCode -eq "STATUS_ALL_UNREACHABLE") {
        throw "Unity Editor for '$ProjectPath' is running but its Pipeline server is unreachable. Close the Editor or restore the Pipeline connection before running tests."
    }

    throw "Unable to determine Unity Editor status for '$ProjectPath':`n$($status.Output)"
}

function Invoke-UnityHeadlessTest([string] $TestMode) {
    New-Item -ItemType Directory -Force -Path $headlessResultsDirectory | Out-Null
    $resultPath = Join-Path $headlessResultsDirectory "$($TestMode.ToLowerInvariant()).xml"
    Remove-Item $resultPath -Force -ErrorAction Ignore

    $arguments = @("test", $ProjectPath, "--mode", $TestMode, "--output", $resultPath, "--timeout", $testTimeout)
    if ($Filter) {
        $arguments += "--filter", $Filter
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $run = Invoke-UnityCli $arguments
    $stopwatch.Stop()

    $summary = Parse-TestResults $resultPath
    if ($null -eq $summary) {
        throw "Unity CLI $TestMode tests did not produce valid results at ${resultPath}:`n$($run.Output)"
    }

    if ($summary.Total -eq 0) {
        throw "Unity CLI $TestMode test results are empty."
    }

    if ($run.ExitCode -ne 0 -and $summary.Success) {
        throw "Unity CLI $TestMode tests exited with code $($run.ExitCode):`n$($run.Output)"
    }

    return [pscustomobject]@{
        Mode         = $TestMode
        Duration     = $stopwatch.Elapsed.TotalSeconds
        Total        = $summary.Total
        Passed       = $summary.Passed
        Failed       = $summary.Failed
        Skipped      = $summary.Skipped
        Inconclusive = $summary.Inconclusive
        FailedTests  = $summary.FailedTests
        Success      = $run.ExitCode -eq 0 -and $summary.Success -and $summary.Inconclusive -eq 0
    }
}
