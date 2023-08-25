#!/usr/bin/pwsh
If(Test-Path .\Prerender\output)
{
    Remove-Item -Path .\Prerender\output -Recurse
}
 
dotnet publish src/mohdali.github.io/mohdali.github.io.csproj -c Release -o Prerender/output --nologo

Push-Location .\Prerender

npx react-snap

Get-ChildItem ".\output\wwwroot\*.html" -Recurse | ForEach-Object { 
    $HtmlFileContent = (Get-Content -Path $_.FullName -Raw);
    $HtmlFileContent = $HtmlFileContent.Replace('<script>var Module; window.__wasmmodulecallback__(); delete window.__wasmmodulecallback__;</script><script src="_framework/dotnet.6.0.7.8zwu4egow5.js" defer="" integrity="sha256-pvJcJ6jtDSfxgXm03QYRTm4aAi9PSzMSJODReHtvASY=" crossorigin="anonymous"></script>','')
    Set-Content -Path $_.FullName -Value $HtmlFileContent
}

Pop-Location

dotnet serve -o -dprerender/output/wwwroot/