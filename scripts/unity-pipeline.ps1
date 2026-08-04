function Invoke-UnityCli([string[]] $arguments) {
    $output = & unity @arguments 2>&1
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

function Get-UnityCliErrorCode([object] $response) {
    if ($null -eq $response) {
        return $null
    }

    $errorsProperty = $response.PSObject.Properties["errors"]
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

function Get-UnityExecutionMode([string] $projectPath) {
    $projectName = Split-Path $projectPath -Leaf
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
        throw "Unity Editor for '$projectPath' is running but its Pipeline server is unreachable. Close the Editor or restore the Pipeline connection."
    }

    throw "Unable to determine Unity Editor status for '$projectPath':`n$($status.Output)"
}

function Invoke-UnityPipelineCommand(
    [string] $projectPath,
    [string] $command,
    [string[]] $commandArguments = @(),
    [int] $timeoutSeconds = 30
) {
    $arguments = @(
        "--json", "command", $command,
        "--project-path", $projectPath,
        "--timeout", $timeoutSeconds
    ) + $commandArguments

    $output = & unity @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Unity command '$command' failed:`n$($output | Out-String)"
    }

    try {
        $response = $output | Out-String | ConvertFrom-Json
    }
    catch {
        throw "Unity command '$command' returned invalid JSON:`n$($output | Out-String)"
    }

    if (-not $response.success -or -not $response.data.success) {
        throw "Unity command '$command' failed:`n$($output | Out-String)"
    }

    return $response.data.result
}

function Wait-ForUnityEditorReady([string] $projectPath, [int] $timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    $lastError = $null

    while ((Get-Date) -lt $deadline) {
        try {
            $status = Invoke-UnityPipelineCommand -ProjectPath $projectPath -Command "editor_status"
        }
        catch {
            $lastError = $_
            Start-Sleep -Seconds 1
            continue
        }

        if ($status.status -eq "ready" -and -not $status.compiling -and -not $status.domainReloadInProgress) {
            if ($status.playMode -ne "stopped") {
                throw "Unity Editor must be stopped before continuing. Current play mode: $($status.playMode)."
            }

            if ((Resolve-Path $status.projectPath).Path -ne $projectPath) {
                throw "Unity Pipeline is connected to $($status.projectPath), not $projectPath."
            }

            return
        }

        Start-Sleep -Seconds 1
    }

    if ($lastError) {
        throw "Unity Editor did not become ready within $timeoutSeconds seconds. $lastError"
    }

    throw "Unity Editor did not become ready within $timeoutSeconds seconds."
}

function Enable-UnityPipelineAutoTick([string] $projectPath) {
    Invoke-UnityPipelineCommand -ProjectPath $projectPath -Command "set_autotick" -CommandArguments @("--enable", "true") | Out-Null
}

function Wait-ForUnityRecompile([string] $projectPath, [int] $timeoutSeconds) {
    $recompile = Invoke-UnityPipelineCommand -ProjectPath $projectPath -Command "recompile" -TimeoutSeconds $timeoutSeconds
    if ($recompile.status -eq "up_to_date") {
        return
    }

    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $status = Invoke-UnityPipelineCommand -ProjectPath $projectPath -Command "recompile_status"
            if ($status -is [string]) {
                $status = $status | ConvertFrom-Json
            }
        }
        catch {
            $lastError = $_
            Start-Sleep -Seconds 1
            continue
        }

        if ($status.failed) {
            throw "Unity script recompilation failed:`n$($status.errors -join "`n")"
        }

        if ($status.status -in @("completed", "up_to_date", "idle")) {
            return
        }

        Start-Sleep -Seconds 1
    }

    if ($lastError) {
        throw "Unity script recompilation did not finish within $timeoutSeconds seconds. $lastError"
    }

    throw "Unity script recompilation did not finish within $timeoutSeconds seconds."
}

function Prepare-UnityPipelineEditor([string] $projectPath, [int] $timeoutSeconds) {
    Wait-ForUnityEditorReady -ProjectPath $projectPath -TimeoutSeconds $timeoutSeconds
    Enable-UnityPipelineAutoTick -ProjectPath $projectPath
    Wait-ForUnityRecompile -ProjectPath $projectPath -TimeoutSeconds $timeoutSeconds
    Enable-UnityPipelineAutoTick -ProjectPath $projectPath
}
