# Development Workflow

This document outlines the standard process for building, deploying, and testing changes to Megabonk Together on Linux.

## Environment Requirements
- **SDK:** .NET 8.0 SDK
- **Game:** Megabonk (Windows version via Steam/Proton)
- **Path:** `/home/tyler/.local/share/Steam/steamapps/common/Megabonk`

## Step 1: Build & Deploy
Execute the build script from the root of the repository:
```bash
./build.sh
```
This script handles:
- Code compilation via `dotnet build`.
- Automatic deployment to the game's `plugins` folder.
- Fallback logic for missing game-side assemblies.

## Step 2: Launch & Test
Launch the game via Steam. Monitor the `BepInEx/LogOutput.log` for errors:
```bash
tail -f ~/.local/share/Steam/steamapps/common/Megabonk/BepInEx/LogOutput.log
```

## Step 3: Verify Cross-Play
1. **Join Test:** Attempt to join a room code provided by a Windows user.
2. **Host Test:** Host a room and provide the code to a Windows user.
3. **Sync Check:** Verify that enemies, items, and player movement are synchronized without `PAL_SEHException` or logic desyncs.

## Versioning & Compatibility
- **DO NOT** change the binary structure of messages in `MegabonkTogether.Common`.
- **DO NOT** use C# features newer than **C# 10** unless the Windows build is also updated.
