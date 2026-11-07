<#
.SYNOPSIS
    Publishes this repository's shared assemblies from a clean, merged main into the drop folder consumers
    re-vendor from, and writes a manifest naming the commit and every file's SHA-256.

.DESCRIPTION
    The drop folder under C:\Workspace\Assemblies is what consuming repositories re-vendor from, each on its own
    leader's Build Order. This script is the one path into it, for Matt and for the coding agent alike, and the
    coding agent runs it only when a Build Order says to publish; never as a side step of a merge.

    It refuses, with the reason named and exit code 1, unless all of these hold:
      1. the checkout is on the branch named by -Branch (main);
      2. the working tree is clean (no modified, staged, deleted or untracked files: an untracked .cs would compile);
      3. HEAD equals origin/<branch> after a fetch, so an unmerged or unpushed commit can never reach the drop folder;
      4. dotnet is on the path.

    It then builds in Release into a staging folder under %TEMP%, in one of two modes:
      - publish mode (-Projects): each project is cleaned and published into the staging folder, documentation
        file included;
      - build mode (-Solution with -OutputProperty): the solution is built once with that MSBuild property set to
        the staging folder, and the repository's own build targets copy each library's output there.
    Then it checks every required file is present, keeps only the files matching -Include, mirrors the staging
    folder into the destination (files the publish did not produce are removed and named, so a stale file cannot
    be vendored by mistake), and writes publish-manifest.json beside the files: repository, mode, informational
    version, commit, branch, UTC time, machine, and each file's size, SHA-256 and (for assemblies) product version.
    The same values are printed, so a hand-back can quote them.

    Pull-request checks already built and tested the merged commit; this script does not run the test suites.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools\publish-assemblies.ps1

    The -ExecutionPolicy Bypass flag is required on a machine whose policy is Restricted; it applies to this one
    process and changes no setting.

.NOTES
    Windows PowerShell 5.1 compatible: no pipeline-chain operators, no ternary, no stderr redirection of native
    commands (a redirected stderr line becomes an ErrorRecord under -ErrorAction Stop even when the exit code is 0).
    The same script, with different defaults, lives in each repository that publishes to C:\Workspace\Assemblies.
#>
[CmdletBinding()]
param(
    [string[]]$Projects = @(),
    [string]$Solution = "src\Roadbed .NET Solution.slnx",
    [string]$OutputProperty = "RoadbedAssembliesDir",
    [string]$Destination = "C:\Workspace\Assemblies\Roadbed",
    [string]$Branch = "main",
    [string]$Include = "Roadbed.*",
    [string]$Repository = "mattonweb/Roadbed",
    [string[]]$RequiredFiles = @(),
    [string]$TestProjectPattern = "^Roadbed\.Test"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestName = "publish-manifest.json"

function Stop-Refused {
    param([string]$Reason)
    Write-Host ""
    Write-Host "REFUSED: $Reason" -ForegroundColor Red
    exit 1
}

function Invoke-GitRead {
    # Runs a read-only git command and returns its stdout lines; refuses on a non-zero exit.
    param([string[]]$Arguments)
    $output = & git -C $repoRoot @Arguments
    if ($LASTEXITCODE -ne 0) {
        Stop-Refused "git $($Arguments -join ' ') exited with $LASTEXITCODE."
    }
    return $output
}

function Invoke-Checked {
    # Runs a native command whose output should stream to the console; refuses on a non-zero exit.
    param([string]$Label, [string]$Command, [string[]]$Arguments)
    Write-Host "> $Command $($Arguments -join ' ')" -ForegroundColor DarkGray
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        Stop-Refused "$Label failed: $Command exited with $LASTEXITCODE."
    }
}

# ---------------------------------------------------------------- mode and inputs

