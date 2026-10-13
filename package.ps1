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
    [string]$TouchSource = 'H:\DSH-Archive\dsh_wechat\_zzz-touchlaunch\src\dist',
    [switch]$NoBlur,
    [string]$Version = '1.3.0'
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

# 2b) 放入触控增强载荷（GPL-3.0，Jerry12228/TouchUILaunch + 本项目的鼠标补丁）
$touchNote = ""
$touchDir = Join-Path $stage 'touch'
New-Item -ItemType Directory -Force -Path $touchDir | Out-Null
$touchNeed = @('TouchUILaunch.exe', 'TouchUILaunch.dll')
$touchMissing = @()
foreach ($f in $touchNeed) {
    $src = Join-Path $TouchSource $f
    if (Test-Path $src) { Copy-Item $src $touchDir -Force } else { $touchMissing += $f }
}
if ($touchMissing.Count -gt 0) {
    Remove-Item $touchDir -Recurse -Force
    $touchNote = "（未含触控载荷：$TouchSource 缺 $($touchMissing -join ', ')）"
} else {
    $touchNote = "（已含触控增强载荷）"
    $notice = @"
触控增强载荷 —— 来源与许可

本目录中的 TouchUILaunch.exe 与 TouchUILaunch.dll 来自开源项目
  Jerry12228/TouchUILaunch  (https://github.com/Jerry12228/TouchUILaunch)
许可：GNU General Public License v3.0 only (GPL-3.0-only)

本项目在其基础上只做了一处改动：在 ZZZ 载荷里增加"鼠标作为第三条触摸来源"
（由环境变量 TOUCHUI_MOUSE 控制），使没有触摸屏的电脑也能用鼠标驱动游戏内的
手机 UI 虚拟摇杆。改动后的完整源码随本项目发布，遵循同一 GPL-3.0 许可。

该载荷同时包含 HDE64 反汇编器（有独立的许可与版权声明，见原项目 third_party）。
完整许可文本见 https://github.com/Jerry12228/TouchUILaunch/blob/master/LICENSE
"@
    Set-Content -Path (Join-Path $touchDir 'LICENSE-和-来源说明.txt') -Value $notice -Encoding UTF8
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
       - 触控增强   ：把鼠标动作当作触摸送进游戏，手机 UI 的虚拟摇杆就能推了。
                      **不需要装任何串流软件，也不需要电脑有触摸屏。**
                      用法：先退出游戏 → 勾上它 → 点"启动游戏"。
                      （游戏必须由本程序启动，载荷只注入自己新建的进程。）
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

四、用手机串流玩（推荐）
  串流软件默认把手机的触摸翻译成鼠标事件，所以游戏里的虚拟摇杆会推不动
  （摇杆认的是真实触摸）。解决办法是在手机端开触控模式，以网易UU远程为例：
      连接后 → [操作] → [屏幕触控] → 打开 [平板触控模式]
  开启后触摸会作为真实触摸传给电脑，手机 UI 的摇杆、双指缩放、按下拖动都能用。
  配合本工具的"手机触控 UI"开关，就是手机原生的操作手感。
  注意：切到手机 UI 后游戏会忽略键盘，所以别再想着把虚拟摇杆映射成 WASD。

五、风险提示
  本工具会修改游戏设置文件、并向游戏进程注入组件，属于客户端修改，可能违反游戏用户协议。
  请自行评估风险。游戏更新后设置文件格式可能变化，届时本工具会拒绝写入而不是写坏文件。
"@
Set-Content -Path (Join-Path $stage '使用说明.txt') -Value $readme -Encoding UTF8

# 4) 压缩
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal

$size = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ""
Write-Host "打包完成：$zip  ($size MB) $blurNote $touchNote" -ForegroundColor Green
