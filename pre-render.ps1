#!/usr/bin/pwsh
If(Test-Path .\Prerender\output)
{
    Remove-Item -Path .\Prerender\output -Recurse
}
 
dotnet publish src/mohdali.github.io/mohdali.github.io.csproj -c Release -o Prerender/output --nologo

Push-Location .\Prerender

# Run react-snap and ignore non-zero exit codes
npx react-snap 2>&1 | Out-Host
$reactSnapExitCode = $LASTEXITCODE

# Check if at least the index.html was created
if (Test-Path ".\output\wwwroot\index.html") {
    Write-Host "Pre-rendering completed successfully (index.html found)" -ForegroundColor Green
} else {
    Write-Host "Pre-rendering failed - no index.html generated" -ForegroundColor Red
    exit 1
}

Get-ChildItem ".\output\wwwroot\*.html" -Recurse | ForEach-Object { 
    $HtmlFileContent = (Get-Content -Path $_.FullName -Raw);
    
    # Remove .NET 9 Blazor autostart script
    $HtmlFileContent = $HtmlFileContent -replace '<script src="_framework/blazor\.webassembly\.js" autostart="false"></script>', ''
    
    # Remove any Blazor.start scripts
    $HtmlFileContent = $HtmlFileContent -replace '<script>Blazor\.start.*?</script>', ''
    
    # Remove dotnet.js references (matches any version)
    $HtmlFileContent = $HtmlFileContent -replace '<script src="_framework/dotnet\.[^"]*\.js"[^>]*></script>', ''
    
    Set-Content -Path $_.FullName -Value $HtmlFileContent -NoNewline
}

Pop-Location

dotnet serve -o -dprerender/output/wwwroot/