$publishMode = ($Projects.Count -gt 0)
$buildMode = (-not [string]::IsNullOrWhiteSpace($Solution))
if ($publishMode -and $buildMode) {
    Stop-Refused "give -Projects (publish mode) or -Solution (build mode), not both."
}
if (-not $publishMode -and -not $buildMode) {
    Stop-Refused "give -Projects (publish mode) or -Solution with -OutputProperty (build mode)."
}
if ($buildMode -and [string]::IsNullOrWhiteSpace($OutputProperty)) {
    Stop-Refused "build mode needs -OutputProperty, the MSBuild property the repository's targets copy to."
}

$projectPaths = @()
foreach ($project in $Projects) {
    $projectPaths += (Join-Path $repoRoot $project)
}
$solutionPath = $null
if ($buildMode) {
    $solutionPath = Join-Path $repoRoot $Solution
}

# The files that must come out of the build: every library's .dll and .xml. Derived from the projects in
# publish mode, and from the non-test projects beside the solution in build mode, unless given explicitly.
if ($RequiredFiles.Count -eq 0) {
    $libraryNames = @()
    if ($publishMode) {
        foreach ($path in $projectPaths) {
            $libraryNames += [System.IO.Path]::GetFileNameWithoutExtension($path)
        }
    }
    else {
        $solutionFolder = Split-Path -Parent $solutionPath
        foreach ($csproj in @(Get-ChildItem -LiteralPath $solutionFolder -Filter *.csproj -Recurse -Depth 1 -File)) {
            $name = [System.IO.Path]::GetFileNameWithoutExtension($csproj.Name)
            if ($name -notmatch $TestProjectPattern) {
                $libraryNames += $name
            }
        }
    }
    foreach ($name in ($libraryNames | Sort-Object)) {
        $RequiredFiles += ($name + ".dll")
        $RequiredFiles += ($name + ".xml")
    }
}

Write-Host "Repository: $repoRoot ($Repository)"
if ($publishMode) {
    Write-Host "Mode:       publish, $($Projects.Count) project(s)"
    foreach ($project in $Projects) { Write-Host "            $project" }
}
else {
    Write-Host "Mode:       build $Solution with -p:$OutputProperty=<staging>"
}
Write-Host "Include:    $Include"
Write-Host "Drop:       $Destination"
Write-Host ""

# ---------------------------------------------------------------- guards

foreach ($path in $projectPaths) {
    if (-not (Test-Path -LiteralPath $path)) {
        Stop-Refused "project file not found: $path"
    }
}
if ($buildMode -and -not (Test-Path -LiteralPath $solutionPath)) {
    Stop-Refused "solution file not found: $solutionPath"
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    Stop-Refused "dotnet is not on the path."
}

$currentBranch = (Invoke-GitRead @("rev-parse", "--abbrev-ref", "HEAD")) | Select-Object -First 1
if ($currentBranch -ne $Branch) {
    Stop-Refused "the checkout is on '$currentBranch'; a publish comes only from '$Branch'. Check out $Branch and pull first."
}

$dirty = @(Invoke-GitRead @("status", "--porcelain", "--untracked-files=all"))
if ($dirty.Count -gt 0) {
    Stop-Refused ("the working tree is not clean; commit, stash or remove these first:`n  " + ($dirty -join "`n  "))
}

Write-Host "> git fetch origin $Branch" -ForegroundColor DarkGray
& git -C $repoRoot fetch --quiet origin $Branch
if ($LASTEXITCODE -ne 0) {
    Stop-Refused "git fetch origin $Branch exited with $LASTEXITCODE; a publish needs the remote to compare against."
}

$headSha = (Invoke-GitRead @("rev-parse", "HEAD")) | Select-Object -First 1
$originSha = (Invoke-GitRead @("rev-parse", "origin/$Branch")) | Select-Object -First 1
if ($headSha -ne $originSha) {
    Stop-Refused "HEAD ($headSha) is not origin/$Branch ($originSha). Only a merged, pushed $Branch is published: pull, or push, then run again."
}

$headSummary = (Invoke-GitRead @("log", "-1", "--format=%h %ad %s", "--date=short")) | Select-Object -First 1
Write-Host "Publishing commit $headSummary"
Write-Host ""

# ---------------------------------------------------------------- build into staging

