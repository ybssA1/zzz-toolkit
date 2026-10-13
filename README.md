# 绝区零工具箱（ZZZ Toolkit）

给 PC 版《绝区零》用的一个小面板，打开就三件事：

1. **选游戏路径**（能自动找）
2. **反虚化开关**
3. **手机触控 UI 开关**

![界面](docs/screenshot.png)

> **反虚化组件由 B 站 [@Ex_M](https://space.bilibili.com/44434084)（UID: 44434084）制作，版权归其所有。**
> 本工具只是把它打包进发行包方便一次下载，未做任何修改。作者若要求移除，请提 issue，我们会立刻删除。

---

## 下载与使用

到 [Releases](../../releases) 下载 **`ZZZToolkit-<版本>-portable.zip`**，解压到任意位置（**别放进游戏目录**），双击 `ZZZToolkit.exe`。

包里已经带了反虚化组件，**不需要你再单独下任何东西**：

```
ZZZToolkit.exe
blur\loader.exe      ← Ex_M 的反虚化组件（注入器）
blur\zencen.dll      ← Ex_M 的反虚化组件（载荷）
使用说明.txt
```

| 开关 | 行为 |
|---|---|
| **反虚化** | 勾上后，点「启动游戏」进游戏时会自动应用；也可进游戏后点「立即应用」。已经注入的效果要**重启游戏**才会消失 |
| **手机触控 UI** | 改游戏自己的设置文件，勾完**重启游戏**生效 |

> 发行包分两种：`-portable.zip`（含反虚化组件）和 `-noblur.zip`（不含，只有手机触控 UI）。介意再分发第三方组件的可以直接用后者。

## 两个开关的原理

### 反虚化

关闭「镜头贴近角色时的距离消隐 / 虚化」是**着色器层面的效果**（元数据里能看到 `_DitherAlpha`、`AlphaFade`、`AlliesDitherInCollision` 这类参数），游戏没有提供对应的设置项，只能向游戏进程注入组件并打补丁 —— 这部分由 Ex_M 的 `loader.exe` + `zencen.dll` 完成，**本工具只负责在正确的时机调用它**：

- 游戏启动后自动应用（监视进程，2 秒查一次）
- 或随时手动「立即应用」

相比组件自带的 `autostart.exe`（它要开启 Windows 进程创建审核、再注册一个 4688 事件触发的计划任务），本工具的方式**不改系统审核策略、不留计划任务**。

### 手机触控 UI

PC 客户端本身就带移动端布局，开关是游戏自己设置文件里的一个字段：

```
<游戏>\ZenlessZoneZero_Data\Persistent\LocalStorage\GENERAL_DATA.bin
    "LocalUILayoutPlatform" : 1     ← 手机 / 触屏布局
    "LocalUILayoutPlatform" : 2     ← PC 布局
```

游戏内「设置 → 输入 → UI布局」改的就是它，但**只有检测到触屏设备时那个选项才可选**，普通 PC 上点不到。

这个文件被米哈游用社区称作 **Sleepy** 的格式包着（BinaryFormatter 形状的头 + XOR/逃逸编码），`src/Sleepy.cs` 是它的实现。**写入前会做往返校验**（解码后再编码必须与原文件逐字节一致），校验不过就直接拒绝写入 —— 游戏更新改了格式也不会把设置文件写坏。

> 这个布局是给触屏输入设计的。鼠标推虚拟摇杆不一定有反应 —— 它更常见的用途是把 PC 画面**串流到手机上**玩。

## 自己构建

需要 .NET 8 SDK。

```powershell
# 只出可执行文件
.\build.ps1 -Mode fx                 # 框架依赖版，约 190 KB
.\build.ps1 -Mode self-contained     # 自包含单文件，约 66 MB

# 打便携包（会从 -BlurSource 拷入反虚化组件）
.\package.ps1 -Mode fx -BlurSource "E:\win-x64"
.\package.ps1 -Mode fx -NoBlur       # 不带反虚化组件的版本
```

## 目录结构

```
src/Sleepy.cs        Sleepy 格式编解码（手机触控 UI 用）
src/GameSettings.cs  读写 GENERAL_DATA.bin 里的 LocalUILayoutPlatform
src/GameLocator.cs   从注册表 / 常见路径找游戏
src/BlurPatch.cs     调用反虚化组件
src/MainForm.cs      界面
package.ps1          打便携包
```

## 免责声明

- 本工具会**修改游戏设置文件**、并向**游戏进程注入组件**，属于客户端修改，**可能违反游戏用户协议**。
- 社区里**有人报告**过使用移动端 UI / 触屏模式后被限制联机或封禁（见 CollapseLauncher/Collapse#466 的讨论）。**请自行评估风险，本工具不承担任何后果。**
- 反虚化组件的版权归其作者 **Ex_M** 所有；本仓库的代码（不含该组件）以 MIT 发布，见 [LICENSE](LICENSE)。

## 致谢

- **Ex_M**（B 站 UID 44434084）—— 反虚化组件作者。
- 手机触控 UI 的机制来自社区讨论：`CollapseLauncher/Collapse#466`（ubdjshdb 指出 `LocalUILayoutPlatform`）与 [fffonion/ZZZConfigManager](https://github.com/fffonion/ZZZConfigManager)（Sleepy 编解码实现参考）。
- 另一条更硬的路子：[Jerry12228/TouchUILaunch](https://github.com/Jerry12228/TouchUILaunch)（注入 + 特征码扫描，不依赖设置文件）。
