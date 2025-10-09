#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

$staged = git diff --cached --name-only --diff-filter=ACM |
    Where-Object { $_ -match '\.(ya?ml)$' } |
    Where-Object { $_ -match '^deploy/(k8s|kubernetes)/' } |
    Where-Object { $_ -notmatch '^deploy/kubernetes/helm/' }

if (-not $staged) {
    Write-Host 'kubeconform: no Kubernetes YAML changes staged; skipping.'
    exit 0
}

function Resolve-Kubeconform {
    if ($env:KUBECONFORM_BIN) {
        return $env:KUBECONFORM_BIN
    }

    $fromPath = Get-Command kubeconform -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Definition
    }

    $installDir = Join-Path $root 'tools/kubeconform'
    $binary = Join-Path $installDir 'kubeconform.exe'
    if (Test-Path $binary) {
        return $binary
    }

    if (-not (Test-Path $installDir)) {
        New-Item -ItemType Directory -Path $installDir | Out-Null
    }

    return Download-Kubeconform -InstallDir $installDir
}

function Download-Kubeconform {
    param(
        [Parameter(Mandatory)]
        [string]$InstallDir
    )

    $version = '0.6.7'
    $architecture = $env:PROCESSOR_ARCHITECTURE
    if (-not $architecture) {
        throw 'kubeconform: cannot detect processor architecture. Install kubeconform manually.'
    }

    $archSegment = switch ($architecture.ToLower()) {
        'amd64' { 'amd64' }
        'x86' { throw 'kubeconform: 32-bit architectures are not supported. Install kubeconform manually.' }
        'arm64' { 'arm64' }
        default { throw "kubeconform: unsupported architecture '$architecture' detected. Install kubeconform manually." }
    }

    $archiveName = "kubeconform-windows-$archSegment.zip"
    $url = "https://github.com/yannh/kubeconform/releases/download/v$version/$archiveName"
    $temp = [System.IO.Path]::GetTempFileName()

    Write-Host "kubeconform: downloading $url"
    Invoke-WebRequest -Uri $url -OutFile $temp

    Expand-Archive -Path $temp -DestinationPath $InstallDir -Force
    Remove-Item $temp -Force

    $binary = Join-Path $InstallDir 'kubeconform.exe'
    if (-not (Test-Path $binary)) {
        throw 'kubeconform: failed to obtain kubeconform executable.'
    }

    return $binary
}

$kubeconformPath = Resolve-Kubeconform
$schemaDir = Join-Path $root 'tools/kubeconform/schemas'
$schemaTemplate = "$schemaDir/{{ .NormalizedKubernetesVersion }}-standalone{{ .StrictSuffix }}/{{ .ResourceKind }}{{ .KindSuffix }}.json"

if (-not (Test-Path $schemaDir)) {
    throw "kubeconform: expected schema cache at '$schemaDir' was not found."
}

foreach ($path in $staged) {
    if (-not (Test-Path $path)) {
        throw "kubeconform: staged file '$path' no longer exists."
    }
}

Write-Host "kubeconform: validating $($staged.Count) file(s)"
& $kubeconformPath -schema-location $schemaTemplate -strict @staged
exit $LASTEXITCODE
