<#
.SYNOPSIS
    Runs Unity of Bugs samples on an Android or iOS device, emulator or simulator, and looks up in Sentry what each
    one sent.

.DESCRIPTION
    Each sample is launched in the app with the `sample` and `run_id` launch arguments: the app runs it, flushes and
    quits, and everything it sends carries the run ID (the `sample.run_id` tag on events, the attribute on logs and
    metrics). The script waits for the app to exit, collects the device log, then finds the run's events, logs and
    metrics in Sentry and checks them against what the sample promises: whether it arrived, whether its stack trace is
    symbolicated, and the context it carries.

    The samples come from samples/unity-of-bugs/Assets/Scripts/Catalogue/SampleCatalogue.cs. Samples that need a tap
    are listed as manual and skipped; samples the build can't run (a missing third-party package) as unavailable.
    Samples that end the app are relaunched once without a sample, so the crash is sent.

    Reading from Sentry needs a token with event:read in SENTRY_API_TOKEN (an org auth token, like the one
    SENTRY_AUTH_TOKEN holds for symbol upload, can only upload). SENTRY_READ_TOKEN is used when SENTRY_API_TOKEN is
    unset.

    The guide is docs/agent-guides/devices.md.

.PARAMETER Platform
    Android or iOS.

.PARAMETER Device
    An adb serial (Android), an iPhone's UDID, or an iOS Simulator's name or UDID. Optional when exactly one device is
    connected (Android) or exactly one iPhone is connected or one simulator booted (iOS).

.PARAMETER Sample
    One or more sample IDs, such as errors/capture-message.

.PARAMETER Category
    A category: the first part of the IDs (errors, crashes, context) or its name in the menu.

.PARAMETER All
    Every sample the platform can run.

.PARAMETER List
    Lists the samples and exits, without a device.

.PARAMETER Build
    Builds the sample (IL2CPP, release) and installs it before running. iOS builds and signs the Xcode project too,
    with the team in the project's settings or APPLE_ID.

.PARAMETER Report
    Writes a Markdown report to this path. The device logs go in a folder next to it.

.PARAMETER TimeoutSeconds
    How long to wait for a sample's app to exit, and for what it sent to arrive in Sentry.

.EXAMPLE
    pwsh scripts/run-sample.ps1 -List

.EXAMPLE
    pwsh scripts/run-sample.ps1 -Platform Android -Build -All -Report .spire/android.md

.EXAMPLE
    pwsh scripts/run-sample.ps1 -Platform iOS -Device "iPhone 17" -Sample errors/caught-exception,context/set-user
#>

