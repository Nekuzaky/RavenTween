<#
.SYNOPSIS
    Builds the competitor benchmark player and compares RavenTween with DOTween, PrimeTween and LitMotion.

.DESCRIPTION
    Builds Benchmarks/CompetitorBench (a separate Unity project that references the package by
    local path) as a Windows release player, then runs one player process per
    (repetition, scenario, tween count, library) in batch mode without graphics, so every library
    starts from a fresh process and a pool sized for its own scenario. Each process measures a
    baseline first (same objects, before any call to the library, so no pool is reserved), then the
    start cost, then the frame time over the measurement window, and appends one row to raw.csv. The script aggregates the repetitions (median) into
    summary.csv and results.md.

    DOTween is optional: its license only allows verbatim redistribution with its readme, so the
    repository does not ship it. Run with -InstallDOTween, which checks the downloaded zip against
    the pinned -DOTweenSha256 (or extract the official zip into
    Benchmarks/CompetitorBench/Assets/Plugins/Demigiant) to include it; without it the benchmark
    skips DOTween.

.EXAMPLE
    ./Tools/Benchmark-Competitors.ps1
    ./Tools/Benchmark-Competitors.ps1 -Repetitions 5 -Counts 1000,10000
    ./Tools/Benchmark-Competitors.ps1 -SkipBuild -Libraries RavenTween,PrimeTween
#>
[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe',
    [ValidateSet('Auto', 'Mono', 'IL2CPP')]
    [string]$Backend = 'Auto',
    [int[]]$Counts = @(1000, 10000, 50000),
    [ValidateSet('Tweens', 'Sequences')]
    [string[]]$Scenarios = @('Tweens', 'Sequences'),
    [string[]]$Libraries = @(),
    [int]$Repetitions = 3,
    [double]$WarmupSeconds = 1,
    [double]$MeasureSeconds = 4,
    [int]$RunTimeoutSeconds = 300,
    [string]$OutputDirectory = '',
    [switch]$SkipBuild,
    [switch]$InstallDOTween,
    [string]$DOTweenVersion = '1_3_045',
    [string]$DOTweenSha256 = '32EBA5C48520A541775D00CB396E18975B83587BA79175DBB878D753725BA5D9'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'Benchmarks\CompetitorBench'
$buildDir = Join-Path $project 'Builds\Windows'
$playerExe = Join-Path $buildDir 'RavenBench.exe'
$logDir = Join-Path $project 'Logs\Benchmark'
if ([string]::IsNullOrEmpty($OutputDirectory)) { $OutputDirectory = Join-Path $repoRoot 'Benchmarks\Results\latest' }
$inv = [System.Globalization.CultureInfo]::InvariantCulture

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }

function Invoke-Process([string]$file, [string[]]$arguments, [int]$timeoutSeconds) {
    $quoted = $arguments | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }
    $process = Start-Process -FilePath $file -ArgumentList ($quoted -join ' ') -PassThru -WindowStyle Hidden
    $null = $process.Handle
    try { $process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::AboveNormal } catch { }
    if (-not $process.WaitForExit($timeoutSeconds * 1000)) {
        Write-Warning "Timed out after $timeoutSeconds s, stopping process $($process.Id) started by this script."
        $process.Kill()
        $process.WaitForExit()
        return -1
    }
    return $process.ExitCode
}

function Install-DOTween {
    $target = Join-Path $project 'Assets\Plugins\Demigiant'
    if (Test-Path (Join-Path $target 'DOTween\DOTween.dll')) { Write-Step 'DOTween already installed'; return }
    $url = "https://dotween.demigiant.com/downloads/DOTween_$DOTweenVersion.zip"
    $zip = Join-Path ([System.IO.Path]::GetTempPath()) "DOTween_$DOTweenVersion.zip"
    Write-Step "Downloading DOTween from $url"
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    $hash = (Get-FileHash -Path $zip -Algorithm SHA256).Hash
    if ($hash -ne $DOTweenSha256.ToUpperInvariant()) {
        Remove-Item $zip
        throw "DOTween_$DOTweenVersion.zip has SHA-256 $hash, expected $DOTweenSha256. Not installed: pass -DOTweenSha256 with the hash of the archive you trust."
    }
    Write-Step "SHA-256 verified: $hash"
    New-Item -ItemType Directory -Force $target | Out-Null
    Expand-Archive -Path $zip -DestinationPath $target -Force
    Remove-Item $zip
}

