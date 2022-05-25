# Note: this is currently used by "integration-*.ps1" scripts as well as "smoke-test-*.ps1" scripts.
# If/when those are merged to some extent, maybe this file could be merged into `IntegrationGlobals.ps1`.

function RunApiServer([string] $ServerScript, [string] $Uri = "http://localhost:8000")
{
    $result = "" | Select-Object -Property process, outFile, errFile, stop, output, dispose
    Write-Host "Starting the $ServerScript on $Uri"
    $result.outFile = New-TemporaryFile
    $result.errFile = New-TemporaryFile

    $result.process = Start-Process "python3" -ArgumentList @("$PSScriptRoot/$ServerScript.py", $Uri) `
        -NoNewWindow -PassThru -RedirectStandardOutput $result.outFile -RedirectStandardError $result.errFile

    $result.output = { "$(Get-Content $result.outFile -Raw)`n$(Get-Content $result.errFile -Raw)" }.GetNewClosure()

    $result.dispose = {
        $result.stop.Invoke()

        Write-Host "Server stdout:" -ForegroundColor Yellow
        $stdout = Get-Content $result.outFile -Raw
        Write-Host $stdout

        Write-Host "Server stderr:" -ForegroundColor Yellow
        $stderr = Get-Content $result.errFile -Raw
        Write-Host $stderr

        Remove-Item $result.outFile -ErrorAction Continue
        Remove-Item $result.errFile -ErrorAction Continue
        return "$stdout`n$stderr"
    }.GetNewClosure()

    $result.stop = {
        # Stop the HTTP server
        Write-Host "Stopping the $ServerScript ... " -NoNewline
        try
        {
            Write-Host (Invoke-WebRequest -Uri "$Uri/STOP").StatusDescription
        }
        catch
        {
            Write-Host "/STOP request failed, killing the server process"
            $result.process | Stop-Process -Force -ErrorAction SilentlyContinue
        }
        $result.process | Wait-Process -Timeout 10 -ErrorAction Continue
        $result.stop = {}
    }.GetNewClosure()

    # The process shouldn't finish by itself, if it did, there was an error, so let's check that
    Start-Sleep -Second 1
    if ($result.process.HasExited)
    {
        Write-Host "Couldn't start the $ServerScript" -ForegroundColor Red
        Write-Host "Standard Output:" -ForegroundColor Yellow
        Get-Content $result.outFile
        Write-Host "Standard Error:" -ForegroundColor Yellow
        Get-Content $result.errFile
        Remove-Item $result.outFile
        Remove-Item $result.errFile
        exit 1
    }

    return $result
}

function CrashTestWithServer([ScriptBlock] $CrashTestCallback, [string] $SuccessString)
{
    if ("$SuccessString" -eq "")
    {
        throw "SuccessString cannot be empty"
    }

    # You can increase this to retry multiple times. Seems a bit flaky at the moment in CI.
    if ($null -eq $env:CI)
    {
        $runs = 1
        $timeout = 5
    }
    else
    {
        $runs = 5
        $timeout = 30
    }

    for ($run = 1; $run -le $runs; $run++)
    {
        if ($run -ne 1)
        {
            Write-Host "Sleeping for $run seconds before the next retry..."
            Start-Sleep -Seconds $run
        }

        # start the server
        $httpServer = RunApiServer "crash-test-server"

        # run the test
        try
        {
            $CrashTestCallback.Invoke()
        }
        catch
        {
            $httpServer.stop.Invoke()
            if ($run -eq $runs)
            {
                throw
            }
            else
            {
                Write-Warning "crash test $run/$runs : FAILED, retrying. The error was: $_"
                continue
            }
        }

        # evaluate the result
        for ($i = $timeout; $i -gt 0; $i--)
        {
            Write-Host "Waiting for the expected message to appear in the server output logs; $i seconds remaining..."
            if ("$($httpServer.output.Invoke())".Contains($SuccessString))
            {
                break
            }
            Start-Sleep -Milliseconds 1000
        }

        $output = $httpServer.dispose.Invoke()
        Write-Host "Looking for the SuccessString ($SuccessString) in the server output..."
        if ("$output".Contains($SuccessString))
        {
            Write-Host "crash test $run/$runs : PASSED" -ForegroundColor Green
            break
        }
        Write-Host "SuccessString ($SuccessString) not found..." -ForegroundColor Red
        if ($run -eq $runs)
        {
            throw "crash test $run/$runs : FAILED"
        }
        else
        {
            Write-Warning "crash test $run/$runs : FAILED, retrying"
        }
    }
}

