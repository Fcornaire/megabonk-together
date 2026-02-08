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
echo "If the game path was correct, the mod files should be in: $GAME_PATH/BepInEx/plugins/MegabonkTogether/"
