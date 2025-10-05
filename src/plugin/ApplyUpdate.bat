@echo off
setlocal enabledelayedexpansion

REM ========================================
REM  MegabonkTogether Auto-Updater
REM  Applies updates after game exits
REM ========================================

set GAME_PID=%1
set PLUGIN_DIR=%2

if "%GAME_PID%"=="" (
    echo ERROR: Missing game process ID
    echo Usage: ApplyUpdate.bat [gameProcessId] [pluginDirectory]
    pause
    exit /b 1
)

if "%PLUGIN_DIR%"=="" (
    echo ERROR: Missing plugin directory
    echo Usage: ApplyUpdate.bat [gameProcessId] [pluginDirectory]
    pause
    exit /b 1
)

cls
echo ========================================
echo   MegabonkTogether Auto-Updater
echo ========================================
echo.

REM Wait for exit
echo Waiting for game process (PID: %GAME_PID%) to exit...
:WAIT_LOOP
tasklist /FI "PID eq %GAME_PID%" 2>NUL | find "%GAME_PID%" >NUL
if !ERRORLEVEL! EQU 0 (
    timeout /t 1 /nobreak >NUL
    goto WAIT_LOOP
)
echo Game process exited successfully.
echo.

echo Waiting a bit for file locks to release...
timeout /t 2 /nobreak >NUL
echo.

set UPDATE_FILE=
for %%F in ("%PLUGIN_DIR%\.update_download_*") do (
    set UPDATE_FILE=%%~fF
)

if "%UPDATE_FILE%"=="" (
    echo ERROR: No update file found
    pause
    exit /b 1
)

echo Found update file: %UPDATE_FILE%
echo.

REM backup
set PLUGIN_DLL=%PLUGIN_DIR%\MegabonkTogether.Plugin.dll
set BACKUP_DLL=%PLUGIN_DLL%.backup

if exist "%PLUGIN_DLL%" (
    echo Creating backup...
    copy /Y "%PLUGIN_DLL%" "%BACKUP_DLL%" >NUL
    if !ERRORLEVEL! NEQ 0 (
        echo ERROR: Failed to create backup
        pause
        exit /b 1
    )
    echo Backup created.
    echo.
)

echo Applying update...
echo.

echo %UPDATE_FILE% | find /i ".zip" >NUL
if !ERRORLEVEL! EQU 0 (
    echo Extracting ZIP archive...
    
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$zipPath = '%UPDATE_FILE%'; $destPath = '%PLUGIN_DIR%'; Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath); try { foreach ($entry in $zip.Entries) { if ($entry.Name -match '\.dll$' -or $entry.Name -match '\.exe$') { $targetPath = Join-Path $destPath $entry.Name; [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $targetPath, $true); Write-Host ('  Extracted: ' + $entry.Name) } } } finally { $zip.Dispose() }"
    
    if !ERRORLEVEL! NEQ 0 (
        echo ERROR: Failed to extract ZIP
        goto RESTORE_BACKUP
    )
)

echo.
echo Cleaning up...
del /F /Q "%UPDATE_FILE%" 2>NUL
if exist "%BACKUP_DLL%" del /F /Q "%BACKUP_DLL%" 2>NUL

echo.
echo ========================================
echo   Update applied successfully!
echo ========================================
echo.
echo You can now restart the game.
echo.
echo This window will close in 5 seconds...
timeout /t 5 /nobreak >NUL
exit /b 0

:RESTORE_BACKUP
echo.
echo Restoring backup...
if exist "%BACKUP_DLL%" (
    copy /Y "%BACKUP_DLL%" "%PLUGIN_DLL%" >NUL
    echo Backup restored.
) else (
    echo WARNING: No backup found to restore
)
echo.
echo Update failed. Press any key to exit...
pause >NUL
exit /b 1
