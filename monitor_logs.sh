#!/bin/bash

LOG_FILE="/home/tyler/.local/share/Steam/steamapps/common/Megabonk/BepInEx/LogOutput.log"

if [ ! -f "$LOG_FILE" ]; then
    echo "Waiting for log file to be created..."
    while [ ! -f "$LOG_FILE" ]; do sleep 1; done
fi

echo "Monitoring Megabonk Together logs (Ctrl+C to stop)..."
tail -f "$LOG_FILE" | grep --line-buffered -E "MegabonkTogether|Error|Warning|Fatal"