function Get-Backend {
    if ($Backend -ne 'Auto') { return $Backend }
    $variations = Join-Path (Split-Path -Parent $UnityPath) 'Data\PlaybackEngines\windowsstandalonesupport\Variations'
    if (Test-Path (Join-Path $variations 'win64_player_nondevelopment_il2cpp')) { return 'IL2CPP' }
    Write-Warning 'The Windows IL2CPP module is not installed for this editor: building a Mono release player.'
    return 'Mono'
}

function Get-Median([double[]]$values) {
    $sorted = @($values | Sort-Object)
    $n = $sorted.Count
    if ($n -eq 0) { return [double]::NaN }
    if ($n % 2 -eq 1) { return [double]$sorted[($n - 1) / 2] }
    return ([double]$sorted[$n / 2 - 1] + [double]$sorted[$n / 2]) / 2
}

function Format-Number([double]$value, [string]$format) {
    if ([double]::IsNaN($value)) { return 'n/a' }
    return $value.ToString($format, $inv)
}

function Get-PackageVersion([string]$name) {
    $lock = Get-Content (Join-Path $project 'Packages\packages-lock.json') -Raw | ConvertFrom-Json
    $entry = $lock.dependencies.PSObject.Properties[$name]
    if ($null -eq $entry) { return 'not installed' }
    return $entry.Value.version
}

if (-not (Test-Path $UnityPath)) { throw "Unity editor not found at $UnityPath" }
if ($InstallDOTween) { Install-DOTween }
New-Item -ItemType Directory -Force $logDir, $OutputDirectory | Out-Null
$backendUsed = Get-Backend

if (-not $SkipBuild) {
    Write-Step "Building the $backendUsed release player (first run imports the project, which is slow)"
    $code = Invoke-Process $UnityPath @('-batchmode', '-nographics', '-quit', '-projectPath', $project,
        '-executeMethod', 'RavenBench.EditorTools.BenchBuild.BuildPlayer',
        '-benchBuildPath', $playerExe, '-benchBackend', $backendUsed,
        '-logFile', (Join-Path $logDir 'build.log')) 3600
    if ($code -ne 0 -or -not (Test-Path $playerExe)) { throw "Player build failed (exit $code), see $(Join-Path $logDir 'build.log')" }
}
if (-not (Test-Path $playerExe)) { throw "No player at $playerExe; run without -SkipBuild first." }

$listFile = Join-Path $logDir 'libraries.csv'
$envFile = Join-Path $logDir 'environment.csv'
Remove-Item $listFile, $envFile -ErrorAction SilentlyContinue
$code = Invoke-Process $playerExe @('-batchmode', '-nographics', '-benchList', $listFile, '-benchEnv', $envFile,
    '-logFile', (Join-Path $logDir 'list.log')) 120
if (-not (Test-Path $listFile)) { throw "The player did not list its libraries (exit $code), see $(Join-Path $logDir 'list.log')" }
$available = @(Import-Csv $listFile)
$environment = @{}
Import-Csv $envFile | ForEach-Object { $environment[$_.key] = $_.value }

$selected = @($available | Where-Object { $Libraries.Count -eq 0 -or $Libraries -contains $_.library } | ForEach-Object { $_.library })
if ($selected.Count -eq 0) { throw 'No library to benchmark.' }
if (-not ($available | Where-Object { $_.library -eq 'DOTween' })) {
    Write-Warning 'DOTween is not installed in Benchmarks/CompetitorBench: it is skipped (use -InstallDOTween).'
}
Write-Step ("Libraries: " + ($selected -join ', ') + "; backend $($environment['backend']); CPU $($environment['cpu'])")

