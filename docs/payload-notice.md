# 触控增强载荷 —— 来源、改动与许可

## 它是什么

`touch/TouchUILaunch.exe` 与 `touch/TouchUILaunch.dll` 不是本项目编写的代码，而是来自由 **Jerry12228** 开发的开源项目 [TouchUILaunch](https://github.com/Jerry12228/TouchUILaunch)。

- 上游版本：**0.2.1**
- 上游许可：**GNU General Public License v3.0 only（GPL-3.0-only）**
- 它做的事：在游戏运行的内存里识别 Unity 输入接口的位置，替换三个输入回调，让**原版不肯接受触摸的游戏**接受触摸，并把触点喂进游戏本来的触摸接口。支持绝区零、原神、星穹铁道、鸣潮。

## 本项目做了什么改动

**只有一处**：在绝区零载荷里增加了「鼠标作为第三条触摸来源」。

在 `games/zzz/src/payload.cpp` 的 `window_proc` 中，紧跟原有的 `WM_TOUCH` / `WM_POINTER` 两个分支之后新增一个鼠标分支：

```cpp
} else if(mouse_bridge_enabled()&&(message==WM_LBUTTONDOWN||message==WM_LBUTTONUP||(message==WM_MOUSEMOVE&&mouse_down))) {
    ++mouse_messages;
    RECT rect{};GetClientRect(hwnd,&rect);
    const touch::Vec2 position{static_cast<float>(GET_X_LPARAM(lparam))/std::max<LONG>(1,rect.right),
                               static_cast<float>(GET_Y_LPARAM(lparam))/std::max<LONG>(1,rect.bottom)};
    record_input(3,mouse_pointer_id,position,message==WM_LBUTTONDOWN,message==WM_LBUTTONUP,false);
    if(message==WM_LBUTTONDOWN)mouse_down=true;else if(message==WM_LBUTTONUP)mouse_down=false;
}
```

配套要点：

- 复用作者原有的 `record_input()`，与触摸共用同一套触点状态机，**没有改动游戏接收触摸的那条路径**。
- 由环境变量 `TOUCHUI_MOUSE` 控制启用；**不设置时行为与上游原版完全一致**，可随时回退。
- 鼠标移动只在左键按住期间转发，避免对未打开的触点调用 `move()` 造出幽灵触点。
- 另外补了按来源分别计数的诊断输出（`source_downs`），便于区分触摸与鼠标各自贡献了多少触点。

完整改动见同目录的 [payload-mouse-touch.patch](payload-mouse-touch.patch)。

## 为什么需要它

PC 版绝区零的手机 UI 里，虚拟摇杆读的是 Unity 的 `Touchscreen` 设备 —— 鼠标产生不了触摸事件，所以按键能点、摇杆不动。上游项目解决了「游戏肯不肯接受触摸」，本项目补上「触摸从哪来」：**让鼠标也算一路触摸**。这样没有触摸屏、也不用任何串流软件，普通鼠标就能推动那个虚拟摇杆。

实测证据：以游戏关闭状态注入后，日志显示 `WM_TOUCH=0`、`WM_POINTER=0`、`srcdown_mouse=2`，全程零真实触摸事件，画面变化 24.6%，角色确实被鼠标驱动行走。

## 许可与源码提供（GPL-3.0 合规）

本项目分发的发行包中包含上述二进制，因此按 GPL-3.0 的要求：

1. **本项目的全部改动以补丁形式随仓库提供**：见 `docs/payload-mouse-touch.patch`。
2. **完整对应源码**：见发行附件 `payload-source.zip`（含改造后的 `games/zzz/src/payload.cpp` 及其余源码树与上游 LICENSE）。
3. 上游完整源代码可从 <https://github.com/Jerry12228/TouchUILaunch> 获取。
4. 二进制中包含的 HDE64 反汇编器保留其上游许可与版权声明（见上游 `third_party/hde64/LICENSE.txt`）。

本项目自身代码（不含该载荷）以 MIT 发布，见 [LICENSE](../LICENSE)。

## 关于另一个第三方组件

发行包里的 `blur/`（反虚化组件，作者为 B 站 @Ex_M，UID 44434084）同样不是本项目代码，版权归其作者所有，本项目只是打包方便一次性下载。作者若要求移除，我们会立刻删除。
