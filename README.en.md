# ZZZ Toolkit

A small GUI utility for the PC version of *Zenless Zone Zero*:

- **Toggle the mobile / touch-screen UI** — edits the game's own settings file. No injection, no game files touched.
- **Disable the depth-of-field blur** — calls a patch component *you* supply. This project does **not** ship that component.

![Screenshot](docs/screenshot.png)

---

## 1. Mobile / touch UI

The PC client already contains the mobile layout; it is switched by a field in the game's own settings file:

```
<Game>\ZenlessZoneZero_Data\Persistent\LocalStorage\GENERAL_DATA.bin
    "LocalUILayoutPlatform" : 1     <- mobile / touch layout
    "LocalUILayoutPlatform" : 2     <- PC layout
```

The in-game "Settings -> Input -> UI Layout" option edits the same field, but it is only selectable when a touch device is detected — so on a normal PC you cannot reach it from the UI and have to edit the file.

The file is wrapped in what the community calls the **Sleepy** format (a .NET BinaryFormatter-shaped header plus an XOR/escape encoding). `src/Sleepy.cs` implements it. **Before writing**, the tool round-trips the file (decode then re-encode must be byte-identical); if it is not, the write is refused — so a game update that changes the format cannot corrupt your settings.

Restart the game after switching. In-game the HUD becomes the touch layout: round dash/attack buttons in the bottom-right, a virtual joystick area in the bottom-left.

> The touch layout is meant for touch input. Dragging the virtual joystick with a mouse may not respond — the usual use case is **streaming the PC to a phone** (Moonlight, remote-play apps) or a touch-screen laptop.

## 2. Disable the blur

Turning off the close-up depth-of-field / culling shader requires injecting a patch DLL into the game process and replacing rendering shaders. **This project does not provide that payload**, because:

1. it is third-party work with no license, so we have no right to redistribute it;
2. injection binaries are not something users should casually download from an unknown repository.

So this project only provides the *entry point*; you bring the payload:

1. create a `blur` folder next to the executable;
2. put your own patch component (its injector and any DLLs) in it;
3. point "component directory" at it (a `blur` folder next to the executable is detected automatically);
4. with the game running, press "Apply blur patch", or tick "apply automatically when the game starts" and let the tool watch for the process.

The component's entry point is looked up as `loader.exe` by default; if it is missing the UI tells you what to do.

> Instead of the original approach (enabling Windows audit policy plus a process-creation event scheduled task), this tool watches for the game process. It **does not touch system audit policy** and leaves no scheduled task behind.

## 3. Usage

1. Download `ZZZToolkit.exe`; put it anywhere (**not** inside the game folder).
2. Run it. The game path is read from the registry (where the official launcher records it); pick it manually if not found.
3. Press "开启手机 UI" (enable mobile UI) or "应用虚化补丁" (apply blur patch) as needed.

| Button | Purpose |
|---|---|
| 自动检测 / auto-detect | Re-scan the registry and common paths for the game |
| 格式自检 / self-test | Check whether this game version's settings file can be decoded losslessly. Run it after a game update. |
| 还原备份 / restore backup | The first write leaves `GENERAL_DATA.bin.bak`; restore from it here |

Settings live in `zzz-toolkit.json` next to the executable.

## 4. Building

Requires the .NET 8 SDK.

```powershell
# framework-dependent (~190 KB; users need the .NET 8 Desktop Runtime)
.\build.ps1 -Mode fx

# self-contained single file (~66 MB; no runtime needed)
.\build.ps1 -Mode self-contained
```

Output goes to `dist/`.

## 5. Disclaimer — please read

- This tool modifies the game's own settings file and (for the blur patch) the game process memory. That is client modification and **may violate the game's terms of service**.
- Community members have **reported** multiplayer restrictions or bans after using mobile-UI / touch mode (see the discussion in CollapseLauncher/Collapse#466). **Evaluate the risk yourself; this project takes no responsibility for consequences.**
- This project does **not** bundle, download, or recommend any third-party payload.
- A game update may change the settings format — the built-in self-test tells you whether the tool still works.

## 6. Credits

- The touch-UI mechanism comes from the community: the discussion in `CollapseLauncher/Collapse#466` (ubdjshdb identified `LocalUILayoutPlatform`) and [fffonion/ZZZConfigManager](https://github.com/fffonion/ZZZConfigManager) (reference for the Sleepy codec).
- A more robust alternative that does not depend on the settings file: [Jerry12228/TouchUILaunch](https://github.com/Jerry12228/TouchUILaunch) (injection + signature scanning + patching).
- The code in this project (excluding third-party components) is released under the MIT license — see [LICENSE](LICENSE).