$raw = Join-Path $OutputDirectory 'raw.csv'
Remove-Item $raw -ErrorAction SilentlyContinue
$total = $Repetitions * $Scenarios.Count * $Counts.Count * $selected.Count
$done = 0
for ($rep = 1; $rep -le $Repetitions; $rep++) {
    foreach ($scenario in $Scenarios) {
        foreach ($count in $Counts) {
            for ($k = 0; $k -lt $selected.Count; $k++) {
                $library = $selected[($k + $rep - 1) % $selected.Count]
                $done++
                Write-Step "[$done/$total] rep $rep, $scenario x $count, $library"
                $log = Join-Path $logDir ("run-{0}-{1}-{2}-{3}.log" -f $rep, $scenario, $count, $library)
                $code = Invoke-Process $playerExe @('-batchmode', '-nographics',
                    '-benchLibrary', $library, '-benchScenario', $scenario, '-benchCount', "$count",
                    '-benchRep', "$rep", '-benchWarmup', $WarmupSeconds.ToString($inv),
                    '-benchMeasure', $MeasureSeconds.ToString($inv), '-benchOut', $raw, '-logFile', $log) $RunTimeoutSeconds
                if ($code -ne 0) { Write-Warning "$library $scenario x $count exited with $code, see $log" }
            }
        }
    }
}
if (-not (Test-Path $raw)) { throw 'No result was written.' }

$rows = @(Import-Csv $raw)
$summary = foreach ($group in ($rows | Group-Object scenario, count, library)) {
    $first = $group.Group[0]
    $metric = { param($name) Get-Median @($group.Group | ForEach-Object { [double]::Parse($_.$name, $inv) }) }
    [pscustomobject]@{
        scenario = $first.scenario
        count = [int]$first.count
        library = $first.library
        runs = $group.Count
        start_ms = & $metric 'start_ms'
        start_alloc_kb = (& $metric 'start_alloc_bytes') / 1024
        update_avg_ms = & $metric 'update_avg_ms'
        update_p95_ms = & $metric 'update_p95_ms'
        frame_avg_ms = & $metric 'frame_avg_ms'
        baseline_avg_ms = & $metric 'baseline_avg_ms'
        gc_bytes_per_frame = & $metric 'gc_bytes_per_frame'
        gc_collections = & $metric 'gc_collections'
        moved_ratio = (& $metric 'moved') / [int]$first.count
    }
}
$summary = @($summary | Sort-Object scenario, count, @{ Expression = { $_.update_avg_ms } })
$summary | ForEach-Object {
    $o = $_
    [pscustomobject]@{
        scenario = $o.scenario; count = $o.count; library = $o.library; runs = $o.runs
        start_ms = Format-Number $o.start_ms '0.00'; start_alloc_kb = Format-Number $o.start_alloc_kb '0.0'
        update_avg_ms = Format-Number $o.update_avg_ms '0.000'; update_p95_ms = Format-Number $o.update_p95_ms '0.000'
        frame_avg_ms = Format-Number $o.frame_avg_ms '0.000'; baseline_avg_ms = Format-Number $o.baseline_avg_ms '0.000'
        gc_bytes_per_frame = Format-Number $o.gc_bytes_per_frame '0.0'; gc_collections = Format-Number $o.gc_collections '0'
        moved_ratio = Format-Number $o.moved_ratio '0.00'
    }
} | Export-Csv (Join-Path $OutputDirectory 'summary.csv') -NoTypeInformation -Encoding UTF8

