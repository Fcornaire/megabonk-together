# Megabonk Together - Linux Porting Notes

## Project Overview
This project is a Linux port of the "Megabonk Together" mod. The primary goal is to maintain network compatibility with the original Windows version to allow cross-play.

## Environment Setup
- **OS:** Linux (Fedora 42 - Rawhide/Branched)
- **SDK:** .NET 8.0 (Targeting net6.0)
- **Dependencies:** BepInEx 6 (Bleeding Edge #753) for IL2CPP on Linux.

## Troubleshooting Log (Native Linux)

### Issue: Segfault (SIGSEGV) at startup
- **Symptoms:** Game crashes immediately upon launch with BepInEx enabled. `Player.log` shows `SIGSEGV` at address `0x9`.
- **Root Cause:** `PAL_SEHException` thrown by the BepInEx-bundled CoreCLR (.NET 6) runtime.
- **Analysis:** This indicates a conflict between the older .NET 6 runtime bundled with BepInEx and the newer Linux kernel/GLIBC security features (likely CET - Control-flow Enforcement Technology) present in Fedora 42.

### Attempted Fixes & Results
1.  **Permissions:** `chmod +x libdoorstop.so` -> No change.
2.  **Environment:** `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` -> No change.
3.  **Graphics:** `SDL_VIDEODRIVER=x11`, `-force-vulkan` -> No change.
4.  **Hooking:** Changed `DetourProviderType` to `Dobby` in `BepInEx.cfg` -> No change.
5.  **Security mitigation:** `GLIBC_TUNABLES=glibc.cpu.hwcaps=-IBT,-SHSTK` (to disable CET) -> No change (SELinux AVC denial logged, but permissive mode confirmed).
6.  **Runtime Replacement:** Configuring BepInEx to use the system's .NET 8 runtime instead of the bundled .NET 6. **(Current Status: Pending Verification)**.

## Current Configuration Changes
- **run_bepinex.sh:** Modified to point `coreclr_path` and `corlib_dir` to the system .NET 8 installation (`/usr/lib64/dotnet/shared/Microsoft.NETCore.App/8.0.23`).
- **Game Directory:** Renamed `dotnet` folder to `dotnet.bak` to prevent conflict.
- **BepInEx.cfg:** Reverted to mostly default settings (DetourProvider=Default).

## Build System Changes
- **Directory.Build.props:** Added to define `MegabonkPath` locally.
- **MegabonkTogether.Plugin.csproj:** 
    - Updated `PostBuild` target to use `cp` on Linux.
    - Fixed XML syntax error.
    - Standardized path separators.
    - Added fallback logic to use `stripped-libs`.

## Code Changes
- **HarmonyPatch Ambiguity:** Fixed ambiguous `HarmonyPatch` calls in `Enemy.cs` and `UnityLocalizedString.cs`.
- **Null-Conditional Assignments:** Fixed `CS0131` errors in `NetPlayerCard.cs` and `SynchronizationService.cs`.
- **AutoUpdaterService:** Disabled on Linux.

## Cross-Play Considerations
- **Network Protocol:** `MegabonkTogether.Common` defines shared messages. MUST remain binary compatible.
- **Serialization:** Using `MemoryPack` and `LiteNetLib`.

## Strategic Pivot: Native vs. Proton

### Decision (Feb 8, 2026)
After encountering persistent `PAL_SEHException` crashes with BepInEx 6 on Fedora 42 (Rawhide), we are pivoting to support the **Windows version of the mod running via Proton**.

### Rationale
1.  **Stability:** BepInEx 6 for IL2CPP is significantly more mature and stable on Windows/Proton than on native Linux.
2.  **Compatibility:** Using the Windows binaries ensures 100% network compatibility with the base mod's community.
3.  **Maintenance:** It avoids deep-level debugging of GLIBC/CET/CoreCLR conflicts on bleeding-edge Linux distributions.

### Target Environment (Proton)
- **Proton Version:** Proton 9.0 (current stable) or Proton Experimental.
- **Mod Version:** Windows-compiled DLLs (which we are already building).
- **BepInEx Version:** BepInEx 6 (Windows x64 build).

## Reference Artifacts
- **[PROTON_SETUP.md](./PROTON_SETUP.md):** Step-by-step guide for BepInEx on Proton.
- **[DEVELOPMENT_WORKFLOW.md](./DEVELOPMENT_WORKFLOW.md):** Build, deploy, and verify instructions.

## Final Plan: Achieving Cross-Platform Success
1. **Infrastructure:** Pivot to Proton 9.0 to eliminate runtime environmental differences between our Linux dev environment and Windows players.
2. **Binary Parity:** Build and deploy Windows-native BepInEx plugins that run identically on both platforms.
3. **Validation:** Perform hosting and joining tests with Windows users to confirm logic synchronization.


