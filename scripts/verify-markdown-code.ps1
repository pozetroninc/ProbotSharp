# Verify C# code blocks in Markdown documentation

Write-Host "Markdown C# Code Verifier" -ForegroundColor Yellow
Write-Host

# Get repository root
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

# Build project first to generate assemblies
Write-Host "Building project for assembly references..." -NoNewline
dotnet build --configuration Release --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host " ✓ Build complete" -ForegroundColor Green
    Write-Host
} else {
    Write-Host " ✗ Build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Run verifier
$excludePatterns = "node_modules/,.git/,.aidocs/,TEST-MARKDOWN-VERIFIER.md"
$additionalArgs = $args -join " "
dotnet run --project tools/MarkdownCodeVerifier --configuration Release --no-build -- "$RepoRoot" "$excludePatterns" $additionalArgs
exit $LASTEXITCODE