[CmdletBinding(DefaultParameterSetName = "Run")]
param(
    [Parameter(ParameterSetName = "Run", Mandatory)]
    [ValidateSet("Android", "iOS")]
    [string] $Platform,

    [Parameter(ParameterSetName = "Run")]
    [string] $Device,

    [Parameter(ParameterSetName = "Run")]
    [string[]] $Sample,

    [Parameter(ParameterSetName = "Run")]
    [string] $Category,

    [Parameter(ParameterSetName = "Run")]
    [switch] $All,

    [Parameter(ParameterSetName = "List", Mandatory)]
    [switch] $List,

    [Parameter(ParameterSetName = "Run")]
    [switch] $Build,

    [Parameter(ParameterSetName = "Run")]
    [string] $Report,

    [Parameter(ParameterSetName = "Run")]
    [int] $TimeoutSeconds = 120
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$projectPath = Join-Path $repoRoot "samples/unity-of-bugs-local"
$sharedPath = Join-Path $repoRoot "samples/unity-of-bugs"
$cataloguePath = Join-Path $sharedPath "Assets/Scripts/Catalogue/SampleCatalogue.cs"

# Samples that drop what they send: nothing may arrive, and there's nothing to wait for beyond this.
$dropWaitSeconds = 45

# Samples an iOS Simulator can't run as a phone does, and why.
$simulatorSkips = @{
    "platform/out-of-memory"  = "phone only: a simulator has no iOS memory limit, so the app would take the Mac's memory"
    "hangs/main-thread-sleep" = "phone only: sentry-cocoa can't capture an App Hang's stack on a simulator"
    "hangs/native-freeze"     = "phone only: sentry-cocoa can't capture an App Hang's stack on a simulator"
}

# ---------------------------------------------------------------------------------------------------------------------
# Catalogue
# ---------------------------------------------------------------------------------------------------------------------

# Reads the samples from the catalogue's source: each entry starts with `new SampleEntry("category/name", "Title",` on
# a line of its own, inside a `new SampleCategory("Name",` block.
function Get-SampleCatalogue {
    $lines = Get-Content $cataloguePath
    $entries = [System.Collections.Generic.List[object]]::new()
    $categoryName = $null
    $current = $null
    $body = ""

    $finish = {
        if ($current) {
            $requirements = @([regex]::Matches($body, 'SampleRequirements\.(\w+)') | ForEach-Object { $_.Groups[1].Value })
            $current.Requirements = $requirements
            $current.Crashes = $body -match 'crashes:\s*true'
            $current.ManualOnly = $body -match 'manualOnly:\s*true'
            $entries.Add([pscustomobject]$current)
        }
    }

    foreach ($line in $lines) {
        $categoryMatch = [regex]::Match($line, '^\s*new SampleCategory\("([^"]+)"')
        if ($categoryMatch.Success) {
            . $finish
            $current = $null
            $categoryName = $categoryMatch.Groups[1].Value
        }

        $entryMatch = [regex]::Match($line, '^\s*new SampleEntry\("([^"]+)",\s*"([^"]+)"')
        if ($entryMatch.Success) {
            . $finish
            $current = [ordered]@{
                Id           = $entryMatch.Groups[1].Value
                Title        = $entryMatch.Groups[2].Value
                Category     = $categoryName
                Description  = $null
                Requirements = @()
                Crashes      = $false
                ManualOnly   = $false
            }
            $body = $line
            continue
        }

        if ($current) {
            $descriptionMatch = [regex]::Match($line, '^\s*"(.*)",\s*$')
            if ($null -eq $current.Description -and $descriptionMatch.Success) {
                $current.Description = $descriptionMatch.Groups[1].Value -replace '\\"', '"'
            }

            $body += "`n$line"
            if ($line -match '^\s*\};') {
                . $finish
                $current = $null
            }
        }
    }

    . $finish
    if ($entries.Count -eq 0) {
        throw "No samples found in $cataloguePath."
    }

    return $entries
}

# Why the platform can't run a sample, or $null when it can. Third-party packages are checked by the app itself.
function Get-PlatformMismatch($entry, [string] $platform) {
    if ($entry.Requirements -contains "Android" -and $platform -ne "Android") { return "Android only" }
    if ($entry.Requirements -contains "IOS" -and $platform -ne "iOS") { return "iOS only" }
    return $null
}

function Select-Samples($catalogue) {
    if ($Sample) {
        $ids = @($Sample | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        $selected = foreach ($id in $ids) {
            $entry = $catalogue | Where-Object Id -eq $id
            if (-not $entry) {
                throw "Unknown sample '$id'. List them with: pwsh scripts/run-sample.ps1 -List"
            }
            $entry
        }
        return @($selected)
    }

    if ($Category) {
        $selected = @($catalogue | Where-Object { ($_.Id -split '/')[0] -eq $Category -or $_.Category -eq $Category })
        if ($selected.Count -eq 0) {
            $names = ($catalogue | ForEach-Object { ($_.Id -split '/')[0] } | Select-Object -Unique) -join ", "
            throw "Unknown category '$Category'. Use one of: $names"
        }
        return @($selected | Where-Object { -not (Get-PlatformMismatch $_ $Platform) })
    }

    if ($All) {
        return @($catalogue | Where-Object { -not (Get-PlatformMismatch $_ $Platform) })
    }

    throw "Name the samples to run: -Sample <id>, -Category <name> or -All. List them with -List."
}

# ---------------------------------------------------------------------------------------------------------------------
# What each sample promises
# ---------------------------------------------------------------------------------------------------------------------

# How a sample's outcome is found and checked. Kind: Event (an error or message event), Transaction, Logs, Metrics,
# Feedback, NoEvent and NoLog (dropped before sending). Prefix: samples run in the same launch first, for samples that
# only change the scope. Then: a sample run after it, whose event shows the change. Check: tests the full event and
# returns what's missing, or $null.
function Get-SampleExpectation([string] $id) {
    switch ($id) {
        "context/set-user" {
            return @{ Kind = "Event"; Then = "errors/capture-message"; Check = {
                    param($e) if ($e.user -and $e.user.email -eq "ant@farm.bug") { $null } else { "user ant@farm.bug missing" } } }
        }
        "context/set-tag" {
            return @{ Kind = "Event"; Then = "errors/capture-message"; Check = {
                    param($e) if ((Get-EventTag $e "sample.tag") -eq "set-from-the-menu") { $null } else { "tag sample.tag missing" } } }
        }
        "context/add-breadcrumb" {
            return @{ Kind = "Event"; Then = "errors/capture-message"; Check = {
                    param($e) if (@(Get-EventBreadcrumbs $e | Where-Object { $_.message -eq "The player picked up a 🐞" }).Count) { $null } else { "breadcrumb missing" } } }
        }
        "context/message-with-context" {
            return @{ Kind = "Event"; Check = {
                    param($e) if ($e.contexts -and $e.contexts.ContainsKey("character")) { $null } else { "character context missing" } } }
        }
        "context/attachment" {
            return @{ Kind = "Event"; Attachment = "sample-attachment.txt" }
        }
        "context/dropped-by-before-send" { return @{ Kind = "NoEvent" } }
        "tracing/transaction" { return @{ Kind = "Transaction"; Name = "Unity of Bugs sample" } }
        "logs/structured-logs" { return @{ Kind = "Logs"; Count = 3 } }
        "logs/sensitive-log" { return @{ Kind = "NoLog" } }
        "logs/metrics" { return @{ Kind = "Metrics"; Names = @("sample.bugs_squashed", "sample.bugs_alive", "sample.squash_time") } }
        "feedback/capture" { return @{ Kind = "Feedback" } }
        { $_ -like "scenes/*" } { return @{ Kind = "Transaction"; Op = "scene.load" } }
        default { return @{ Kind = "Event" } }
    }
}

# ---------------------------------------------------------------------------------------------------------------------
# Listing
# ---------------------------------------------------------------------------------------------------------------------

if ($List) {
    $catalogue = Get-SampleCatalogue
    foreach ($categoryName in ($catalogue.Category | Select-Object -Unique)) {
        Write-Host ""
        Write-Host $categoryName
        foreach ($entry in ($catalogue | Where-Object Category -eq $categoryName)) {
            $notes = @()
            if ($entry.ManualOnly) { $notes += "manual" }
            if ($entry.Crashes) { $notes += "ends the app" }
            $notes += $entry.Requirements
            $suffix = if ($notes.Count) { "  [" + ($notes -join ", ") + "]" } else { "" }
            Write-Host ("  {0,-42} {1}{2}" -f $entry.Id, $entry.Title, $suffix)
        }
    }
    return
}

# ---------------------------------------------------------------------------------------------------------------------
# Project settings
# ---------------------------------------------------------------------------------------------------------------------

function Get-ProjectSetting([string] $name) {
    $settings = Get-Content (Join-Path $projectPath "ProjectSettings/ProjectSettings.asset") -Raw
    if ($settings -match "(?m)^  ${name}: *(.*)$") {
        return $Matches[1].Trim()
    }
    return $null
}

function Get-ApplicationIdentifier([string] $platform) {
    $settings = Get-Content (Join-Path $projectPath "ProjectSettings/ProjectSettings.asset") -Raw
    $key = if ($platform -eq "Android") { "Android" } else { "iPhone" }
    if ($settings -match "(?ms)^  applicationIdentifier:\s*\n(.*?)^  \S" -and $Matches[1] -match "(?m)^    ${key}: *(\S+)") {
        return $Matches[1]
    }
    throw "No $key applicationIdentifier in the sample's ProjectSettings.asset."
}

function Get-SampleDsn {
    $options = Get-Content (Join-Path $sharedPath "Assets/Resources/Sentry/SentryOptions.asset") -Raw
    if ($options -match '<Dsn>k__BackingField: *(\S+)') {
        return $Matches[1]
    }
    throw "No DSN in samples/unity-of-bugs/Assets/Resources/Sentry/SentryOptions.asset."
}

# ---------------------------------------------------------------------------------------------------------------------
# Sentry
# ---------------------------------------------------------------------------------------------------------------------

# Sentry's and Xcode's JSON leaves out fields it has no value for: the functions that read it run with the strict
# mode that allows reading a missing property as $null.
$script:SentryHeaders = $null
$script:SentryBase = $null
$script:SentryOrg = $null
$script:SentryOrgSlug = $null
$script:SentryProject = $null

function Connect-SampleSentry {
    Set-StrictMode -Version 1
    $token = if ($env:SENTRY_API_TOKEN) { $env:SENTRY_API_TOKEN } elseif ($env:SENTRY_READ_TOKEN) { $env:SENTRY_READ_TOKEN } else { $env:SENTRY_AUTH_TOKEN }
    if (-not $token) {
        throw "Set SENTRY_API_TOKEN to a token with event:read, then rerun."
    }

    . (Join-Path $repoRoot "modules/app-runner/import-modules.ps1")
    $dsn = Get-SampleDsn
    Connect-SentryApi -ApiToken $token -DSN $dsn

    $uri = [System.Uri]$dsn
    $script:SentryOrg = [regex]::Match($uri.Host, '^o(\d+)\.').Groups[1].Value
    $script:SentryProject = $uri.AbsolutePath.Trim('/')
    $script:SentryBase = "https://sentry.io/api/0"
    $script:SentryHeaders = @{ Authorization = "Bearer $token" }

    # One cheap read up front, so a token that can't read fails here and not after the first sample.
    try {
        Invoke-WebRequest -Uri "$script:SentryBase/projects/$script:SentryOrg/$script:SentryProject/events/?limit=1" -Headers $script:SentryHeaders | Out-Null
    }
    catch {
        $status = $null
        if ($_.Exception.PSObject.Properties["Response"] -and $_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        }
        if ($status -in @(401, 403)) {
            throw "Sentry refused to read events ($status). Set SENTRY_API_TOKEN to a token with event:read (org auth tokens can only upload), then rerun."
        }
        throw
    }

    $script:SentryOrgSlug = $script:SentryOrg
    try {
        $org = Invoke-SentryRead "organizations/$script:SentryOrg/"
        if ($org.slug) { $script:SentryOrgSlug = $org.slug }
    }
    catch {
        Write-Verbose "Couldn't read the organization's slug: $_"
    }
}

function Invoke-SentryRead([string] $path) {
    Set-StrictMode -Version 1
    $response = Invoke-WebRequest -Uri "$script:SentryBase/$path" -Headers $script:SentryHeaders
    return $response.Content | ConvertFrom-Json -AsHashtable
}

function Get-EventTag($sentryEvent, [string] $key) {
    Set-StrictMode -Version 1
    foreach ($tag in @($sentryEvent.tags)) {
        if ($tag -is [System.Collections.IDictionary] -and $tag.key -eq $key) { return $tag.value }
        if ($tag -is [System.Collections.IList] -and $tag[0] -eq $key) { return $tag[1] }
    }
    return $null
}

function Get-EventEntry($sentryEvent, [string] $type) {
    Set-StrictMode -Version 1
    return @($sentryEvent.entries | Where-Object { $_.type -eq $type })
}

function Get-EventBreadcrumbs($sentryEvent) {
    Set-StrictMode -Version 1
    $entry = Get-EventEntry $sentryEvent "breadcrumbs" | Select-Object -First 1
    if ($entry -and $entry.data -and $entry.data.values) { return @($entry.data.values) }
    return @()
}

# The frames of every stack trace in the event: exceptions, and the threads of a message or a native crash.
function Get-EventFrames($sentryEvent) {
    Set-StrictMode -Version 1
    $frames = [System.Collections.Generic.List[object]]::new()
    foreach ($type in @("exception", "threads")) {
        foreach ($entry in (Get-EventEntry $sentryEvent $type)) {
            foreach ($value in @($entry.data.values)) {
                if ($value -and $value.stacktrace -and $value.stacktrace.frames) {
                    foreach ($frame in $value.stacktrace.frames) { $frames.Add($frame) }
                }
            }
        }
    }
    return $frames
}

# Native frames in the OS's own libraries, or in no library (a thread's last frame, at address 0x0): Sentry has no
# symbols for Android's system libraries (libart, boot.oat) or for the iOS Simulator's runtime (its UIKit and
# CoreFoundation load from the runtime's folder under CoreSimulator), so their missing function names say nothing about
# the app's symbol upload.
$systemLibraryPattern = '^(/apex/|/system/|/system_ext/|/vendor/|/product/|/data/misc/apexdata/|/usr/lib/|/System/|/Developer/)|/Library/Developer/CoreSimulator/Profiles/Runtimes/'

# Symbolicated: every in-app managed frame has a file and a line, and every native frame of the app's own libraries a
# function name rather than a bare address or <unknown>. Returns the verdict, the frames that fall short, and the
# system frames without a name, which don't count against it.
function Test-Symbolicated($sentryEvent) {
    Set-StrictMode -Version 1
    $frames = @(Get-EventFrames $sentryEvent)
    if ($frames.Count -eq 0) {
        return @{ Verdict = "no stack trace"; Problems = @(); SystemFrames = 0 }
    }

    $problems = [System.Collections.Generic.List[string]]::new()
    $systemFrames = 0
    foreach ($frame in $frames) {
        $native = [bool]$frame.instructionAddr
        $function = "$($frame.function)"
        if ($native) {
            if (-not $function -or $function -eq "<unknown>" -or $function -match '^0x[0-9a-f]+$') {
                if (-not $frame.package -or "$($frame.package)" -match $systemLibraryPattern) {
                    $systemFrames++
                }
                else {
                    $problems.Add("native frame $($frame.instructionAddr) in $($frame.package) has no function name")
                }
            }
        }
        elseif ($frame.inApp -and (-not $frame.filename -or -not $frame.lineNo)) {
            $problems.Add("$($frame.module).$function has no file and line")
        }
    }

    $verdict = if ($problems.Count -eq 0) { "yes" } else { "no ($($problems.Count) of $($frames.Count) frames)" }
    return @{ Verdict = $verdict; Problems = @($problems); SystemFrames = $systemFrames }
}

function Get-EventLink($sentryEvent) {
    Set-StrictMode -Version 1
    if ($sentryEvent.groupID) {
        return "https://$script:SentryOrgSlug.sentry.io/issues/$($sentryEvent.groupID)/events/$($sentryEvent.eventID)/"
    }
    return "https://$script:SentryOrgSlug.sentry.io/discover/$($sentryEvent.projectSlug):$($sentryEvent.eventID)/"
}

function Get-EventSummary($sentryEvent) {
    Set-StrictMode -Version 1
    $contexts = if ($sentryEvent.contexts) { $sentryEvent.contexts } else { @{} }
    $device = if ($contexts.ContainsKey("device")) { $contexts.device } else { @{} }
    $os = if ($contexts.ContainsKey("os")) { $contexts.os } else { @{} }
    $user = if ($sentryEvent.user) { (@($sentryEvent.user.GetEnumerator() | Where-Object { $_.Key -in @("id", "email", "username") -and $_.Value } | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ", ") } else { "" }
    $model = @($device.model, $device.name) | Where-Object { $_ } | Select-Object -First 1
    return [ordered]@{
        Title       = $sentryEvent.title
        Release     = if ($sentryEvent.release) { $sentryEvent.release.version } else { $null }
        Environment = Get-EventTag $sentryEvent "environment"
        Device      = $model
        OS          = (@($os.name, $os.version) | Where-Object { $_ }) -join " "
        User        = $user
        Tags        = (@($sentryEvent.tags | ForEach-Object { if ($_ -is [System.Collections.IDictionary]) { "$($_.key)=$($_.value)" } }) -join ", ")
        Contexts    = (@($contexts.Keys | Sort-Object) -join ", ")
        Breadcrumbs = @(Get-EventBreadcrumbs $sentryEvent).Count
    }
}

# Lists the run's error and message events, newest first: id, level and title. A run may send many (a crash that
# leaves the app logging errors), so this reads the list and only the events it judges in full.
function Find-RunEvents([string] $runId) {
    Set-StrictMode -Version 1
    $query = [uri]::EscapeDataString("sample.run_id:$runId")
    $result = Invoke-SentryRead "organizations/$script:SentryOrg/events/?dataset=errors&project=$script:SentryProject&statsPeriod=24h&field=id&field=level&field=title&field=timestamp&sort=-timestamp&per_page=100&query=$query"
    return @($result.data)
}

function Get-RunEvent([string] $eventId) {
    Set-StrictMode -Version 1
    return Invoke-SentryRead "projects/$script:SentryOrg/$script:SentryProject/events/$eventId/"
}

function Find-RunTransactions([string] $runId) {
    Set-StrictMode -Version 1
    $query = [uri]::EscapeDataString("sample.run_id:$runId")
    $result = Invoke-SentryRead "organizations/$script:SentryOrg/events/?dataset=transactions&project=$script:SentryProject&statsPeriod=24h&field=id&field=transaction&field=transaction.op&field=trace&query=$query"
    return @($result.data)
}

function Find-RunFeedback([string] $runId) {
    Set-StrictMode -Version 1
    $query = [uri]::EscapeDataString("issue.category:feedback sample.run_id:$runId")
    return @(Invoke-SentryRead "organizations/$script:SentryOrg/issues/?project=$script:SentryProject&statsPeriod=24h&query=$query")
}

function Find-RunAttachments($sentryEvent) {
    Set-StrictMode -Version 1
    return @(Invoke-SentryRead "projects/$script:SentryOrg/$script:SentryProject/events/$($sentryEvent.eventID)/attachments/")
}

# Looks up what one run sent and judges it. Returns $null while it isn't there yet.
function Test-RunInSentry($run) {
    Set-StrictMode -Version 1
    $expectation = $run.Expectation
    $result = [ordered]@{
        Arrived      = "no"
        Symbolicated = ""
        Passed       = $false
        Details      = ""
        Link         = ""
        Summary      = $null
        Problems     = @()
    }

    switch ($expectation.Kind) {
        "Event" {
            $events = @(Find-RunEvents $run.RunId)
            if ($events.Count -eq 0) { return $null }
            $missing = [System.Collections.Generic.List[string]]::new()
            $count = if ($events.Count -ge 100) { "100 or more events" } else { "$($events.Count) event$(if ($events.Count -ne 1) { 's' })" }
            $listed = $events[0]
            if ($run.Entry.Crashes) {
                # A crash arrives as a fatal event on the next launch; until then, other events of the run may be there.
                $fatal = @($events | Where-Object { $_.level -eq "fatal" })
                if ($fatal.Count -eq 0) {
                    if (-not $run.Final) { return $null }
                    $titles = @($events | ForEach-Object { $_.title } | Select-Object -Unique -First 3) -join "; "
                    $missing.Add("no crash (fatal) event arrived; the run sent $count instead: $titles")
                }
                else {
                    $listed = $fatal[0]
                }
            }
            $sentryEvent = Get-RunEvent $listed.id
            $result.Arrived = "yes ($count)"
            $result.Link = Get-EventLink $sentryEvent
            $result.Summary = Get-EventSummary $sentryEvent
            $symbols = Test-Symbolicated $sentryEvent
            $result.Symbolicated = $symbols.Verdict
            $result.Problems = $symbols.Problems
            if ($symbols.SystemFrames) {
                $result.Summary."System frames without a name" = $symbols.SystemFrames
            }
            if ($expectation.ContainsKey("Check")) {
                $problem = & $expectation.Check $sentryEvent
                if ($problem) { $missing.Add($problem) }
            }
            if ($expectation.ContainsKey("Attachment")) {
                $attachments = @(Find-RunAttachments $sentryEvent)
                if (-not ($attachments | Where-Object { $_.name -eq $expectation.Attachment })) {
                    $missing.Add("attachment $($expectation.Attachment) missing")
                }
                else {
                    $result.Summary.Attachments = ($attachments | ForEach-Object { $_.name }) -join ", "
                }
            }
            $result.Passed = $missing.Count -eq 0
            $result.Details = if ($missing.Count) { $missing -join "; " } else { $sentryEvent.title }
        }
        "Transaction" {
            $transactions = @(Find-RunTransactions $run.RunId)
            if ($expectation.ContainsKey("Name")) { $transactions = @($transactions | Where-Object { $_.transaction -eq $expectation.Name }) }
            if ($expectation.ContainsKey("Op")) { $transactions = @($transactions | Where-Object { $_["transaction.op"] -eq $expectation.Op }) }
            if ($transactions.Count -eq 0) { return $null }
            $transaction = $transactions[0]
            $result.Arrived = "yes"
            $result.Passed = $true
            $result.Details = "$($transaction.transaction) ($($transaction['transaction.op']))"
            $result.Link = "https://$script:SentryOrgSlug.sentry.io/explore/traces/trace/$($transaction.trace)/"
        }
        "Logs" {
            $logs = @((Get-SentryLogsByAttribute -AttributeName "sample.run_id" -AttributeValue $run.RunId).data)
            if ($logs.Count -lt $expectation.Count) { return $null }
            $result.Arrived = "yes ($($logs.Count) logs)"
            $result.Passed = $true
            $result.Details = ($logs | ForEach-Object { "$($_.severity): $($_.message)" }) -join "; "
            $result.Link = "https://$script:SentryOrgSlug.sentry.io/explore/logs/?query=" + [uri]::EscapeDataString("sample.run_id:$($run.RunId)")
        }
        "Metrics" {
            $names = [System.Collections.Generic.List[string]]::new()
            foreach ($name in $expectation.Names) {
                $metrics = @((Get-SentryMetricsByAttribute -MetricName $name -AttributeName "sample.run_id" -AttributeValue $run.RunId).data)
                if ($metrics.Count) { $names.Add($name) }
            }
            if ($names.Count -lt $expectation.Names.Count) { return $null }
            $result.Arrived = "yes"
            $result.Passed = $true
            $result.Details = $names -join ", "
        }
        "Feedback" {
            $feedback = @(Find-RunFeedback $run.RunId)
            if ($feedback.Count -eq 0) { return $null }
            $result.Arrived = "yes"
            $result.Passed = $true
            $result.Details = $feedback[0].title
            $result.Link = $feedback[0].permalink
        }
        "NoEvent" {
            $events = @(Find-RunEvents $run.RunId)
            if ($events.Count) {
                $result.Arrived = "yes"
                $result.Details = "an event arrived that BeforeSend should have dropped"
                $result.Link = Get-EventLink (Get-RunEvent $events[0].id)
            }
            else {
                $result.Arrived = "no, as expected"
                $result.Passed = $true
                $result.Details = "nothing arrived in $dropWaitSeconds s"
            }
        }
        "NoLog" {
            $logs = @((Get-SentryLogsByAttribute -AttributeName "sample.run_id" -AttributeValue $run.RunId).data)
            if ($logs.Count) {
                $result.Arrived = "yes"
                $result.Details = "a log arrived that BeforeSendLog should have dropped: $($logs[0].message)"
            }
            else {
                $result.Arrived = "no, as expected"
                $result.Passed = $true
                $result.Details = "nothing arrived in $dropWaitSeconds s"
            }
        }
    }

    return $result
}

# ---------------------------------------------------------------------------------------------------------------------
# Android
# ---------------------------------------------------------------------------------------------------------------------

function Invoke-Adb {
    $output = & adb -s $script:DeviceId @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "adb $($args -join ' ') failed:`n$($output | Out-String)"
    }
    return $output
}

function Resolve-AndroidDevice {
    if (-not (Get-Command adb -ErrorAction SilentlyContinue)) {
        throw "adb was not found on PATH. Install the Android SDK platform-tools and add them to PATH."
    }

    $devices = @(& adb devices | Select-Object -Skip 1 | Where-Object { $_ -match '^(\S+)\s+device$' } | ForEach-Object { ($_ -split '\s+')[0] })
    if ($Device) {
        if ($devices -notcontains $Device) {
            throw "Android device '$Device' isn't connected and ready. Connected: $($devices -join ', '). Check 'adb devices'."
        }
        return $Device
    }
    if ($devices.Count -ne 1) {
        throw "Found $($devices.Count) Android devices ($($devices -join ', ')): name one with -Device <serial>."
    }
    return $devices[0]
}

function Get-AndroidActivity([string] $package) {
    $dump = Invoke-Adb shell dumpsys package $package | Out-String
    if ($dump -notmatch [regex]::Escape($package)) {
        throw "$package isn't installed on $script:DeviceId. Rerun with -Build."
    }
    if ($dump -match "com.unity3d.player.UnityPlayerGameActivity") {
        return "com.unity3d.player.UnityPlayerGameActivity"
    }
    return "com.unity3d.player.UnityPlayerActivity"
}

function Build-Android {
    & pwsh (Join-Path $PSScriptRoot "build-sample.ps1") -Target Android
    if ($LASTEXITCODE -ne 0) { throw "The Android build failed." }

    # The Gradle task sentryUploadDebugFiles prints sentry-cli's summary into Gradle's build log.
    $gradleLog = Join-Path $projectPath "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/logs/unity-assembleRelease-build.log"
    $upload = @(if (Test-Path $gradleLog) {
            Get-Content $gradleLog | Where-Object { $_ -match '^(> Task :launcher:sentryUploadDebugFiles|> (Found|Resolved|Prepared|Uploaded|File upload|Nothing)|  UPLOADED|  OK)' }
        })
    if ($upload.Count -gt 0) {
        Write-Host "Debug symbol upload ($gradleLog):"
        $upload | ForEach-Object { Write-Host $_ }
    }
    else {
        Write-Warning "No debug symbols were uploaded: set SENTRY_AUTH_TOKEN and rerun with -Build."
    }

    $apk = Join-Path $projectPath "Builds/Android/unity-of-bugs-local.apk"
    Write-Host "Installing $apk on $script:DeviceId"
    Invoke-Adb install -r $apk | Out-Host
}

function Wait-AndroidExit([string] $package, [int] $seconds) {
    $deadline = (Get-Date).AddSeconds($seconds)
    Start-Sleep -Seconds 2
    while ((Get-Date) -lt $deadline) {
        $processId = & adb -s $script:DeviceId shell pidof $package 2>$null
        if (-not $processId) { return $true }
        Start-Sleep -Seconds 1
    }
    return $false
}

function Invoke-AndroidRun([string] $ids, [string] $runId, [string] $logPath, [bool] $crashes) {
    $package = $script:AppId
    Invoke-Adb shell am force-stop $package | Out-Null
    Invoke-Adb logcat -c | Out-Null
    # Streamed rather than dumped at the end: a run that logs a lot overflows the device's log buffer.
    $logcat = Start-Process -FilePath "adb" -ArgumentList @("-s", $script:DeviceId, "logcat") -RedirectStandardOutput $logPath -RedirectStandardError "$logPath.stderr" -PassThru
    try {
        Invoke-Adb shell am start -n "$package/$script:Activity" -e sample $ids -e run_id $runId | Out-Null
        $exited = Wait-AndroidExit $package $TimeoutSeconds
        if (-not $exited) {
            Invoke-Adb shell am force-stop $package | Out-Null
        }

        if ($crashes) {
            # The crash is sent on the next launch.
            Invoke-Adb shell am start -n "$package/$script:Activity" | Out-Null
            Start-Sleep -Seconds 10
            Invoke-Adb shell am force-stop $package | Out-Null
        }
        Start-Sleep -Seconds 1
    }
    finally {
        Stop-Process -Id $logcat.Id -ErrorAction SilentlyContinue
        $logcat.WaitForExit(5000) | Out-Null
        Remove-Item "$logPath.stderr" -ErrorAction SilentlyContinue
    }
    return $exited
}

# ---------------------------------------------------------------------------------------------------------------------
# iOS
# ---------------------------------------------------------------------------------------------------------------------

$script:IosSimulator = $false

function Resolve-IosDevice {
    Set-StrictMode -Version 1
    if (-not (Get-Command xcrun -ErrorAction SilentlyContinue)) {
        throw "xcrun was not found. Install Xcode and select it with xcode-select."
    }

    $json = New-TemporaryFile
    try {
        & xcrun devicectl list devices --quiet --json-output $json.FullName 2>&1 | Out-Null
        $phones = @((Get-Content $json.FullName -Raw | ConvertFrom-Json).result.devices | Where-Object {
                $_.hardwareProperties.reality -eq "physical" -and $_.hardwareProperties.platform -eq "iOS" })
    }
    finally {
        Remove-Item $json.FullName -ErrorAction SilentlyContinue
    }

    $simulators = @(((& xcrun simctl list devices available --json) | ConvertFrom-Json).devices.PSObject.Properties.Value | ForEach-Object { $_ })

    if ($Device) {
        $phone = $phones | Where-Object { $_.hardwareProperties.udid -eq $Device -or $_.identifier -eq $Device -or $_.deviceProperties.name -eq $Device } | Select-Object -First 1
        if ($phone) {
            return @{ Id = $phone.hardwareProperties.udid; Simulator = $false; Name = $phone.deviceProperties.name }
        }
        $simulator = $simulators | Where-Object { $_.udid -eq $Device -or $_.name -eq $Device } | Select-Object -First 1
        if ($simulator) {
            return @{ Id = $simulator.udid; Simulator = $true; Name = $simulator.name }
        }
        throw "No iPhone or simulator '$Device'. Check 'xcrun devicectl list devices' and 'xcrun simctl list devices'."
    }

    $connected = @($phones | Where-Object { $_.connectionProperties.pairingState -eq "paired" })
    if ($connected.Count -eq 1) {
        return @{ Id = $connected[0].hardwareProperties.udid; Simulator = $false; Name = $connected[0].deviceProperties.name }
    }
    $booted = @($simulators | Where-Object { $_.state -eq "Booted" })
    if ($connected.Count -eq 0 -and $booted.Count -eq 1) {
        return @{ Id = $booted[0].udid; Simulator = $true; Name = $booted[0].name }
    }
    throw "Found $($connected.Count) iPhones and $($booted.Count) booted simulators: name one with -Device <udid or simulator name>."
}

function Get-SigningTeam {
    $team = Get-ProjectSetting "appleDeveloperTeamID"
    if (-not $team) { $team = $env:APPLE_ID }
    if (-not $team) {
        throw "No signing team: set APPLE_ID and rerun bootstrap."
    }
    return $team
}

function Get-IosAppPath {
    $configuration = if ($script:IosSimulator) { "Release-iphonesimulator" } else { "Release-iphoneos" }
    $products = Join-Path $projectPath "Builds/iOS-derived/Build/Products/$configuration"
    $app = Get-ChildItem -Path $products -Filter "*.app" -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
    return $app
}

function Build-Ios {
    $sdk = if ($script:IosSimulator) { "Simulator" } else { "Device" }
    & pwsh (Join-Path $PSScriptRoot "build-sample.ps1") -Target iOS -IosSdk $sdk
    if ($LASTEXITCODE -ne 0) { throw "The iOS build failed." }

    $derived = Join-Path $projectPath "Builds/iOS-derived"
    $arguments = @(
        "-project", (Join-Path $projectPath "Builds/iOS/Unity-iPhone.xcodeproj"),
        "-scheme", "Unity-iPhone",
        "-configuration", "Release",
        "-derivedDataPath", $derived
    )
    if ($script:IosSimulator) {
        $arguments += @("-sdk", "iphonesimulator", "-destination", "generic/platform=iOS Simulator")
    }
    else {
        $arguments += @(
            "-destination", "generic/platform=iOS",
            "-allowProvisioningUpdates", "-allowProvisioningDeviceRegistration",
            "CODE_SIGN_STYLE=Automatic", "DEVELOPMENT_TEAM=$(Get-SigningTeam)"
        )
    }

    $log = Join-Path $projectPath "Builds/xcodebuild.log"
    # The build phase that uploads debug symbols writes sentry-cli's output next to the Xcode project.
    $uploadLog = Join-Path $projectPath "Builds/iOS/sentry-symbols-upload.log"
    Remove-Item $uploadLog -ErrorAction SilentlyContinue
    Write-Host "xcodebuild $($arguments -join ' ') (log: $log)"
    & xcodebuild @arguments build *> $log
    if ($LASTEXITCODE -ne 0) {
        Get-Content $log -Tail 40 | Out-Host
        throw "xcodebuild failed with exit code $LASTEXITCODE. See $log."
    }
    if (Test-Path $uploadLog) {
        Write-Host "Debug symbol upload ($uploadLog):"
        Get-Content $uploadLog | Where-Object { $_ -match '^(> |  UPLOADED|  OK|error)' } | ForEach-Object { Write-Host $_ }
    }
    else {
        Write-Warning "No debug symbols were uploaded: set SENTRY_AUTH_TOKEN and rerun with -Build."
    }

    $app = Get-IosAppPath
    if (-not $app) { throw "xcodebuild produced no .app under $derived." }

    Write-Host "Installing $($app.FullName) on $script:DeviceName"
    if ($script:IosSimulator) {
        Start-IosSimulator
        & xcrun simctl install $script:DeviceId $app.FullName
    }
    else {
        & xcrun devicectl device install app --device $script:DeviceId $app.FullName
    }
    if ($LASTEXITCODE -ne 0) { throw "Installing $($app.Name) failed." }
}

function Start-IosSimulator {
    Set-StrictMode -Version 1
    $state = (((& xcrun simctl list devices --json) | ConvertFrom-Json).devices.PSObject.Properties.Value | ForEach-Object { $_ } | Where-Object udid -eq $script:DeviceId).state
    if ($state -ne "Booted") {
        & xcrun simctl boot $script:DeviceId
        & xcrun simctl bootstatus $script:DeviceId | Out-Null
    }
}

# Launches the app with its output streamed to the log, and waits for it to exit.
function Start-IosProcess([string[]] $appArguments, [string] $logPath) {
    if ($script:IosSimulator) {
        & xcrun simctl terminate $script:DeviceId $script:AppId 2>$null | Out-Null
        $arguments = @("simctl", "launch", "--console-pty", "--terminate-running-process", $script:DeviceId, $script:AppId) + $appArguments
    }
    else {
        $arguments = @("devicectl", "device", "process", "launch", "--console", "--terminate-existing", "--device", $script:DeviceId, $script:AppId) + $appArguments
    }
    return Start-Process -FilePath "xcrun" -ArgumentList $arguments -RedirectStandardOutput $logPath -RedirectStandardError "$logPath.stderr" -PassThru
}

function Stop-IosApp {
    Set-StrictMode -Version 1
    if ($script:IosSimulator) {
        & xcrun simctl terminate $script:DeviceId $script:AppId 2>$null | Out-Null
        return
    }

    $json = New-TemporaryFile
    try {
        & xcrun devicectl device info processes --device $script:DeviceId --quiet --json-output $json.FullName 2>&1 | Out-Null
        $processes = @((Get-Content $json.FullName -Raw | ConvertFrom-Json).result.runningProcesses | Where-Object {
                $_.executable -and $_.executable -like "*/$($script:IosExecutable).app/*" })
        foreach ($process in $processes) {
            & xcrun devicectl device process terminate --device $script:DeviceId --pid $process.processIdentifier --quiet 2>&1 | Out-Null
        }
    }
    finally {
        Remove-Item $json.FullName -ErrorAction SilentlyContinue
    }
}

function Wait-IosProcess($process, [int] $seconds) {
    if ($process.WaitForExit($seconds * 1000)) { return $true }
    Stop-IosApp
    if (-not $process.WaitForExit(10000)) { $process.Kill() }
    return $false
}

function Invoke-IosRun([string] $ids, [string] $runId, [string] $logPath, [bool] $crashes) {
    $process = Start-IosProcess @("--sample", $ids, "--run_id", $runId) $logPath
    $exited = Wait-IosProcess $process $TimeoutSeconds

    if ($crashes) {
        # The crash is sent on the next launch.
        $relaunch = Start-IosProcess @() "$logPath.relaunch"
        Start-Sleep -Seconds 10
        Stop-IosApp
        if (-not $relaunch.WaitForExit(10000)) { $relaunch.Kill() }
        Get-Content "$logPath.relaunch" -ErrorAction SilentlyContinue | Add-Content $logPath
        Remove-Item "$logPath.relaunch" -ErrorAction SilentlyContinue
    }

    if (Test-Path "$logPath.stderr") {
        Get-Content "$logPath.stderr" | Add-Content $logPath
        Remove-Item "$logPath.stderr", "$logPath.relaunch.stderr" -ErrorAction SilentlyContinue
    }
    return $exited
}

# ---------------------------------------------------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------------------------------------------------

$catalogue = Get-SampleCatalogue
$selected = @(Select-Samples $catalogue)

$script:AppId = Get-ApplicationIdentifier $Platform
if ($Platform -eq "Android") {
    $script:DeviceId = Resolve-AndroidDevice
    $script:DeviceName = "$((Invoke-Adb shell getprop ro.product.model | Out-String).Trim()) ($script:DeviceId)"
}
else {
    $target = Resolve-IosDevice
    $script:DeviceId = $target.Id
    $script:DeviceName = "$($target.Name) ($($target.Id))"
    $script:IosSimulator = $target.Simulator
}

Connect-SampleSentry

if ($Build) {
    if ($Platform -eq "Android") { Build-Android } else { Build-Ios }
}

if ($Platform -eq "Android") {
    $script:Activity = Get-AndroidActivity $script:AppId
}
else {
    $app = Get-IosAppPath
    $script:IosExecutable = if ($app) { $app.BaseName } else { "unity-of-bugs" }
    if ($script:IosSimulator) { Start-IosSimulator }
}

$reportPath = if ($Report) { [System.IO.Path]::GetFullPath($Report) } else { $null }
$resultsDirectory = if ($reportPath) {
    Join-Path ([System.IO.Path]::GetDirectoryName($reportPath)) ([System.IO.Path]::GetFileNameWithoutExtension($reportPath) + "-logs")
}
else {
    Join-Path $projectPath "Builds/run-sample-logs"
}
New-Item -ItemType Directory -Force -Path $resultsDirectory | Out-Null

$runs = [System.Collections.Generic.List[object]]::new()
foreach ($entry in $selected) {
    $run = [ordered]@{
        Entry       = $entry
        RunId       = [guid]::NewGuid().ToString()
        Expectation = Get-SampleExpectation $entry.Id
        Status      = "pending"
        Exited      = $null
        FinishedAt  = $null
        Log         = $null
        Result      = $null
        Final       = $false
        Note        = ""
    }
    $runs.Add($run)

    $mismatch = Get-PlatformMismatch $entry $Platform
    if (-not $mismatch -and $script:IosSimulator -and $simulatorSkips.ContainsKey($entry.Id)) {
        $mismatch = $simulatorSkips[$entry.Id]
    }
    if ($entry.ManualOnly) {
        $run.Status = "manual"
        $run.Note = "needs a tap: run it from the menu"
        Write-Host "[$($entry.Id)] manual: skipped"
        continue
    }
    if ($mismatch) {
        $run.Status = "unavailable"
        $run.Note = $mismatch
        Write-Host "[$($entry.Id)] $mismatch`: skipped"
        continue
    }

    $ids = $entry.Id
    if ($run.Expectation.ContainsKey("Then")) { $ids = "$ids,$($run.Expectation.Then)" }
    $run.Log = Join-Path $resultsDirectory (($entry.Id -replace '/', '-') + ".log")
    Write-Host "[$($entry.Id)] launching (run $($run.RunId))"

    $exited = if ($Platform -eq "Android") {
        Invoke-AndroidRun $ids $run.RunId $run.Log $entry.Crashes
    }
    else {
        Invoke-IosRun $ids $run.RunId $run.Log $entry.Crashes
    }
    $run.Exited = $exited
    $run.FinishedAt = Get-Date

    $log = Get-Content $run.Log -Raw -ErrorAction SilentlyContinue
    if ($log -match "Sample '[^']+' can't run here: ([^\r\n]+)") {
        $run.Status = "unavailable"
        $run.Note = $Matches[1].Trim()
        Write-Host "[$($entry.Id)] unavailable: $($run.Note)"
        continue
    }

    $run.Status = "sent"
    # A launch the device refused (an iPhone that's locked, say) sends nothing: fail it now, with the reason.
    if ($log -match "(?m)^ERROR: The application failed to launch" -or $log -match "Unable to launch \S+ because ([^\r\n.`")]+)") {
        $reason = if ($log -match "Unable to launch \S+ because ([^\r\n.`")]+)") { "the app didn't launch: $($Matches[1].Trim())" } else { "the app didn't launch" }
        $run.Result = [ordered]@{ Arrived = "no"; Symbolicated = ""; Passed = $false; Details = $reason; Link = ""; Summary = $null; Problems = @() }
        continue
    }
    if (-not $exited) {
        $run.Note = if ($entry.Crashes) { "the app didn't end within $TimeoutSeconds s" } else { "the app didn't exit within $TimeoutSeconds s" }
    }
}

