# Note: this is currently used by integration test scripts as well as "smoke-test-*.ps1" scripts.
# If/when those are merged to some extent, maybe this file could be merged into `globals.ps1`.

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

function RunApiServer([string] $ServerScript, [string] $Uri, [string] $EnvelopeDir)
{
    if ([string]::IsNullOrEmpty($Uri))
    {
        $Uri = SymbolServerUrlFor ((Test-Path variable:UnityPath) ? $UnityPath : '')
    }

    $result = "" | Select-Object -Property process, outFile, errFile, stop, output, dispose, envelopeDir
    Write-Host "Starting the '$ServerScript' on '$Uri' with envelope dir '$EnvelopeDir'"
    $result.outFile = New-TemporaryFile
    $result.errFile = New-TemporaryFile

    $result.process = Start-Process "python3" -ArgumentList @("$PSScriptRoot/$ServerScript.py", $Uri, $EnvelopeDir) `
        -NoNewWindow -PassThru -RedirectStandardOutput $result.outFile -RedirectStandardError $result.errFile

    $result.output = { "$(Get-Content $result.outFile -Raw)`n$(Get-Content $result.errFile -Raw)" }.GetNewClosure()

    $result.dispose = {
        $result.stop.Invoke()

        $stdout = Get-Content $result.outFile -Raw
        $stderr = Get-Content $result.errFile -Raw

        if ($env:CI)
        {
            Write-Host "::group::  Server stdout" -ForegroundColor Yellow
            Write-Host $stdout
            Write-Host "::endgroup::"
    
            Write-Host "::group::  Server stderr" -ForegroundColor Yellow
            Write-Host $stderr
            Write-Host "::endgroup::"
        }
        
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
            Write-Host "/STOP request failed: $_ - killing the server process instead"
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

    Write-Host "Running crash test with server" -ForegroundColor Yellow

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
                Write-Host $_.ScriptStackTrace
                continue
            }
        }

        # evaluate the result
        # for ($i = $timeout; $i -gt 0; $i--)
        # {
        #     Write-Host "Waiting for the expected message to appear in the server output logs; $i seconds remaining..."
        #     if ("$($httpServer.output.Invoke())".Contains($SuccessString))
        #     {
        #         break
        #     }
        #     Start-Sleep -Milliseconds 1000
        # }

        $output = $httpServer.dispose.Invoke()
        # Write-Host "Looking for the SuccessString ($SuccessString) in the server output..."
        # if ("$output".Contains($SuccessString))
        # {
        #     Write-Host "crash test $run/$runs : PASSED" -ForegroundColor Green
        #     break
        # }
        # Write-Host "SuccessString ($SuccessString) not found..." -ForegroundColor Red
        # if ($run -eq $runs)
        # {
        #     throw "crash test $run/$runs : FAILED"
        # }
        # else
        # {
        #     Write-Warning "crash test $run/$runs : FAILED, retrying"
        # }
    }
}

