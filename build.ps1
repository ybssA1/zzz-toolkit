<#
    构建脚本
      框架依赖版（约 190 KB，用户需要装 .NET 8 桌面运行时）
        .\build.ps1 -Mode fx
      自包含单文件版（约 66 MB，用户不需要装任何运行时）
        .\build.ps1 -Mode self-contained
#>
param(
    [ValidateSet('fx', 'self-contained')]
    [string]$Mode = 'fx'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'src'
$out = Join-Path $root ('dist\' + $Mode)

Write-Host "构建模式: $Mode" -ForegroundColor Cyan

if ($Mode -eq 'fx') {
    dotnet publish $proj -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true -o $out --nologo
} else {
    dotnet publish $proj -c Release -r win-x64 -p:SelfContained=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $out --nologo
}

if ($LASTEXITCODE -ne 0) { throw "构建失败，退出码 $LASTEXITCODE" }

$exe = Join-Path $out 'ZZZToolkit.exe'
$size = [math]::Round((Get-Item $exe).Length / 1KB, 0)
Write-Host ""
Write-Host "完成: $exe  ($size KB)" -ForegroundColor Green
if ($Mode -eq 'fx') {
    Write-Host "提示：这是框架依赖版，用户需要安装 .NET 8 桌面运行时。" -ForegroundColor Yellow
    Write-Host "      https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
}