# Look up every run, until each has arrived or its time is up. Drop samples are judged once their shorter wait is over.
foreach ($run in ($runs | Where-Object { $_.Status -eq "sent" })) {
    if ($run.Result) {
        Write-Host "[$($run.Entry.Id)] FAIL - $($run.Result.Details)"
        continue
    }
    $kind = $run.Expectation.Kind
    $drop = $kind -in @("NoEvent", "NoLog")
    $deadline = $run.FinishedAt.AddSeconds($(if ($drop) { $dropWaitSeconds } else { $TimeoutSeconds }))
    Write-Host "[$($run.Entry.Id)] looking up in Sentry"

    while ($true) {
        if ($drop) {
            $wait = ($deadline - (Get-Date)).TotalSeconds
            if ($wait -gt 0) { Start-Sleep -Seconds ([math]::Ceiling($wait)) }
        }
        # The last look: what has arrived by now is judged as it is.
        $run.Final = (Get-Date) -ge $deadline

        try {
            $result = Test-RunInSentry $run
        }
        catch {
            Write-Warning "[$($run.Entry.Id)] Sentry lookup failed: $_"
            $result = $null
        }

        if ($result) {
            $run.Result = $result
            break
        }
        if ($run.Final) {
            break
        }
        Start-Sleep -Seconds 5
    }

    if (-not $run.Result) {
        $run.Result = [ordered]@{ Arrived = "no"; Symbolicated = ""; Passed = $false; Details = "nothing arrived in $TimeoutSeconds s"; Link = ""; Summary = $null; Problems = @() }
    }
    $verdict = if ($run.Result.Passed) { "pass" } else { "FAIL" }
    Write-Host "[$($run.Entry.Id)] $verdict - arrived: $($run.Result.Arrived); symbolicated: $($run.Result.Symbolicated); $($run.Result.Details) $($run.Result.Link)"
}

