#!/bin/bash

# Exit on error
set -e

echo "Building Megabonk Together for Linux..."

# Define the game path (edit this if your game is installed elsewhere)
GAME_PATH="${MEGABONK_PATH:-$HOME/.local/share/Steam/steamapps/common/Megabonk}"

echo "Using game path: $GAME_PATH"

# Create Directory.Build.props if it doesn't exist to override the path
if [ ! -f "Directory.Build.props" ]; then
    echo "Creating Directory.Build.props..."
    cat > Directory.Build.props <<EOF
<Project>
  <PropertyGroup>
    <MegabonkPath>$GAME_PATH</MegabonkPath>
  </PropertyGroup>
</Project>
EOF
fi

# Build the project
dotnet build

echo "Build complete!"
if [ -f "$GAME_PATH/Megabonk.exe" ]; then
    echo "Files deployed to: $GAME_PATH/BepInEx/plugins/MegabonkTogether/"
else
    echo "WARNING: Megabonk.exe not found at $GAME_PATH. Is the path correct? (Proton version required)"
fi
