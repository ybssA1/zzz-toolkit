<#
    打包成"用户只下一次"的便携包。

    产物：dist\ZZZToolkit-<版本>-portable.zip
      内容：ZZZToolkit.exe
            blur\loader.exe        <- Ex_M 的反虚化组件（注入器）
            blur\zencen.dll        <- Ex_M 的反虚化组件（载荷）
            使用说明.txt

    参数：
      -Mode            fx（默认，约 190 KB，需 .NET 8 桌面运行时）
                       self-contained（约 66 MB，用户不用装运行时）
      -BlurSource      反虚化组件的来源目录，默认 E:\win-x64
      -NoBlur          打一个不带反虚化组件的包（只有手机触控 UI 功能）
      -Version         版本号，默认取 csproj
#>
param(
    [ValidateSet('fx', 'self-contained')]
    [string]$Mode = 'fx',
    [string]$BlurSource = 'E:\win-x64',
    [switch]$NoBlur,
    [string]$Version = '1.1.0'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'src'
$stage = Join-Path $root 'dist\package'
if ($NoBlur) { $suffix = '-noblur' } else { $suffix = '-portable' }
$zip = Join-Path $root ("dist\ZZZToolkit-$Version$suffix.zip")

# 1) 发布本体
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
if ($Mode -eq 'fx') {
    dotnet publish $proj -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true -o $stage --nologo
} else {
    dotnet publish $proj -c Release -r win-x64 -p:SelfContained=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $stage --nologo
}
if ($LASTEXITCODE -ne 0) { throw "发布失败，退出码 $LASTEXITCODE" }

# 2) 放入反虚化组件
$blurNote = ""
if (-not $NoBlur) {
    $blurDir = Join-Path $stage 'blur'
    New-Item -ItemType Directory -Force -Path $blurDir | Out-Null
    $need = @('loader.exe', 'zencen.dll')
    $missing = @()
    foreach ($f in $need) {
        $src = Join-Path $BlurSource $f
        if (Test-Path $src) { Copy-Item $src $blurDir -Force }
        else { $missing += $f }
    }
    if ($missing.Count -gt 0) {
        Write-Host "警告：$BlurSource 里缺少 $($missing -join ', ')，打出来的包不含反虚化组件。" -ForegroundColor Yellow
        Remove-Item $blurDir -Recurse -Force
        $blurNote = "（本次未放入反虚化组件）"
    } else {
        $blurNote = "（已包含 Ex_M 的反虚化组件）"
    }
} else {
    $blurNote = "（按要求未包含反虚化组件）"
}

# 3) 使用说明
$readme = @"
绝区零工具箱 $Version

一、怎么用
  1. 把整个文件夹解压到任意位置（不要放进游戏目录）。
  2. 双击 ZZZToolkit.exe。
  3. 程序会自动找到游戏路径；找不到就点"浏览…"选到绝区零安装目录。
  4. 需要哪个功能就勾哪个开关：
       - 反虚化     ：勾上后，点"启动游戏"进游戏会自动生效（也可进游戏后点"立即应用"）。
                      已经注入的效果要重启游戏才会消失。
       - 手机触控UI ：改的是游戏自己的设置，勾完重启游戏生效。
  5. 关掉程序后设置会记住，下次打开还是上次的状态。

二、反虚化组件署名
  反虚化组件（blur 文件夹里的 loader.exe 与 zencen.dll）由 B 站 @Ex_M 制作，版权归其所有。
      B 站主页：https://space.bilibili.com/44434084
  本工具只是把它打包进来，方便一次性下载；没有对组件做任何修改。
  如果作者要求移除，请到项目仓库提 issue，我们会立刻删除。

三、常见问题
  - 打开就提示缺少 .NET：装一下 .NET 8 桌面运行时
      https://dotnet.microsoft.com/download/dotnet/8.0
  - 杀毒软件报警：注入类工具常被误报，把整个文件夹加白名单即可。
  - 反虚化没效果：确认游戏已经在运行，再看程序里的日志。
  - 手机触控UI 点了没变化：这个改动要重启游戏才生效。

四、风险提示
  本工具会修改游戏设置文件、并向游戏进程注入组件，属于客户端修改，可能违反游戏用户协议。
  请自行评估风险。游戏更新后设置文件格式可能变化，届时本工具会拒绝写入而不是写坏文件。
"@
Set-Content -Path (Join-Path $stage '使用说明.txt') -Value $readme -Encoding UTF8

# 4) 压缩
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal

$size = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ""
Write-Host "打包完成：$zip  ($size MB) $blurNote" -ForegroundColor Green
