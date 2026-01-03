# Building for Thunderstore

This project supports building specifically for Thunderstore, which disables automatic update downloads while keeping update notifications.

## How to Build for Thunderstore

### Windows (PowerShell/Command Prompt)

```powershell
# Set the environment variable
$env:THUNDERSTORE_BUILD="true"

# Build the project
dotnet build src/plugin/MegabonkTogether.Plugin.csproj -c Release
```

### CI/CD (GitHub Actions)

```yaml
- name: Build for Thunderstore
  run: dotnet build src/plugin/MegabonkTogether.Plugin.csproj -c Release
  env:
    THUNDERSTORE_BUILD: true
```

## What Changes in Thunderstore Builds?

Nothing more than just disabling auto update