# ---------------------------------------------------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------------------------------------------------

function Format-Cell([string] $text) {
    return ($text -replace '\|', '\|' -replace "`r?`n", ' ')
}

function Get-RunVerdict($run) {
    switch ($run.Status) {
        "manual" { return "manual" }
        "unavailable" { return "unavailable" }
        default { if ($run.Result -and $run.Result.Passed) { return "pass" } else { return "**fail**" } }
    }
}

$failed = @($runs | Where-Object { $_.Status -eq "sent" -and -not $_.Result.Passed })
$passed = @($runs | Where-Object { $_.Status -eq "sent" -and $_.Result.Passed })

if ($reportPath) {
    $md = [System.Text.StringBuilder]::new()
    [void]$md.AppendLine("# Unity of Bugs on $script:DeviceName")
    [void]$md.AppendLine("")
    [void]$md.AppendLine("$Platform, $script:AppId, $(Get-Date -Format 'yyyy-MM-dd HH:mm'). $($passed.Count) passed, $($failed.Count) failed, $(@($runs | Where-Object Status -in @('manual', 'unavailable')).Count) skipped.")
    [void]$md.AppendLine("")
    [void]$md.AppendLine("| Sample | Result | Arrived | Symbolicated | Details | Link |")
    [void]$md.AppendLine("| --- | --- | --- | --- | --- | --- |")
    foreach ($run in $runs) {
        $result = $run.Result
        $arrived = if ($result) { $result.Arrived } else { "" }
        $symbolicated = if ($result) { $result.Symbolicated } else { "" }
        $details = @($run.Note, $(if ($result) { $result.Details })) | Where-Object { $_ }
        $link = if ($result -and $result.Link) { "[link]($($result.Link))" } else { "" }
        [void]$md.AppendLine("| ``$($run.Entry.Id)`` | $(Get-RunVerdict $run) | $(Format-Cell $arrived) | $(Format-Cell $symbolicated) | $(Format-Cell ($details -join '; ')) | $link |")
    }

    [void]$md.AppendLine("")
    [void]$md.AppendLine("## Details")
    foreach ($run in ($runs | Where-Object Status -eq "sent")) {
        [void]$md.AppendLine("")
        [void]$md.AppendLine("### $($run.Entry.Id)")
        [void]$md.AppendLine("")
        [void]$md.AppendLine("$($run.Entry.Description)")
        [void]$md.AppendLine("")
        [void]$md.AppendLine("- Run ID: ``$($run.RunId)``")
        [void]$md.AppendLine("- Device log: [$([System.IO.Path]::GetFileName($run.Log))]($([System.IO.Path]::GetRelativePath([System.IO.Path]::GetDirectoryName($reportPath), $run.Log)))")
        if ($run.Result.Link) { [void]$md.AppendLine("- Link: $($run.Result.Link)") }
        if ($run.Result.Summary) {
            foreach ($item in $run.Result.Summary.GetEnumerator()) {
                [void]$md.AppendLine("- $($item.Key): $(Format-Cell "$($item.Value)")")
            }
        }
        foreach ($problem in ($run.Result.Problems | Select-Object -First 10)) {
            [void]$md.AppendLine("- Unsymbolicated: $(Format-Cell $problem)")
        }
    }

    New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($reportPath)) | Out-Null
    Set-Content -Path $reportPath -Value $md.ToString() -Encoding utf8
    Write-Host "Report: $reportPath"
}

Write-Host "$($passed.Count) passed, $($failed.Count) failed, $(@($runs | Where-Object Status -in @('manual', 'unavailable')).Count) skipped. Device logs: $resultsDirectory"
if ($failed.Count) {
    exit 1
}
