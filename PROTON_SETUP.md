# Proton Setup Guide for BepInEx 6

This document details the steps to run Megabonk Together on Linux using Proton.

## 1. Game Configuration
- **Steam Properties:** Force the use of **Proton 9.0-2** (or newer).
- **Executable:** Verify `Megabonk.exe` exists in the root folder.

## 2. BepInEx Installation (Windows x64)
- **Source:** [BepisBuilds BepInEx 6 BE (Unity.IL2CPP-win-x64)](https://builds.bepinex.dev/projects/bepinex_be)
- **Target:** Extract directly into the game root.
- **Verification:** You should see `winhttp.dll` and a `BepInEx` folder in the root.

## 3. Steam Launch Options
To allow Proton to load the BepInEx `winhttp.dll` hook, you must set the following in Steam Launch Options:
```bash
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

## 4. First Run (Initialization)
1. Launch the game via Steam.
2. BepInEx will appear in a separate terminal window (if enabled).
3. Wait for the main menu. BepInEx is generating `interop` DLLs in the background.
4. Close the game.

## 5. Mod Deployment
Run the `./build.sh` script from the project root. It will:
1. Compile the C# code.
2. Copy the DLLs to `BepInEx/plugins/MegabonkTogether/`.
