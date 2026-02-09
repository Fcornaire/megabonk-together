# Megabonk Together - Technical Notes

## 1. Project Overview
**Megabonk Together** is a co-op networking mod for a Unity-based game. It employs a Host-Client architecture where one player acts as the authoritative host, and others connect as clients. A dedicated server handles matchmaking and relaying traffic if P2P connections fail.

**Current State (Linux):** The project has pivoted from a native Linux port to supporting the Windows version running under **Proton 9.0+**. This decision was made due to instability with BepInEx 6 IL2CPP on native Linux (specifically `PAL_SEHException` crashes).

## 2. Dependencies & Environment

### Frameworks & Tools
*   **Project Management:** Beads (`bd`)
*   **Mod Loader:** BepInEx 6 (Bleeding Edge) - IL2CPP
*   **Target Runtime:** .NET 6.0

### Key Libraries
| Library | Version | Purpose |
| :--- | :--- | :--- |
| **LiteNetLib** | 1.3.5 | Reliable UDP networking for game state synchronization. |
| **MemoryPack** | 1.21.4 | High-performance zero-allocation binary serialization. |
| **BepInEx.Unity.IL2CPP** | 6.0.0-be.* | Modding API for IL2CPP Unity games. |
| **Microsoft.Extensions.Hosting** | 6.0.1 | Dependency injection and application lifecycle management. |

## 3. Networking Architecture

### 3.1. Transports
The mod uses a dual-transport system:
1.  **WebSocket (TCP/TLS):**
    *   **Purpose:** Matchmaking, Lobby negotiation, and Run Statistics.
    *   **Service:** `WebsocketClientService`
    *   **Endpoints:** `/ws?random` (Quickplay), `/ws?friendlies` (Private Lobbies).
2.  **UDP (LiteNetLib):**
    *   **Purpose:** Real-time game state synchronization (Movement, Enemies, Projectiles).
    *   **Service:** `UdpClientService`
    *   **Port:** Default `27015` (increments if occupied).
    *   **NAT Punchthrough:** Native LiteNetLib implementation.
    *   **Relay Fallback:** If NAT punch fails, traffic is routed through the matchmaking server using `RelayEnvelope` packets.

### 3.2. Packet Structure & Serialization
All network messages implement `IGameNetworkMessage` (UDP) or `IWsMessage` (WS) and are serialized using **MemoryPack**.

*   **LobbyUpdates:** Sent by Host. Contains full snapshots of Enemies and Boss Orbs.
*   **PlayerUpdate:** Bi-directional. Contains Position, Rotation, Stats, and Animation State.
*   **RelayEnvelope:** Wraps a standard payload with `TargetConnectionId` for server-side routing.

### 3.3. Relay Protocol
*   **RELAY_BIND:** Client tells server it is ready to receive relay traffic.
*   **USE_RELAY:** Instruction to switch from P2P to Relay mode.

## 4. Player Synchronization

### 4.1. State Capture (Local)
The local player's state is captured each frame in `UdpClientService.UpdateLocalPlayer()` using extensions in `MyPlayerExtensions.cs`.
*   **Animator State:** Boolean flags (`grounded`, `moving`, `jumping`, `grinding`, `idle`).
*   **Physics:** Position is **Quantized** (compressed) before transmission to save bandwidth.

### 4.2. Remote Representation (`NetPlayer.cs`)
Remote players are instantiated as `NetPlayer` GameObjects.
*   **Visuals:**
    *   Clones the character prefab.
    *   Applies Skins and Hats (`SetHat`, `SetSkin`).
    *   Visualizes weapon effects (e.g., *DragonsBreath*, *Aura*) using `ConstantAttack` prefabs attached to the model.
*   **Movement:** Uses `PlayerInterpolator` to smooth out position updates (snapshot interpolation).
*   **UI:** Custom Nameplates (World Space) and Minimap Icons.

### 4.3. Inventory & Stats
*   **Inventory:** Synced via `PlayerInventory` class.
*   **Items:** `ItemAdded` and `ItemRemoved` events trigger visual effects and stat updates.
*   **Health/Shield:** Synced in `PlayerUpdate` and applied to the local representation.

## 5. Linux/Proton Specifics
*   **Native Linux:** Abandoned due to CoreCLR/GLIBC conflicts on newer distros (Fedora 42).
*   **Proton Path:** The mod is built as a Windows DLL and installed into the game's Proton prefix.
*   **Launch Options:** Requires `WINEDLLOVERRIDES="winhttp=n,b"` for BepInEx to hook correctly.

## 6. Directory Structure Reference
*   `src/common`: Shared Models, Messages (`IGameNetworkMessage`), and Serialization logic.
*   `src/plugin`: Client-side mod logic (BepInEx plugin).
    *   `Services/`: Networking services (`UdpClientService`, `WebsocketClientService`).
    *   `Scripts/NetPlayer/`: Remote player logic (`NetPlayer.cs`).
    *   `Patches/`: Harmony patches to hook into game events.
*   `src/server`: Dedicated matchmaking/relay server (ASP.NET Core).