function SymbolServerUrlFor([string] $UnityPath)
{
    $UnityPath.StartsWith("docker ") ? 'http://172.17.0.1:8000' : 'http://localhost:8000'
}

function RunWithSymbolServer([ScriptBlock] $Callback)
{
    # start the server
    $httpServer = RunApiServer "symbol-upload-server" (SymbolServerUrlFor $UnityPath)

    # run the test
    try
    {
        $Callback.Invoke()
    }
    finally
    {
        $httpServer.stop.Invoke()
    }

    return $httpServer.dispose.Invoke()
}

function CheckSymbolServerOutput([string] $buildMethod, [string] $symbolServerOutput)
{
    $expectedFiles = @()
    If ($buildMethod.contains('Mac'))
    {
        $expectedFiles = @(
            'IntegrationTest',
            'UnityPlayer.dylib',
            'GameAssembly.dylib',
            'Sentry.dylib',
            'Sentry.dylib.dSYM'
        )
    }
    ElseIf ($buildMethod.contains('Windows'))
    {
        $expectedFiles = @(
            'test.exe',
            'GameAssembly.dll',
            'GameAssembly.pdb',
            'UnityPlayer.dll',
            'sentry.pdb',
            'sentry.dll'
        )
    }
    ElseIf ($buildMethod.contains('Linux'))
    {
        $expectedFiles = @(
            'test',
            'test_s.debug',
            'GameAssembly.so',
            'UnityPlayer.so',
            'UnityPlayer_s.debug',
            'libsentry.dbg.so'
        )
    }
    ElseIf ($buildMethod.contains('Android'))
    {
        $expectedFiles = @(
            'libmain.so',
            'libunity.so',
            @('libunity.dbg.so', 'libunity.sym.so'),
            'libil2cpp.so',
            @('libil2cpp.dbg.so', 'libil2cpp.sym.so'),
            'libsentry.so',
            'libsentry-android.so'
        )
    }
    ElseIf ($buildMethod.contains('IOS'))
    {
        $expectedFiles = @(
            'IntegrationTest',
            'UnityFramework',
            'libiPhone-lib.dylib',
            'Sentry'
        )
    }
    ElseIf ($buildMethod.contains('WebGL'))
    {
        Write-Host 'No symbols are uploaded for WebGL - nothing to test.' -ForegroundColor Yellow
        return
    }
    Else
    {
        Throw "Cannot CheckSymbolServerOutput() for an unknown buildMethod: '$buildMethod'"
    }

    Write-Host 'Verifying debug symbol upload...'
    $successful = $true
    :nextExpectedFile foreach ($name in $expectedFiles)
    {
        $alternatives = ($name -is [array]) ? $name : @($name)
        foreach ($name in $alternatives)
        {
            # It's enough if a single symbol alternative is found
            if ($symbolServerOutput -match "Received: .* $([Regex]::Escape($name))\b")
            {
                Write-Host "  $name - OK"
                continue nextExpectedFile
            }
        }
        # Note: control only gets here if none of the alternatives match...
        $successful = $false
        Write-Host "  $name - MISSING" -ForegroundColor Red
    }
    if ($successful)
    {
        Write-Host 'All expected debug symbols have been uploaded' -ForegroundColor Green
    }
    else
    {
        exit 1
    }
}
