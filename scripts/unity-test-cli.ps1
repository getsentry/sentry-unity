. $PSScriptRoot/test-utils.ps1

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
        $arguments += "--filter", ".*$([regex]::Escape($Filter)).*"
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
