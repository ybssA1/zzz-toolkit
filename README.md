# 绝区零工具箱（ZZZ Toolkit）

| | |
|---|---|
| **项目名称** | 绝区零工具箱 · ZZZ Toolkit |
| **用途** | 给 PC 版《绝区零》用一个图形面板管理三件事：**反虚化**、**手机触控 UI**、**触控增强**（鼠标当手指推动虚拟摇杆） |
| **下载** | [Releases](https://github.com/ybssA1/zzz-toolkit/releases/latest) → `ZZZToolkit-<版本>-portable.zip` |
| **形态** | 一个 zip，解压后双击一个 exe 即用。不需要命令行、不需要配置、不需要另外下组件 |
| **平台** | Windows x64（.NET 8 桌面运行时；自包含版不需要） |
| **许可** | 本项目代码 MIT。内含**两个第三方组件**，来源与许可见下方专门章节 |

![界面](docs/screenshot.png)

---

## ⚠️ 一、反虚化组件来源（请务必阅读）

**反虚化组件不是本项目编写的，它是别人的作品。**

| 项目 | 内容 |
|---|---|
| 组件文件 | `blur/loader.exe`（注入器）、`blur/zencen.dll`（载荷） |
| **作者** | **B站 @ExM** |
| **作者主页** | <https://space.bilibili.com/44434084> |
| **B站 UID** | **44434084** |
| **版权** | **归作者 @ExM 所有** |
| 本项目做了什么 | **只是把它打包进发行包，方便用户一次下载就能用。没有修改、没有反编译、没有重新编译。** |
| 作者若要求移除 | 请提 Issue，**我们会立刻删除**，并同时撤下所有含该组件的发行包 |

**为什么它在这里**：这个组件关闭的是「镜头贴近角色时的距离消隐 / 虚化」。那是着色器层面的效果，游戏本身没有提供对应开关，只能向游戏进程注入补丁 —— 这部分的知识与实现**完全属于 @ExM**。本项目的价值只在于把它和另外两个功能放进同一个面板，并在正确的时机调用它。

**如果你不接受再分发第三方组件**：发行包另提供 `-noblur.zip`，不含该组件，只保留手机触控 UI 与触控增强。

---

## 二、用法

1. 到 [Releases](https://github.com/ybssA1/zzz-toolkit/releases/latest) 下载 **`ZZZToolkit-<版本>-portable.zip`**
2. 解压到任意位置（**不要放进游戏目录**）
3. 双击 `ZZZToolkit.exe`
4. 程序会自动找到游戏目录；找不到就点「浏览…」选到绝区零安装目录
5. 需要哪个功能就勾哪个开关，然后点「启动游戏」

解压后目录长这样（三样东西都在包里，不需要再下别的）：

```
ZZZToolkit.exe          ← 双击这个
blur/loader.exe         ← 反虚化组件（@ExM 作品）
blur/zencen.dll         ← 反虚化组件（@ExM 作品）
touch/TouchUILaunch.exe ← 触控增强载荷（Jerry12228 作品，GPL-3.0）
touch/TouchUILaunch.dll
使用说明.txt
```

### 三个开关分别做什么

| 开关 | 行为 | 生效时机 |
|---|---|---|
| **反虚化** | 关闭角色虚化 / 镜头贴近时的距离消隐 | 勾上后点「启动游戏」会自动应用；也可进游戏后点「立即应用」。已注入的效果要**重启游戏**才消失 |
| **手机触控 UI** | 让游戏把 HUD 画成手机布局（右下圆形技能键、左下虚拟摇杆盘）。**改的是游戏自己的设置文件，不注入** | 需**重启游戏** |
| **触控增强** | 把鼠标动作当作触摸送进游戏，**手机 UI 的虚拟摇杆就能推了** | 勾上后点「启动游戏」。**游戏必须由本程序启动** |

**两个前提**：

- 「触控增强」要求游戏**由本程序启动** —— 载荷只注入它自己新建的进程，不会附身已经在跑的游戏。所以流程是：先退出游戏 → 勾上 → 点「启动游戏」。
- 首次运行可能需要同意**一次管理员权限**（注入需要）。

## 三、触控增强需要装串流软件吗？

**不需要。** 这条链路里没有用到任何串流软件（也没有用它们的驱动、触控模式或按键映射）。完整依赖只有三样：

| 需要什么 | 说明 |
|---|---|
| 游戏本体 | PC 版绝区零 |
| 本工具 + 包里的 `touch/` | 已经打包好了 |
| **一个鼠标** | 物理鼠标就行；手机串流时，串流软件替你把手指定为鼠标 |

原理：载荷在游戏进程内部，把**鼠标的按下 / 移动 / 抬起**合成成触摸事件，喂进游戏本来就有的触摸接口。所以：

- **不需要电脑有触摸屏**，也不需要任何触摸驱动；
- **不需要特定的串流软件** —— UU远程、ToDesk、向日葵、Parsec、Moonlight、Steam Link、Windows 远程桌面……任何能把你的手指定成鼠标的软件都可以；
- **不用串流也行** —— 直接在电脑上用鼠标推那个虚拟摇杆，一样动。

实测证据：以游戏关闭状态注入后，载荷日志显示 `WM_TOUCH=0`、`WM_POINTER=0`、`srcdown_mouse=2`，**全程零真实触摸事件**，画面变化 24.6%，角色确实被鼠标驱动行走。

---

## 四、第三方组件清单

本项目自身只写了界面、调度与设置文件读写。真正提供底层能力的是下面两个第三方组件，**版权都属于各自的作者**：

### 1. 反虚化组件 —— 作者：B站 @ExM（UID 44434084）

见上方「**一、反虚化组件来源**」。未作任何修改，仅打包分发；作者要求即删。

### 2. 触控增强载荷 —— 作者：Jerry12228

| 项目 | 内容 |
|---|---|
| 上游仓库 | [Jerry12228/TouchUILaunch](https://github.com/Jerry12228/TouchUILaunch) |
| 使用版本 | **0.2.1** |
| 许可 | **GPL-3.0-only** |
| 本项目改动 | **只有一处**：在绝区零载荷里增加「鼠标作为第三条触摸来源」（原来只认 Windows 触摸消息）。由环境变量 `TOUCHUI_MOUSE` 控制，**不设置时行为与上游原版完全一致** |
| 改动补丁 | [`docs/payload-mouse-touch.patch`](docs/payload-mouse-touch.patch) |
| 完整对应源码 | 见发行附件 `payload-source.zip`（GPL-3.0 要求随二进制提供对应源码） |
| 详细说明 | [`docs/payload-notice.md`](docs/payload-notice.md) |

该载荷内部还包含 HDE64 反汇编器，保留其上游许可与版权声明。

---

## 五、参考的项目（社区线索）

「手机触控 UI」这条路不是本项目发现的，是社区先走通的。最关键的几条线索：

| 来源 | 提供了什么 |
|---|---|
| [CollapseLauncher/Collapse#466](https://github.com/CollapseLauncher/Collapse/issues/466) | 指出 PC 端移动布局由游戏设置字段 `LocalUILayoutPlatform` 控制（ubdjshdb 发现）；也记录了社区关于触屏模式风险的讨论 |
| [fffonion/ZZZConfigManager](https://github.com/fffonion/ZZZConfigManager) | Sleepy 格式编解码的实现参考 —— 绝区零设置文件 `.bin` 的编解码方式 |
| [Jerry12228/TouchUILaunch](https://github.com/Jerry12228/TouchUILaunch) | **本项目的触控载荷完全来自它**；同时它也是「让不肯接受触摸的游戏接受触摸」这一思路的出处 |
| [winTEuser/Genshin_StarRail_fps_unlocker](https://github.com/winTEuser/Genshin_StarRail_fps_unlocker) | TouchUILaunch 部分逻辑的上游（MIT） |

## 六、本项目自己实现的部分

| 文件 | 作用 |
|---|---|
| `src/Sleepy.cs` | 游戏设置文件的编解码（写前做往返校验：解码再编码必须与原文件逐字节一致，不一致就拒绝写入） |
| `src/GameSettings.cs` | 读写 `GENERAL_DATA.bin` 里的 `LocalUILayoutPlatform` |
| `src/GameLocator.cs` | 从注册表 / 常见路径找游戏 |
| `src/BlurPatch.cs` | 在正确时机调用反虚化组件 |
| `src/TouchLaunch.cs` | 在正确时机用改造过的载荷启动游戏并注入 |
| `src/MainForm.cs` | 界面（含自制 DPI 缩放，高 DPI 屏下不裁字） |
| `package.ps1` | 打便携包：把程序、反虚化组件、触控载荷、使用说明装进一个 zip |

## 七、自己构建

需要 **.NET 8 SDK**。

```powershell
# 只出可执行文件
.\build.ps1 -Mode fx                 # 框架依赖版，约 190 KB
.\build.ps1 -Mode self-contained     # 自包含单文件，约 66 MB

# 打便携包（会从 -BlurSource 拷入反虚化组件、从 -TouchSource 拷入触控载荷）
.\package.ps1 -Mode fx -BlurSource "E:\win-x64" -TouchSource "<载荷 dist 目录>"
.\package.ps1 -Mode fx -NoBlur       # 不带反虚化组件的版本
```

> 注意：本仓库**不包含**那两个第三方组件的二进制（见 `.gitignore`），它们只随发行包分发。自己构建时需要自备。

## 八、免责声明

- 本工具会**修改游戏设置文件**、并向**游戏进程注入代码**。这属于客户端修改，**可能违反游戏用户协议**。
- 社区里**有人报告**过使用移动端 UI / 触屏模式后被限制联机或封禁（见 CollapseLauncher/Collapse#466 的讨论）。**请自行评估风险，本项目不承担任何后果。**
- 两个第三方组件的版权归各自作者所有，详见「第三方组件清单」。本项目代码以 MIT 发布，见 [LICENSE](LICENSE)。
