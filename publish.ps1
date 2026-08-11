$ErrorActionPreference = 'Stop'

dotnet publish `
    'src\CodexUsageWidget\CodexUsageWidget.csproj' `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -o 'publish'

Write-Host "Published to $((Resolve-Path 'publish').Path)"