$versions = [ordered]@{
    'RavenTween' = (Get-Content (Join-Path $repoRoot 'Packages\com.raventween.core\package.json') -Raw | ConvertFrom-Json).version
    'PrimeTween' = Get-PackageVersion 'com.kyrylokuzyk.primetween'
    'LitMotion' = Get-PackageVersion 'com.annulusgames.lit-motion'
    'DOTween' = 'not installed'
}
$ravenCommit = (& git -C $repoRoot log -1 --format=%h -- 'Packages/com.raventween.core' 2>$null)
$ravenDirty = (& git -C $repoRoot status --porcelain -- 'Packages/com.raventween.core' 2>$null)
if ($ravenCommit) {
    $suffix = $ravenCommit
    if ($ravenDirty) { $suffix += ' + uncommitted changes' }
    $versions['RavenTween'] = "$($versions['RavenTween']) (commit $suffix)"
}
$dotween = $available | Where-Object { $_.library -eq 'DOTween' }
if ($dotween) { $versions['DOTween'] = $dotween.version }
$cpu = (Get-CimInstance Win32_Processor | Select-Object -First 1).Name.Trim()

$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine('# Tween library benchmark')
[void]$md.AppendLine()
[void]$md.AppendLine("Generated by ``Tools/Benchmark-Competitors.ps1`` on $((Get-Date).ToString('yyyy-MM-dd', $inv)).")
[void]$md.AppendLine()
[void]$md.AppendLine('| Setting | Value |')
[void]$md.AppendLine('| :--- | :--- |')
[void]$md.AppendLine("| CPU | $cpu ($($environment['logical_cores']) logical cores) |")
[void]$md.AppendLine("| OS | $($environment['os']) |")
[void]$md.AppendLine("| Unity | $($environment['unity_version']), Windows player, release (non-development) |")
[void]$md.AppendLine("| Scripting backend | $($environment['backend']) |")
[void]$md.AppendLine("| Run mode | ``-batchmode -nographics``, one process per measurement, vSync off, no frame cap |")
[void]$md.AppendLine("| Timing | warm-up $WarmupSeconds s, measured $MeasureSeconds s, median of $Repetitions repetitions |")
[void]$md.AppendLine("| GC measurement | $($environment['alloc_method']) |")
foreach ($key in $versions.Keys) { [void]$md.AppendLine("| $key | $($versions[$key]) |") }
[void]$md.AppendLine()
[void]$md.AppendLine('**Update** is the CPU time of a whole player-loop frame minus the same process''s baseline frame, measured with the same objects before the library is touched (no pool reserved, no tween created): it is what the library adds per frame, its idle pool walk and transform writes included. **p95** is the 95th percentile frame minus the baseline median. **Start** is the time to create all tweens or sequences in one frame, with each library''s pool reserved beforehand.')
foreach ($scenario in $Scenarios) {
    [void]$md.AppendLine()
    if ($scenario -eq 'Tweens') { [void]$md.AppendLine('## N position tweens (InOutQuad, 20 s)') }
    else { [void]$md.AppendLine('## N sequences of 3 chained position tweens (InOutQuad, 2 s each)') }
    [void]$md.AppendLine()
    [void]$md.AppendLine('| N | Library | Start (ms) | Update avg (ms/frame) | Update p95 (ms/frame) | GC per frame (B) | GC at start (KB) |')
    [void]$md.AppendLine('| ---: | :--- | ---: | ---: | ---: | ---: | ---: |')
    foreach ($row in ($summary | Where-Object { $_.scenario -eq $scenario })) {
        $moved = ''
        if ($row.moved_ratio -lt 0.99) { $moved = ' (not all targets moved)' }
        [void]$md.AppendLine([string]::Format($inv, "| {0:N0} | {1}{2} | {3} | {4} | {5} | {6} | {7} |", $row.count, $row.library, $moved,
            (Format-Number $row.start_ms '0.00'), (Format-Number $row.update_avg_ms '0.000'), (Format-Number $row.update_p95_ms '0.000'),
            (Format-Number $row.gc_bytes_per_frame '0'), (Format-Number $row.start_alloc_kb '0')))
    }
}
$mdPath = Join-Path $OutputDirectory 'results.md'
[System.IO.File]::WriteAllText($mdPath, $md.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Step "Results: $mdPath"
Get-Content $mdPath
