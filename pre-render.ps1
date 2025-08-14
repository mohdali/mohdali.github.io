#!/usr/bin/pwsh
Write-Host "Starting Blazor pre-rendering process..." -ForegroundColor Cyan

# Clean output directory
If(Test-Path .\Prerender\output)
{
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item -Path .\Prerender\output -Recurse
}

# Publish Blazor app
Write-Host "Publishing Blazor WebAssembly app..." -ForegroundColor Yellow
dotnet publish src/mohdali.github.io/mohdali.github.io.csproj -c Release -o Prerender/output --nologo

Push-Location .\Prerender

# Decompress index.html if needed
if (-not (Test-Path "output\wwwroot\index.html") -and (Test-Path "output\wwwroot\index.html.gz")) {
    Write-Host "Decompressing index.html.gz..." -ForegroundColor Yellow
    $bytes = [System.IO.File]::ReadAllBytes("output\wwwroot\index.html.gz")
    $stream = New-Object System.IO.MemoryStream(,$bytes)
    $gzip = New-Object System.IO.Compression.GZipStream($stream, [System.IO.Compression.CompressionMode]::Decompress)
    $reader = New-Object System.IO.StreamReader($gzip)
    $content = $reader.ReadToEnd()
    [System.IO.File]::WriteAllText("output\wwwroot\index.html", $content)
    $reader.Close()
    $gzip.Close()
    $stream.Close()
}

# Install dependencies if needed
if (-not (Test-Path "node_modules\playwright")) {
    Write-Host "Installing Playwright..." -ForegroundColor Yellow
    npm install
    # Install browsers for Playwright
    npx playwright install chromium
}

# Run the pre-rendering with Playwright
Write-Host "Pre-rendering pages with Playwright..." -ForegroundColor Yellow
npm run prerender

Pop-Location

# Serve the site locally for testing
Write-Host "Starting local server..." -ForegroundColor Cyan
dotnet serve -o -d Prerender/output/wwwroot/