function SmokeTestWithServer([string] $EnvelopeDir, [ScriptBlock] $RunGameCallback)
{
    RunEnvelopeCapturingServer -EnvelopeDir $EnvelopeDir -RunGameCallback $RunGameCallback
    
    $envelopeContents = @(ReadEnvelopes $EnvelopeDir)
    
    if ($envelopeContents.Count -le 0)
    {
        Write-Host "No envelopes found in $EnvelopeDir" -ForegroundColor Red
        throw "Smoke Test with Server: FAILED - No envelopes found"
    }
    
    # Find all event envelopes
    $eventEnvelopes = @()
    $sessionEnvelopes = @()
    $transactionEnvelopes = @()
    foreach ($envelope in $envelopeContents)
    {
        if ($envelope -match """type"":""event""")
        {
            $eventEnvelopes += $envelope
        }
        elseif ($envelope -match """type"":""session""")
        {
            $sessionEnvelopes += $envelope
        }
        elseif ($envelope -match """type"":""transaction""")
        {
            $transactionEnvelopes += $envelope
        }
    }

    Write-Host "Checking for expected session envelope:" -ForegroundColor Yellow
    $sessionEnvelopesFound = $sessionEnvelopes.Count -gt 0
    if ($sessionEnvelopesFound)
    {
        Write-Host "  ✓ Found session envelopes" -ForegroundColor Green
        
    }
    else
    {
        Write-Host "  ✗ Missing session envelopes" -ForegroundColor Red
    }

    Write-Host "Checking for expected transaction envelope:" -ForegroundColor Yellow
    $transactionEnvelopesFound = $transactionEnvelopes.Count -gt 0
    if ($transactionEnvelopesFound)
    {
        Write-Host "  ✓ Found transaction envelopes" -ForegroundColor Green
    }
    else
    {
        Write-Host "  ✗ Missing transaction envelopes" -ForegroundColor Red
    }

    $expectedEventEnvelopes = @(
        @{ name = "CaptureLogError event"; pattern = """message"":""CaptureLogError""" },
        @{ name = "CaptureMessage event"; pattern = """message"":""CaptureMessage""" },
        @{ name = "CaptureException event"; pattern = """type"":""System.Exception"",""value"":""CaptureException""" }
    )
    
    $allEventEnvelopesFound = $true
    $exceptionEnvelope = $null

    Write-Host "Checking for expected events:" -ForegroundColor Yellow
    foreach ($expectedEvent in $expectedEventEnvelopes)
    {
        $eventFound = $false
        foreach ($envelope in $eventEnvelopes)
        {
            if ($envelope -match $expectedEvent.pattern)
            {
                $eventFound = $true

                # We're going to check for additional contexts in the exception envelope only
                if ($expectedEvent.name -eq "CaptureException event")
                {
                    $exceptionEnvelope = $envelope
                }
                break
            }
        }
        
        if ($eventFound)
        {
            Write-Host "  ✓ Found $($expectedEvent.name)" -ForegroundColor Green
        }
        else
        {
            Write-Host "  ✗ Missing $($expectedEvent.name)" -ForegroundColor Red
            $allEventEnvelopesFound = $false
        }
    }
    
    # Required contexts to validate to be present by default in all envelopes
    $defaultContexts = @(
        @{ name = "app context"; pattern = """type""\s*:\s*""app""" },
        @{ name = "device context"; pattern = """type""\s*:\s*""device""" },
        @{ name = "gpu context"; pattern = """type""\s*:\s*""gpu""" },
        @{ name = "os context"; pattern = """type""\s*:\s*""os""" },
        @{ name = "runtime context"; pattern = """type""\s*:\s*""runtime""" },
        @{ name = "unity context"; pattern = """type""\s*:\s*""unity""" },
        @{ name = "user data"; pattern = """user""\s*:\s*\{""id""" },
        @{ name = "attachment"; pattern = """filename""\s*:\s*""screenshot.jpg""" }
    )
    
    Write-Host "Checking for all default contexts expected to be present in all envelopes:" -ForegroundColor Yellow
    $allDefaultContextsPassed = $true
    foreach ($context in $defaultContexts)
    {
        $contextFound = $false
        foreach ($envelope in $eventEnvelopes)
        {
            if ($envelope -match $context.pattern)
            {
                $contextFound = $true
                break
            }
        }
        
        if ($contextFound)
        {
            Write-Host "  ✓ Found $($context.name)" -ForegroundColor Green
        }
        else
        {
            Write-Host "  ✗ Missing $($context.name)" -ForegroundColor Red
            $allDefaultContextsPassed = $false
        }
    }

    $additionalContexts = @(
        @{ name = "breadcrumb"; pattern = """message"":""crumb"",""type"":""error"",""data"":\{""foo"":""bar""" },
        @{ name = "scope breadcrumb"; pattern = """message"":""scope-crumb""" },
        @{ name = "extra data"; pattern = """extra"":\{""extra-key"":42\}" },
        @{ name = "tag"; pattern = """tag-key"":""tag-value""" },
        @{ name = "user id"; pattern = """user"":\{""id"":""user-id""" },
        @{ name = "username"; pattern = """username"":""username""" },
        @{ name = "email"; pattern = """email"":""email@example.com""" },
        @{ name = "ip address"; pattern = """ip_address"":""::1""" },
        @{ name = "user role"; pattern = """role"":""admin""" }
    )

    Write-Host "Checking for additional contexts in exception envelope only:" -ForegroundColor Yellow
    $allAdditionalContextsPassed = $true
    foreach ($context in $additionalContexts)
    {
        if ($exceptionEnvelope -match $context.pattern)
        {
            Write-Host "  ✓ Found $($context.name) in exception envelope" -ForegroundColor Green
        }
        else
        {
            Write-Host "  ✗ Missing $($context.name) in exception envelope" -ForegroundColor Red
            $allAdditionalContextsPassed = $false
        }
    }

    if ($allEventEnvelopesFound -and $allDefaultContextsPassed -and $allAdditionalContextsPassed -and $sessionEnvelopesFound -and $transactionEnvelopesFound)
    {
        Write-Host "Smoke Test with Server: PASSED" -ForegroundColor Green
    }
    else
    {
        Write-Host "Smoke Test with Server: FAILED" -ForegroundColor Red
        exit 1
    }
}

function RunEnvelopeCapturingServer([string] $EnvelopeDir, [ScriptBlock] $RunGameCallback)
{
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

        # Clear all envelopes from the previous run
        Get-ChildItem -Path $EnvelopeDir -Filter "envelope_*.json" -ErrorAction SilentlyContinue | Remove-Item -Force

        $httpServer = RunApiServer "envelope-logging-server" -EnvelopeDir $EnvelopeDir

        try
        {
            $RunGameCallback.Invoke()
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
                Write-Host $_.ScriptStackTrace
                continue
            }
        }

        # Wait for all envelopes to be processed
        Start-Sleep -Seconds 2

        # Stop the server
        $httpServer.dispose.Invoke()


        # evaluate the result
        # for ($i = $timeout; $i -gt 0; $i--)
        # {
        #     Write-Host "Waiting for the expected message to appear in the server output logs; $i seconds remaining..."
        #     if ("$($httpServer.output.Invoke())".Contains($SuccessString))
        #     {
        #         break
        #     }
        #     Start-Sleep -Milliseconds 1000
        # }

        # $output = $httpServer.dispose.Invoke()
        # Write-Host "Looking for the SuccessString ($SuccessString) in the server output..."
        # if ("$output".Contains($SuccessString))
        # {
        #     Write-Host "crash test $run/$runs : PASSED" -ForegroundColor Green
        #     break
        # }
        # Write-Host "SuccessString ($SuccessString) not found..." -ForegroundColor Red
        # if ($run -eq $runs)
        # {
        #     throw "crash test $run/$runs : FAILED"
        # }
        # else
        # {
        #     Write-Warning "crash test $run/$runs : FAILED, retrying"
        # }
    }
}

function ReadEnvelopes([string] $EnvelopeDir)
{
    Write-Host "Reading all envelopes in '$EnvelopeDir'" -ForegroundColor Yellow
    
    if (-not (Test-Path $EnvelopeDir))
    {
        Write-Warning "Envelope directory '$EnvelopeDir' does not exist"
        return @()
    }
    
    $envelopeFiles = @(Get-ChildItem -Path $EnvelopeDir -Filter "envelope_*.json" -ErrorAction SilentlyContinue)
    $envelopeCount = $envelopeFiles.Length
    
    if ($envelopeCount -eq 0)
    {
        Write-Warning "No envelope files found in directory"
        return @()
    }
    
    Write-Host "Found $envelopeCount envelope file(s)" -ForegroundColor Cyan
    
    [System.Collections.ArrayList]$envelopeContents = @()
    foreach ($file in $envelopeFiles)
    {
        try
        {
            $content = Get-Content -Path $file.FullName -Raw
            [void]$envelopeContents.Add($content)
        }
        catch
        {
            Write-Warning "Failed to read envelope file $($file.FullName): $_"
        }
    }
    
    # Force array return even if only one element
    return @($envelopeContents)
}

function SymbolServerUrlFor([string] $UnityPath, [string] $Platform = "")
{
    # Note: Android and iOS have special handling - even though the project is exported while running in docker,
    # the actual build and thus the symbol-server test runs later, on a different machine, outside docker.
    # Therefore, we return localhost regardless of building in docker.
    (!$UnityPath.StartsWith("docker ") -or ($Platform -eq "iOS") -or ($Platform -eq "Android-Export")) `
        ? 'http://localhost:8000' : 'http://172.17.0.1:8000'
}

function RunWithSymbolServer([ScriptBlock] $Callback)
{
    # start the server
    $httpServer = RunApiServer "symbol-upload-server"

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

function CheckSymbolServerOutput([string] $buildMethod, [string] $symbolServerOutput, [string] $unityVersion)
{
    Write-Host "Checking symbol server output" -ForegroundColor Yellow

    # Server stats contains:
    # filename
    #    count = the number of occurrences of the same file name during upload,
    #    chunks = the total number of chunks over all occurrences of a file.
    # We don't check the number of chunks because it depends on the file size.
    $expectedFiles = @()
    $unity_2020_OrHigher = $unityVersion -match "202[0-9]+"
    $unity_2021_OrHigher = $unityVersion -match "202[1-9]+"

    # Currently we only test symbol upload with sources, but we want to keep the values below to also test without in the future.
    # We can have up to 4 different types of files grouped under one name:
    # * the executable itself
    # * the corresponding debug file
    # * the sources if requested
    # * the resolved il2cpp line mapping file
    # For Platforms that pack two different architectures (x64 and arm64 for example)
    # into one file, these numbers are doubled.
    $withSources = $true
    If ($buildMethod.contains('Mac'))
    {
        if ($unity_2020_OrHigher)
        {
            $expectedFiles = @(
                "GameAssembly.dylib: count=$($withSources ? 8 : 6)",
                'IntegrationTest: count=2',
                'Sentry.dylib: count=2',
                "Sentry.dylib.dSYM: count=$($withSources ? 4 : 2)",
                'UnityPlayer.dylib: count=2'
            )
        }
        else
        {
            $expectedFiles = @(
                "GameAssembly.dylib: count=$($withSources ? 3 : 2)",
                'IntegrationTest: count=1',
                'Sentry.dylib: count=2',
                "Sentry.dylib.dSYM: count=$($withSources ? 4 : 2)",
                'UnityPlayer.dylib: count=1'
            )
        }
    }
    ElseIf ($buildMethod.contains('Windows'))
    {
        if ($unity_2020_OrHigher)
        {
            $expectedFiles = @(
                'GameAssembly.dll: count=1',
                "GameAssembly.pdb: count=$($withSources ? 3 : 2)",
                'sentry.dll: count=1',
                "sentry.pdb: count=$($withSources ? 2 : 1)",
                'test.exe: count=1',
                'UnityPlayer.dll: count=1'
            )
        }
        else
        {
            $expectedFiles = @(
                'GameAssembly.dll: count=1',
                "GameAssembly.pdb: count=$($withSources ? 2 : 1)",
                'sentry.dll: count=1',
                "sentry.pdb: count=$($withSources ? 2 : 1)",
                'test.exe: count=1',
                'UnityPlayer.dll: count=1'
            )
        }
    }
    ElseIf ($buildMethod.contains('Linux'))
    {
        $expectedFiles = @(
            'GameAssembly.so: count=1',
            'UnityPlayer.so: count=1',
            'UnityPlayer_s.debug: count=1',
            "libsentry.dbg.so: count=$($withSources ? 2 : 1)",
            'test: count=1',
            'test_s.debug: count=1'
        )
    }
    ElseIf ($buildMethod.contains('Android'))
    {
        $expectedFiles = @(
            'libil2cpp.so: count=1',
            'libmain.so: count=1',
            'libsentry-android.so: count=1',
            'libsentry.so: count=1',
            'libunity.so: count=1',
            'libunity.sym.so: count=1'
        )
    }
    ElseIf ($buildMethod.contains('IOS'))
    {
        if ($unity_2020_OrHigher)
        {
            $expectedFiles = @(
                "IntegrationTest: count=$($withSources ? 3 : 2)",
                'Sentry: count=8',
                "UnityFramework: count=$($withSources ? 5 : 4)",
                'libiPhone-lib.dylib: count=1'
            )
        }
        else
        {
            $expectedFiles = @(
                "IntegrationTest: count=$($withSources ? 3 : 2)",
                'Sentry: count=8',
                "UnityFramework: count=$($withSources ? 4 : 3)",
                'libiPhone-lib.dylib: count=1'
            )
        }
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
    :nextExpectedFile foreach ($file in $expectedFiles)
    {
        $alternatives = ($file -is [array]) ? $file : @($file)
        foreach ($file in $alternatives)
        {
            # It's enough if a single symbol alternative is found
            if ($symbolServerOutput -match "  $([Regex]::Escape($file))\b")
            {
                Write-Host "  $file - OK"
                continue nextExpectedFile
            }
        }
        # Note: control only gets here if none of the alternatives match...
        $fileWithoutCount = $file.Substring(0, $file.Length - 1)
        $filePattern = [Regex]::new('(?<=' + "$([Regex]::Escape($fileWithoutCount))" + ')[\w]+')
        $actualCount = $filePattern.Matches($symbolServerOutput)

        if ("$actualCount" -eq "")
        {
            $successful = $false
        }

        Write-Host "  $alternatives - MISSING `n    Server received '$actualCount' instead." -ForegroundColor Red
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

function RunUnityAndExpect([string] $UnityPath, [string] $name, [string] $successMessage, [string[]] $arguments)
{
    $stdout = RunUnityCustom $UnityPath $arguments -ReturnLogOutput
    $lineWithSuccess = $stdout | Select-String $successMessage
    If ($null -ne $lineWithSuccess)
    {
        Write-Host "`n$name | SUCCESS because the following text was found: '$lineWithSuccess'" -ForegroundColor Green
    }
    Else
    {
        Write-Error "$name | Unity exited without an error but the successMessage was not found in the output ('$successMessage')"
    }
}

function MakeExecutable([string] $file)
{
    If ((Test-Path -Path $file) -and (Get-Command 'chmod' -ErrorAction SilentlyContinue))
    {
        Write-Host -NoNewline "Fixing permission for $file : "
        chmod +x $file
        Write-Host "OK" -ForegroundColor Green
    }
}
