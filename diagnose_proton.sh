#!/bin/bash

GAME_PATH="/home/tyler/.local/share/Steam/steamapps/common/Megabonk"
echo "--- Megabonk Together: Proton Diagnostic Tool ---"

# 1. Check for Windows Executable
if [ -f "$GAME_PATH/Megabonk.exe" ]; then
    echo "[PASS] Megabonk.exe found."
else
    echo "[FAIL] Megabonk.exe NOT found. Is the Windows version downloaded?"
fi

# 2. Check for BepInEx Hook
if [ -f "$GAME_PATH/winhttp.dll" ]; then
    echo "[PASS] winhttp.dll (BepInEx Hook) found."
else
    echo "[WARN] winhttp.dll NOT found. BepInEx is not installed yet."
fi

# 3. Check for Interop DLLs
if [ -d "$GAME_PATH/BepInEx/interop" ] && [ "$(ls -A $GAME_PATH/BepInEx/interop)" ]; then
    echo "[PASS] Interop DLLs exist."
else
    echo "[INFO] Interop DLLs not found. They will generate on first run."
fi

# 4. Check for Mod DLLs
if [ -f "$GAME_PATH/BepInEx/plugins/MegabonkTogether/MegabonkTogether.dll" ]; then
    echo "[PASS] Mod is deployed."
else
    echo "[INFO] Mod is NOT deployed. Run ./build.sh after BepInEx is ready."
fi

echo "-----------------------------------------------"