$staging = Join-Path $env:TEMP ("publish-assemblies-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $staging | Out-Null

try {
    if ($publishMode) {
        foreach ($path in $projectPaths) {
            Invoke-Checked -Label "clean" -Command "dotnet" -Arguments @("clean", $path, "-c", "Release", "--nologo", "-v", "quiet")
            Invoke-Checked -Label "publish" -Command "dotnet" -Arguments @(
                "publish", $path, "-c", "Release", "-o", $staging, "--nologo", "-v", "quiet",
                "-p:PublishDocumentationFile=true", "-p:PublishDocumentationFiles=true"
            )
        }
    }
    else {
        Invoke-Checked -Label "clean" -Command "dotnet" -Arguments @("clean", $solutionPath, "-c", "Release", "--nologo", "-v", "quiet")
        Invoke-Checked -Label "build" -Command "dotnet" -Arguments @(
            "build", $solutionPath, "-c", "Release", "--nologo", "-v", "quiet", ("-p:" + $OutputProperty + "=" + $staging)
        )
    }

    $missing = @()
    foreach ($required in $RequiredFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $staging $required))) {
            $missing += $required
        }
    }
    if ($missing.Count -gt 0) {
        Stop-Refused ("the build produced no " + ($missing -join ", ") + " in $staging.")
    }

    # ------------------------------------------------------------ mirror into the drop folder

    if (-not (Test-Path -LiteralPath $Destination)) {
        New-Item -ItemType Directory -Path $Destination | Out-Null
    }

    $produced = @(Get-ChildItem -LiteralPath $staging -File | Where-Object { $_.Name -like $Include })
    if ($produced.Count -eq 0) {
        Stop-Refused "no produced file matches -Include '$Include'."
    }
    $producedNames = @{}
    foreach ($file in $produced) {
        $producedNames[$file.Name] = $true
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $Destination $file.Name) -Force
    }

    $removed = @()
    foreach ($existing in @(Get-ChildItem -LiteralPath $Destination -File)) {
        if ($existing.Name -eq $manifestName) { continue }
        if (-not $producedNames.ContainsKey($existing.Name)) {
            Remove-Item -LiteralPath $existing.FullName -Force
            $removed += $existing.Name
        }
    }

    # ------------------------------------------------------------ manifest

    $firstAssembly = Join-Path $Destination $RequiredFiles[0]
    $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($firstAssembly).ProductVersion
    $files = @()
    foreach ($file in @(Get-ChildItem -LiteralPath $Destination -File | Where-Object { $_.Name -ne $manifestName } | Sort-Object Name)) {
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $entry = [ordered]@{ name = $file.Name; bytes = $file.Length; sha256 = $hash }
        if ($file.Extension -eq ".dll") {
            $entry["version"] = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).ProductVersion
        }
        $files += $entry
    }

    $mode = "publish"
    if ($buildMode) { $mode = "build" }
    $manifest = [ordered]@{
        repository           = $Repository
        mode                 = $mode
        projects             = $Projects
        solution             = $Solution
        informationalVersion = $version
        commit               = $headSha
        branch               = $Branch
        publishedUtc         = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        machine              = $env:COMPUTERNAME
        removed              = $removed
        files                = $files
    }
    $json = $manifest | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText((Join-Path $Destination $manifestName), $json, (New-Object System.Text.UTF8Encoding $false))

    # ------------------------------------------------------------ report

    Write-Host ""
    Write-Host "PUBLISHED $Repository ($($files.Count) files, first assembly $($RequiredFiles[0]) $version) from $headSha to $Destination" -ForegroundColor Green
    Write-Host ""
    foreach ($entry in $files) {
        Write-Host ("  {0,-48} {1,10:N0}  {2}" -f $entry.name, $entry.bytes, $entry.sha256)
    }
    if ($removed.Count -gt 0) {
        Write-Host ""
        Write-Host ("  removed (not produced by this publish): " + ($removed -join ", ")) -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "  manifest: $(Join-Path $Destination $manifestName)"
}
finally {
    if (Test-Path -LiteralPath $staging) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
}

exit 0
