# ZZZ Toolkit

| | |
|---|---|
| **Name** | ZZZ Toolkit |
| **What it does** | One GUI panel for three things in the PC version of *Zenless Zone Zero*: **disable the anti-blur / DOF**, **mobile touch UI**, **touch enhancement** (mouse acts as a finger, so the virtual joystick works) |
| **Download** | [Releases](https://github.com/ybssA1/zzz-toolkit/releases/latest) → `ZZZToolkit-<version>-portable.zip` |
| **Form** | One zip. Unpack, double-click one exe. No command line, no configuration, no extra downloads |
| **Platform** | Windows x64 (.NET 8 Desktop Runtime; not needed for the self-contained build) |
| **License** | This project's own code is MIT. It ships **two third-party components** — see the dedicated sections below |

![Screenshot](docs/screenshot.png)

---

## ⚠️ 1. The anti-blur component — author credit (please read)

**The anti-blur component is not this project's work. It belongs to someone else.**

| | |
|---|---|
| Files | `blur/loader.exe` (injector), `blur/zencen.dll` (payload) |
| **Author** | **@Ex_M on Bilibili** |
| **Author's page** | <https://space.bilibili.com/44434084> |
| **Bilibili UID** | **44434084** |
| **Copyright** | **held by @Ex_M** |
| What we did | **Packaged it into the release so users only download once. No modification, no decompilation, no recompilation.** |
| If the author objects | Open an Issue — **we remove it immediately**, and pull every release containing it |

**Why it is here**: it switches off the shader-level depth-of-field / culling that blurs what the camera sees at close range. The game exposes no setting for this, so it can only be done by injecting a patch into the process — that knowledge and implementation **belong entirely to @Ex_M**. This project's only contribution is putting it in the same panel as two other features and invoking it at the right moment.

**If you do not accept redistributed third-party components**: a `-noblur.zip` release is offered that omits it, keeping only the mobile touch UI and touch enhancement.

---

## 2. Usage

1. Download **`ZZZToolkit-<version>-portable.zip`** from [Releases](https://github.com/ybssA1/zzz-toolkit/releases/latest)
2. Unpack anywhere (**not** inside the game folder)
3. Double-click `ZZZToolkit.exe`
4. It finds the game automatically; if not, click "Browse…" and point at the ZZZ install folder
5. Tick whichever switches you need, then click "Launch game"

Unpacked, it looks like this — everything is in the package, nothing else to download:

```
ZZZToolkit.exe          <- double-click this
blur/loader.exe         <- anti-blur component (by @Ex_M)
blur/zencen.dll         <- anti-blur component (by @Ex_M)
touch/TouchUILaunch.exe <- touch payload (by Jerry12228, GPL-3.0)
touch/TouchUILaunch.dll
```

### What the three switches do

| Switch | Behaviour | When it takes effect |
|---|---|---|
| **Anti-blur** | Disables character blur / close-range culling | Applied when you launch from this tool; or press "Apply now" with the game running. An injected patch disappears only after the game restarts |
| **Mobile touch UI** | Makes the game draw the touch HUD (round skill buttons bottom-right, virtual joystick bottom-left). **Edits the game's own settings file — no injection** | Requires a **game restart** |
| **Touch enhancement** | Feeds mouse input into the game as touch, **so the virtual joystick responds** | Ticked before "Launch game". **The game must be launched by this tool** |

**Two preconditions:**

- Touch enhancement requires the game to be **launched by this tool** — the payload only injects into the process it starts itself; it does not attach to a running game. So: quit the game → tick → "Launch game".
- The first run may ask for **administrator rights** once (injection needs it).

## 3. Does touch enhancement need a streaming app?

**No.** No streaming software is involved (not its driver, not its touch mode, not key remapping). The complete dependency list is three items:

| What | Why |
|---|---|
| The game | PC version of ZZZ |
| This tool + the bundled `touch/` | Already packaged |
| **A mouse** | A physical one works; when streaming, the streaming app presents your finger as a mouse |

How it works: inside the game process the payload synthesises **mouse down / move / up** into touch events and feeds them to the touch interface the game already has. Therefore:

- **No touchscreen and no touch driver** required;
- **No particular streaming software** — UU Remote, ToDesk, Sunlogin, Parsec, Moonlight, Steam Link, Windows RDP… anything that turns your finger into a mouse;
- **Streaming not even required** — pushing the virtual joystick with a plain mouse on the desktop works too.

Measured evidence: injected while the game was closed, the payload log shows `WM_TOUCH=0`, `WM_POINTER=0`, `srcdown_mouse=2` — **zero real touch events throughout** — with 24.6% of the screen changing; the character was indeed walked by the mouse.

---

## 4. Third-party components

This project itself only writes the UI, the orchestration and the settings-file reader/writer. The underlying capability comes from two third-party components, **each owned by its author**:

### 1. Anti-blur component — author: @Ex_M on Bilibili (UID 44434084)

See section "**1. The anti-blur component — author credit**" above. Unmodified; redistributed as-is; removed on request.

### 2. Touch payload — author: Jerry12228

| | |
|---|---|
| Upstream | [Jerry12228/TouchUILaunch](https://github.com/Jerry12228/TouchUILaunch) |
| Version used | **0.2.1** |
| License | **GPL-3.0-only** |
| Our change | **Exactly one**: added "mouse as a third touch source" to the ZZZ payload (upstream only understood Windows touch messages). Gated by the `TOUCHUI_MOUSE` environment variable — **behaviour is identical to upstream when unset** |
| Patch | [`docs/payload-mouse-touch.patch`](docs/payload-mouse-touch.patch) |
| Full corresponding source | release asset `payload-source.zip` (GPL-3.0 requires the source alongside the binary) |
| Details | [`docs/payload-notice.md`](docs/payload-notice.md) |

The payload also contains an HDE64 disassembler; its upstream license and copyright notice are retained.

---

## 5. Referenced projects (community leads)

The mobile-UI route was not discovered by this project — the community got there first. The key leads:

| Source | What it provided |
|---|---|
| [CollapseLauncher/Collapse#466](https://github.com/CollapseLauncher/Collapse/issues/466) | Identified `LocalUILayoutPlatform` as the field behind the PC mobile layout (found by ubdjshdb); also documents the community discussion of touch-mode risk |
| [fffonion/ZZZConfigManager](https://github.com/fffonion/ZZZConfigManager) | Reference for the Sleepy codec — how ZZZ settings `.bin` files are encoded |
| [Jerry12228/TouchUILaunch](https://github.com/Jerry12228/TouchUILaunch) | **This project's touch payload comes entirely from it**; also the origin of the idea of making a game accept touch |
| [winTEuser/Genshin_StarRail_fps_unlocker](https://github.com/winTEuser/Genshin_StarRail_fps_unlocker) | Upstream of part of TouchUILaunch's logic (MIT) |

## 6. What this project implements itself

| File | Role |
|---|---|
| `src/Sleepy.cs` | Codec for the game settings file (round-trip check before writing: decode then re-encode must be byte-identical, otherwise the write is refused) |
| `src/GameSettings.cs` | Reads/writes `LocalUILayoutPlatform` in `GENERAL_DATA.bin` |
| `src/GameLocator.cs` | Finds the game via the registry / common paths |
| `src/BlurPatch.cs` | Calls the anti-blur component at the right moment |
| `src/TouchLaunch.cs` | Launches the game through the modified payload at the right moment |
| `src/MainForm.cs` | The UI (with hand-rolled DPI scaling so text is not clipped at high DPI) |
| `package.ps1` | Builds the portable zip |

## 7. Building

Requires the **.NET 8 SDK**.

```powershell
.\build.ps1 -Mode fx                 # framework-dependent, ~190 KB
.\build.ps1 -Mode self-contained     # self-contained single file, ~66 MB

.\package.ps1 -Mode fx -BlurSource "E:\win-x64" -TouchSource "<payload dist>"
.\package.ps1 -Mode fx -NoBlur       # without the anti-blur component
```

> This repository **does not contain** either third-party binary (see `.gitignore`); they ship only inside the release package. Supply your own when building.

## 8. Disclaimer

- This tool **modifies the game's settings file** and **injects code into the game process**. That is client modification and **may violate the game's terms of service**.
- Community members have **reported** multiplayer restrictions or bans after using mobile-UI / touch mode (see CollapseLauncher/Collapse#466). **Evaluate the risk yourself; this project takes no responsibility for the consequences.**
- The two third-party components remain the property of their authors; see "Third-party components". This project's code is MIT — see [LICENSE](LICENSE).
