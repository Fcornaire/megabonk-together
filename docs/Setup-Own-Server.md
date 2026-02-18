# Self-Hosting a MegabonkTogether Server

This guide explains how to set up and run your own MegabonkTogether matchmaking/relay server and how players connect to it.

## Prerequisites

- **Windows x64** (if using the prebuilt release)
- **or** [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) (if building from source)

### Option A - Download the Prebuilt Server

1. Go to the [latest GitHub release](https://github.com/Fcornaire/megabonk-together/releases/latest).
2. Download the **`MegabonkTogether-Server-win-x64-*.zip`** archive.
3. Extract it to a folder of your choice.
4. Run `MegabonkTogether.Server.exe`.

### Option B - Build from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/Fcornaire/megabonk-together.git
   cd megabonk-together
   ```
2. Build and run the server:
   ```bash
   dotnet run --project src/server/MegabonkTogether.Server.csproj
   ```
   By default it will listen on `http://127.0.0.1:5000`.

## Configuration

### HTTP / WebSocket Port

Set the listening URL via the `ASPNETCORE_URLS` environment variable:

```bash
$env:ASPNETCORE_URLS = "http://0.0.0.0:5000"
```

The default development profile uses `http://127.0.0.1:5000`.

### UDP Relay Port

The UDP relay (rendezvous) server listens on port **5678** by default. Make sure this port is open (UDP) if players need to connect from outside your local network.

## Exposing to the Internet

### With a Reverse Proxy (recommended)

If you expose the server to the internet, it is **strongly recommended** to put it behind a reverse proxy (e.g. **Nginx**) that handles TLS termination. This way players connect using `wss://` (WebSocket Secure).

Players would then set their server URL to something like:

```
wss://megabonk.example.com
```

You can use [acme-companion](https://github.com/nginx-proxy/acme-companion) for lestencrypt certificates + auto renewal , used in tandem with a [nginx proxy](https://github.com/nginx-proxy/nginx-proxy)

### Without a Reverse Proxy

If you are **not** using a reverse proxy (e.g. LAN play or testing), the connection URL must use plain `ws://` instead of `wss://`:

```
ws://<your-ip>:5000
```

> [!WARNING]  
> This is **not recommended** for internet-facing servers as traffic will be unencrypted.

## Connecting Players to Your Server

Each player needs to update their `ServerUrl` setting in their **MegabonkTogether plugin config** (found in BepInEx config at `{Megabonk Game path}/BepInEx/config/MegabonkTogether.cfg`
