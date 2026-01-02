using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using BepInEx.Logging;
using LiteNetLib;
using LiteNetLib.Utils;
using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.GameNetworkMessages;
using MegabonkTogether.Extensions;
using MegabonkTogether.Helpers;
using MemoryPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MegabonkTogether.Services
{
    internal class PeerIntroduction
    {
        public string Name { get; internal set; }
        public uint ConnectionId { get; internal set; }
        public bool IsHost { get; internal set; }
        public bool HasSelected { get; set; }

        public PeerIntroduction(string name, uint connectionId, bool isHost, bool hasSelected = false)
        {
            Name = name;
            ConnectionId = connectionId;
            IsHost = isHost;
            HasSelected = hasSelected;
        }
    }

    public interface IUdpClientService
    {
        public bool Initialize();
        public void Update();

        public void Poll();

        public Task<bool> HandleMatch(MatchInfo matchInfo, uint selfConnectionId, string rdvServerHost, uint rdvServerPort);
        public bool HasAllPeersConnected();

        public void SendToAllClients<T>(T data, DeliveryMethod deliveryMethod) where T : IGameNetworkMessage;
        public void SendToAllClients(byte[] data, DeliveryMethod deliveryMethod);

        public void SendToHost<T>(T data) where T : IGameNetworkMessage;
        public void SendToClient<T>(NetPeer client, T data, uint netPlayerId) where T : IGameNetworkMessage;
        public void SendToAllClientsExcept<T>(int netPlayerId, uint sender, T data) where T : IGameNetworkMessage;
        public bool? IsHost();
        public void UpdateEnemies();
        public void UpdateProjectiles();
        public void UpdateTumbleWeeds();

        public void Reset();
        public void GameOver();

        public int GetNetPeerCount();
        public bool AreAllPeersReady();
    }
    internal class UdpClientService : IUdpClientService
    {
        private const int MAX_PACKET_SIZE_BYTES = 1000;
        private const int STARTING_GAME_UDP_PORT = 27015;
        private int GAME_UDP_PORT = STARTING_GAME_UDP_PORT;
        private NetManager netManager;
        private EventBasedNetListener listener;
        private EventBasedNatPunchListener natListener;
        private TaskCompletionSource<bool> natPunchComplete;
        private readonly ConcurrentDictionary<int, NetPeer> gamePeers = [];
        private uint? selfConnectionId;
        private readonly ConcurrentDictionary<int, PeerIntroduction> gamePeersIntroduced = [];
        private readonly ConcurrentDictionary<uint, PeerIntroduction> gamePeersIntroducedByRelay = [];
        private bool? isHost { get; set; } = null;
        private int expectedPeerCount = 0;
        private bool hasStarted = false;
        private bool hasAllPeersConnected = false;
        private bool isGameOver = false;
        private readonly IPlayerManagerService playerManagerService;
        private readonly IEnemyManagerService enemyManagerService;
        private readonly IProjectileManagerService projectileManagerService;
        private readonly IFinalBossOrbManagerService finalBossOrbManagerService;
        private readonly ISpawnedObjectManagerService spawnedObjectManagerService;
        private readonly ManualLogSource logger;

        private string rdvServerHost;
        private int rdvServerPort;
        private readonly HashSet<uint> usesRelay = [];
        private NetPeer relayPeer = null;
        private readonly object relayPeerLock = new();

        private ConcurrentDictionary<string, bool> tokens = new();
        private bool hasTriedForceRelay = false;

        private const int POLL_INTERVAL_MS = 5;

        public UdpClientService(
            IPlayerManagerService playerManagerService,
            IEnemyManagerService enemyManagerService,
            IProjectileManagerService projectileManagerService,
            IFinalBossOrbManagerService finalBossOrbManagerService,
            ISpawnedObjectManagerService spawnedObjectManagerService,
            ManualLogSource logger)
        {
            this.playerManagerService = playerManagerService;
            this.enemyManagerService = enemyManagerService;
            this.projectileManagerService = projectileManagerService;
            this.finalBossOrbManagerService = finalBossOrbManagerService;
            this.spawnedObjectManagerService = spawnedObjectManagerService;
            this.logger = logger;
        }

        public bool Initialize()
        {
            if (hasStarted)
            {
                return true;
            }

            listener = new EventBasedNetListener();
            natListener = new EventBasedNatPunchListener();
            netManager = new NetManager(listener)
            {
                IPv6Enabled = true,
                UnconnectedMessagesEnabled = true,
                NatPunchEnabled = true,
                EnableStatistics = true,
                DisconnectTimeout = 15000,
                UpdateTime = POLL_INTERVAL_MS
            };

            bool portInUse = true;
            while (portInUse)
            {
                try
                {
                    hasStarted = netManager.Start(GAME_UDP_PORT);
                    if (hasStarted)
                    {
                        portInUse = false;
                    }
                    else
                    {
                        GAME_UDP_PORT++;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Port {GAME_UDP_PORT} in use, trying next port. Exception: {ex.Message}");
                    GAME_UDP_PORT++;
                }
            }

            if (!hasStarted)
            {
                Plugin.Log.LogError("Failed to start NetManager");
                return false;
            }

            Plugin.Log.LogInfo($"UDPClient listening on port {GAME_UDP_PORT}");

            netManager.NatPunchModule.Init(natListener);

            natListener.NatIntroductionRequest += (local, remote, token) =>
            {
                // ignore on client side
            };

            natListener.NatIntroductionSuccess += (target, natType, token) =>
            {
                try
                {
                    if (!tokens.TryAdd(token, true)) // Atomique!
                    {
                        Plugin.Log.LogWarning($"Duplicate NAT introduction success with token={token}, ignoring.");
                        return;
                    }

                    Plugin.Log.LogInfo($"NAT introduction success, natType={natType}, token={token}");

                    if (netManager != null && netManager.IsRunning)
                    {
                        Plugin.Log.LogInfo($"Connecting...");
                        netManager.Connect(target, "yourKey"); //TODO: technically we should use a key but do we really care ? will check later
                    }
                    else
                    {
                        Plugin.Log.LogError("NetManager is not running, cannot connect");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Error in NAT introduction success handler: {ex}");
                }
            };

            listener.ConnectionRequestEvent += request =>
            {
                Plugin.Log.LogInfo($"Got a connection request from remote"); //TODO: technically we should validate the request key here
                request.Accept();
            };

            listener.PeerConnectedEvent += peer =>
            {
                if (peer.Address.ToString() != DnsHelper.ResolveDomainToIp(this.rdvServerHost))
                {
                    gamePeers.TryAdd(peer.Id, peer);
                }
                else
                {
                    logger.LogInfo($"Connected to relay server: {peer.Id}");
                    lock (relayPeerLock)
                    {
                        relayPeer = peer;
                    }
                }

                if (isHost == null)
                {
                    Plugin.Log.LogError("IsHost not set?!");
                    return;
                }

                if (isHost.HasValue && isHost.Value)
                {
                    Plugin.Log.LogInfo($"Host: Client connected ({gamePeers.Count + usesRelay.Count}/{expectedPeerCount})");
                }
                else
                {
                    Plugin.Log.LogInfo($"Client: Connected to host");
                    IGameNetworkMessage introduced = new Introduced
                    {
                        ConnectionId = selfConnectionId.Value,
                        Name = Configuration.ModConfig.PlayerName.Value,
                        IsHost = isHost.HasValue && isHost.Value
                    };

                    SendToHost(introduced);
                }
            };

            listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
            {
                try
                {
                    byte[] data = reader.GetRemainingBytes();
                    var deserializedMsg = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(data);
                    HandleMessage(deserializedMsg, peer.Id);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Error handling network receive: {ex}");
                }
            };

            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Plugin.Log.LogInfo($"Peer disconnected: {info.Reason}");

                if (peer == null)
                {
                    return;
                }

                HandleDisconnectedPeer(peer);
            };

            listener.NetworkErrorEvent += (endPoint, socketError) =>
            {
                Plugin.Log.LogError($"Network error to {endPoint}: {socketError}");
            };

            listener.NetworkLatencyUpdateEvent += (peer, latency) =>
            {
                //Plugin.Log.LogInfo($"Latency to {peer.Address}: {latency}ms");
            };

            listener.NetworkReceiveUnconnectedEvent += (endPoint, reader, messageType) =>
            {
                var msg = reader.GetString();

                (var mode, var remoteConnectionId, var remoteEndpoint) = msg.Split('|') switch
                {
                    [var m, var rcid, var rep] => (m, uint.Parse(rcid), rep),
                    _ => (null, 0u, null)
                };

                if (mode == null)
                {
                    Plugin.Log.LogWarning($"Invalid unconnected message format: {msg}");
                    return;
                }

                if (mode == "USE_RELAY")
                {
                    logger.LogInfo($"Received USE_RELAY instruction, connecting to relay server...");
                    usesRelay.Add(remoteConnectionId);

                    bool alreadyConnected;
                    lock (relayPeerLock)
                    {
                        alreadyConnected = relayPeer != null;
                    }
                    if (alreadyConnected)
                    {
                        logger.LogInfo($"Already connected to relay server.");
                        return;
                    }

                    netManager.Connect(rdvServerHost, rdvServerPort, "RELAY");
                }
            };

            return hasStarted;
        }

        public int GetNetPeerCount()
        {
            return gamePeers.Count;
        }

        private void HandleDisconnectedPeer(NetPeer peer)
        {
            if (usesRelay.Any())
            {
                bool isRelayPeer;
                lock (relayPeerLock)
                {
                    isRelayPeer = relayPeer == peer;
                }
                if (isRelayPeer)
                {
                    Plugin.Log.LogInfo($"Relay peer disconnected.");
                    lock (relayPeerLock)
                    {
                        relayPeer = null;
                    }
                    usesRelay.Clear();

                    var host = gamePeersIntroducedByRelay.FirstOrDefault(p => p.Value.IsHost);
                    gamePeersIntroducedByRelay.Clear();

                    Plugin.StartNotification(
                    ("MegabonkTogether", "ClientDisconnected"),
                    ("MegabonkTogether", "ClientDisconnected_Description"),
                    [],
                    AudioManager.Instance.uiAbort,
                    item: EItem.BobDead);

                    Plugin.GoToMainMenu();
                    return;
                }

            }


            if (!gamePeers.Remove(peer.Id, out _))
            {
                Plugin.Log.LogWarning($"Disconnected peer {peer.Id} not found in gamePeers");
                return;
            }

            if (!gamePeersIntroduced.TryRemove(peer.Id, out var introInfo))
            {
                Plugin.Log.LogWarning($"Disconnected peer {peer.Id} introduction info not found");
                return;
            }


            gamePeers.Remove(peer.Id, out _); // Remove from connected peers

            if (usesRelay.Any())
            {
                if (!isHost.Value && usesRelay.Count == 1)
                {
                    usesRelay.Clear();
                }
                else if (isHost.Value)
                {
                    var info = gamePeersIntroduced.FirstOrDefault(p => p.Value.ConnectionId == peer.Id);
                    if (info.Value != null)
                    {
                        usesRelay.Remove(info.Value.ConnectionId);
                    }
                }
            }


            if (introInfo.IsHost)
            {
                Plugin.StartNotification(
                    ("MegabonkTogether", "HostDisconnected"),
                    ("MegabonkTogether", "HostDisconnected_Description"),
                    [introInfo.Name],
                    AudioManager.Instance.uiAbort,
                    item: EItem.BobDead
                );
                Plugin.GoToMainMenu();
            }
            else
            {
                if (gamePeers.IsEmpty)
                {
                    Plugin.Log.LogInfo($"All players disconnected, returning to main menu. : is host : {isHost.Value}");

                    Plugin.StartNotification(
                        ("MegabonkTogether", "AllPlayerDisconnected"),
                        ("MegabonkTogether", "AllPlayerDisconnected_Description"),
                        [introInfo.Name],
                        AudioManager.Instance.uiAbort,
                        item: EItem.BobDead
                    );
                    Plugin.GoToMainMenu();
                }
                else
                {
                    if (isHost.HasValue && isHost.Value)
                    {
                        IGameNetworkMessage disconnectedPlayer = new PlayerDisconnected
                        {
                            ConnectionId = introInfo.ConnectionId
                        };

                        EventManager.OnPlayerDisconnected(disconnectedPlayer as PlayerDisconnected);
                        SendToAllClients(disconnectedPlayer, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        public bool AreAllPeersReady()
        {
            if (!isHost.HasValue || !isHost.Value)
            {
                return false;
            }

            var areAllRelayReady = true;
            var areGamePeersReady = true;

            if (usesRelay.Any())
            {
                areAllRelayReady = gamePeersIntroducedByRelay.Values.All(p => p.HasSelected);
            }

            areGamePeersReady = gamePeersIntroduced.Values.All(p => p.HasSelected);

            return areAllRelayReady && areGamePeersReady;
        }

        private void HandleMessage(IGameNetworkMessage message, int netPeerId)
        {
            if (!isHost.Value)
            {
                switch (message)
                {
                    case Introduced introduced:
                        if (relayPeer != null && netPeerId == relayPeer.Id)
                        {
                            if (!gamePeersIntroducedByRelay.TryAdd(introduced.ConnectionId, new PeerIntroduction(introduced.Name, introduced.ConnectionId, introduced.IsHost)))
                            {
                                Plugin.Log.LogWarning($"Duplicate introduction from relay for host={netPeerId}, ignoring.");
                            }

                            return;
                        }

                        if (!gamePeersIntroduced.TryAdd(netPeerId, new PeerIntroduction(introduced.Name, introduced.ConnectionId, introduced.IsHost)))
                        {
                            Plugin.Log.LogWarning($"Duplicate introduction from host={netPeerId}, ignoring.");
                            return;
                        }

                        break;
                    case PlayerDisconnected playerDisconnected:
                        if (usesRelay.Any())
                        {
                            var disconnectedPeerByRelay = gamePeersIntroducedByRelay.FirstOrDefault(p => p.Value.ConnectionId == playerDisconnected.ConnectionId);
                            if (disconnectedPeerByRelay.Value != null)
                            {
                                if (disconnectedPeerByRelay.Value.IsHost)
                                {
                                    logger.LogWarning($"Host disconnected via relay.");
                                    HandleDisconnectedPeer(relayPeer);
                                }
                                else
                                {
                                    EventManager.OnPlayerDisconnected(playerDisconnected);
                                }

                                return;
                            }
                        }

                        var disconnectedPeer = gamePeersIntroduced.FirstOrDefault(p => p.Value.ConnectionId == playerDisconnected.ConnectionId);

                        if (disconnectedPeer.Value == null) //Disonnected peer not a host
                        {
                            EventManager.OnPlayerDisconnected(playerDisconnected);
                            return;
                        }

                        //Host disconnected
                        var peer = gamePeers.FirstOrDefault(p => p.Value.Id == disconnectedPeer.Key).Value;
                        HandleDisconnectedPeer(peer);

                        break;
                    case LobbyUpdates lobbyUpdate:
                        OnLobbyUpdate(lobbyUpdate);
                        break;
                    case ProjectilesUpdate projectilesUpdate:
                        EventManager.OnProjectilesUpdate(projectilesUpdate.Projectiles);
                        break;
                    case SpawnedObject spawnedObject:
                        EventManager.OnSpawnedObject(spawnedObject);
                        break;
                    case SpawnedEnemy spawnedEnemy:
                        EventManager.OnSpawnedEnemy(spawnedEnemy);
                        break;
                    case AbstractSpawnedProjectile spawnedProjectile:
                        EventManager.OnSpawnedProjectile(spawnedProjectile);
                        break;
                    case SelectedCharacter selectedCharacter:
                        EventManager.OnSelectedCharacter(selectedCharacter);
                        break;
                    case EnemyDied enemyDied:
                        EventManager.OnEnemyDied(enemyDied);
                        break;
                    case ProjectileDone projectileDone:
                        EventManager.OnProjectileDone(projectileDone);
                        break;
                    case SpawnedPickupOrb spawnedPickup:
                        EventManager.OnSpawnedPickupOrb(spawnedPickup);
                        break;
                    case SpawnedPickup spawnedPickupItem:
                        EventManager.OnSpawnedPickup(spawnedPickupItem);
                        break;
                    case PickupFollowingPlayer pickupFollowingPlayer:
                        EventManager.OnPickupFollowingPlayer(pickupFollowingPlayer);
                        break;
                    case PickupApplied pickupApplied:
                        EventManager.OnPickupApplied(pickupApplied);
                        break;
                    case SpawnedChest spawnedChest:
                        EventManager.OnSpawnedChest(spawnedChest);
                        break;
                    case ChestOpened chestOpened:
                        EventManager.OnChestOpened(chestOpened);
                        break;
                    case WeaponAdded weaponAdded:
                        EventManager.OnWeaponAdded(weaponAdded);
                        break;
                    case InteractableUsed interactableUsed:
                        EventManager.OnInteractableUsed(interactableUsed);
                        break;
                    case StartingChargingShrine startingChargingShrine:
                        EventManager.OnStartingChargingShrine(startingChargingShrine);
                        break;
                    case StoppingChargingShrine stoppingChargingShrine:
                        EventManager.OnStoppingChargingShrine(stoppingChargingShrine);
                        break;
                    case EnemyExploder enemyExploder:
                        EventManager.OnEnemyExploder(enemyExploder);
                        break;
                    case EnemyDamaged enemyDamaged:
                        EventManager.OnEnemyDamaged(enemyDamaged);
                        break;
                    case SpawnedEnemySpecialAttack spawnedEnemySpecialAttack:
                        EventManager.OnSpawnedEnemySpecialAttack(spawnedEnemySpecialAttack);
                        break;
                    case StartingChargingPylon startingChargingPylon:
                        EventManager.OnStartingChargingPylon(startingChargingPylon);
                        break;
                    case StoppingChargingPylon stoppingChargingPylon:
                        EventManager.OnStoppingChargingPylon(stoppingChargingPylon);
                        break;
                    case FinalBossOrbSpawned finalBossOrbSpawned:
                        EventManager.OnFinalBossOrbSpawned(finalBossOrbSpawned);
                        break;
                    case FinalBossOrbDestroyed finalBossOrbDestroyed:
                        EventManager.OnFinalBossOrbDestroyed(finalBossOrbDestroyed);
                        break;
                    case StartedSwarmEvent startedSwarmEvent:
                        EventManager.OnStartedSwarmEvent(startedSwarmEvent);
                        break;
                    case GameOver gameOver:
                        EventManager.OnGameOver(gameOver);
                        break;
                    case RetargetedEnemies retargetedEnemies:
                        EventManager.OnRetargetedEnemies(retargetedEnemies);
                        break;
                    case RunStarted runStarted:
                        EventManager.OnRunStarted(runStarted);
                        break;
                    case TomeAdded tomeAdded:
                        EventManager.OnTomeAdded(tomeAdded);
                        break;
                    case LightningStrike lightningStrike:
                        EventManager.OnLightningStrike(lightningStrike);
                        break;
                    case TornadoesSpawned tornadoesSpawned:
                        EventManager.OnTornadoesSpawned(tornadoesSpawned);
                        break;
                    case StormStarted stormStarted:
                        EventManager.OnStormStarted(stormStarted);
                        break;
                    case StormStopped stormStopped:
                        EventManager.OnStormStopped(stormStopped);
                        break;
                    case TumbleWeedSpawned tumbleWeedSpawned:
                        EventManager.OnTumbleWeedSpawned(tumbleWeedSpawned);
                        break;
                    case TumbleWeedsUpdate tumbleWeedsUpdate:
                        EventManager.OnTumbleWeedsUpdate(tumbleWeedsUpdate.TumbleWeeds);
                        break;
                    case TumbleWeedDespawned tumbleWeedDespawned:
                        EventManager.OnTumbleWeedDespawned(tumbleWeedDespawned);
                        break;
                    case ItemAdded itemAdded:
                        EventManager.OnItemAdded(itemAdded);
                        break;
                    case ItemRemoved itemRemoved:
                        EventManager.OnItemRemoved(itemRemoved);
                        break;
                    case WeaponToggled weaponToggled:
                        EventManager.OnWeaponToggled(weaponToggled);
                        break;
                    case SpawnedObjectInCrypt spawnedObjectInCrypt:
                        EventManager.OnSpawnedObjectInCrypt(spawnedObjectInCrypt);
                        break;
                    case StartingChargingLamp startingChargingLamp:
                        EventManager.OnStartingChargingLamp(startingChargingLamp);
                        break;
                    case StoppingChargingLamp stoppingChargingLamp:
                        EventManager.OnStoppingChargingLamp(stoppingChargingLamp);
                        break;
                    case TimerStarted timerStarted:
                        EventManager.OnTimerStarted(timerStarted);
                        break;
                    case HatChanged hatChanged:
                        EventManager.OnHatChanged(hatChanged);
                        break;
                    default:
                        Plugin.Log.LogWarning($"Unknown message type received. message={message}");
                        break;
                }
            }
            else
            {
                switch (message)
                {
                    case Introduced introduced:
                        if (relayPeer != null && netPeerId == relayPeer.Id)
                        {
                            if (!gamePeersIntroducedByRelay.TryAdd(introduced.ConnectionId, new PeerIntroduction(introduced.Name, introduced.ConnectionId, introduced.IsHost)))
                            {
                                Plugin.Log.LogWarning($"Duplicate introduction from netPlayerId={netPeerId} via relay, ignoring.");
                            }

                            return;
                        }
                        else
                        {
                            if (!gamePeersIntroduced.TryAdd(netPeerId, new PeerIntroduction(introduced.Name, introduced.ConnectionId, introduced.IsHost)))
                            {
                                Plugin.Log.LogWarning($"Duplicate introduction from netPlayerId={netPeerId}, ignoring.");
                                return;
                            }
                        }

                        IGameNetworkMessage introducedResponse = new Introduced
                        {
                            ConnectionId = selfConnectionId.Value,
                            Name = Configuration.ModConfig.PlayerName.Value
                        };

                        var peer = gamePeers.FirstOrDefault(p => p.Value.Id == netPeerId).Value;
                        if (peer != null)
                        {
                            SendToClient(peer, introducedResponse, introduced.ConnectionId);
                        }

                        break;
                    case ClientInGameReady clientInGameReady:
                        var clientReadyId = clientInGameReady.ConnectionId;
                        var player = playerManagerService.GetPlayer(clientReadyId);
                        if (player == null)
                        {
                            Plugin.Log.LogWarning($"Received ClientReady from unknown player with connection ID {clientReadyId}.");
                            return;
                        }
                        player.IsReady = true;
                        playerManagerService.UpdatePlayer(player);

                        Plugin.Log.LogInfo($"Player {clientReadyId} is ready.");

                        break;
                    case PlayerUpdate playerUpdate:
                        var playerUpdateId = playerUpdate.ConnectionId;
                        var playerToUpdate = playerManagerService.GetPlayer(playerUpdateId);
                        if (playerToUpdate == null)
                        {
                            Plugin.Log.LogWarning($"Received PlayerUpdate from unknown player with connection ID {playerUpdateId}.");
                            return;
                        }

                        playerToUpdate.Position = Quantizer.Quantize(playerUpdate.Position.ToUnityVector3());
                        playerToUpdate.MovementState = playerUpdate.MovementState;
                        playerToUpdate.AnimatorState = playerUpdate.AnimatorState;
                        playerToUpdate.ConnectionId = playerUpdate.ConnectionId;
                        if (playerToUpdate.Hp != 0)
                        {
                            playerToUpdate.Hp = playerUpdate.Hp;
                            playerToUpdate.Shield = playerUpdate.Shield;
                        }
                        playerToUpdate.MaxHp = playerUpdate.MaxHp;
                        playerToUpdate.MaxShield = playerUpdate.MaxShield;
                        //playerToUpdate.Xp = playerUpdate.Xp;
                        playerToUpdate.Inventory = playerUpdate.Inventory;
                        playerToUpdate.Name = playerUpdate.Name;

                        playerManagerService.UpdatePlayer(playerToUpdate);

                        EventManager.OnPlayerUpdate(playerUpdate);
                        break;
                    case SelectedCharacter selectedCharacter:

                        if (gamePeersIntroducedByRelay.TryGetValue(selectedCharacter.ConnectionId, out var introInfoByRelay))
                        {
                            introInfoByRelay.HasSelected = true;
                            gamePeersIntroducedByRelay[selectedCharacter.ConnectionId] = introInfoByRelay;
                        }

                        if (gamePeersIntroduced.TryGetValue(netPeerId, out var introInfo))
                        {
                            introInfo.HasSelected = true;
                            gamePeersIntroduced[netPeerId] = introInfo;
                        }

                        var toUpdate = playerManagerService.GetPlayer(selectedCharacter.ConnectionId); //We could technically use EventManager.OnSelectedCharacter but the metrics RunStatistics sent later will miss the update
                        if (toUpdate == null)
                        {
                            logger.LogWarning($"Player not found for ConnectionId: {selectedCharacter.ConnectionId}");
                            return;
                        }

                        toUpdate.Character = selectedCharacter.Character;
                        toUpdate.Skin = selectedCharacter.Skin;
                        playerManagerService.UpdatePlayer(toUpdate);

                        SendToAllClientsExcept(netPeerId, selectedCharacter.ConnectionId, selectedCharacter);

                        if (AreAllPeersReady() && playerManagerService.HasSelectedCharacter() && Plugin.Instance.IS_HOST_READY)
                        {
                            var runConfig = WindowManager.activeWindow.GetComponentInChildren<MapSelectionUi>().runConfig;
                            MapController.StartNewMap(runConfig);
                        }
                        break;
                    case AbstractSpawnedProjectile spawnedProjectile:
                        EventManager.OnSpawnedProjectile(spawnedProjectile);
                        SendToAllClientsExcept(netPeerId, spawnedProjectile.OwnerId, spawnedProjectile);
                        break;
                    case ProjectileDone projectileDone:
                        EventManager.OnProjectileDone(projectileDone);
                        SendToAllClientsExcept(netPeerId, projectileDone.SenderConnectionId, projectileDone);
                        break;
                    case EnemyDied enemyDied:
                        EventManager.OnEnemyDied(enemyDied);
                        SendToAllClientsExcept(netPeerId, enemyDied.DiedByOwnerId, enemyDied);
                        break;
                    case PickupApplied pickupApplied:
                        EventManager.OnPickupApplied(pickupApplied);
                        SendToAllClientsExcept(netPeerId, pickupApplied.OwnerId, pickupApplied);
                        break;
                    case PickupFollowingPlayer pickupFollowingPlayer:
                        EventManager.OnPickupFollowingPlayer(pickupFollowingPlayer);
                        SendToAllClientsExcept(netPeerId, pickupFollowingPlayer.PlayerId, pickupFollowingPlayer);
                        break;
                    case ChestOpened chestOpened:
                        EventManager.OnChestOpened(chestOpened);
                        SendToAllClientsExcept(netPeerId, chestOpened.OwnerId, chestOpened);
                        break;
                    case WeaponAdded weaponAdded:
                        EventManager.OnWeaponAdded(weaponAdded);
                        SendToAllClientsExcept(netPeerId, weaponAdded.OwnerId, weaponAdded);
                        break;
                    case InteractableUsed interactableUsed:
                        EventManager.OnInteractableUsed(interactableUsed);
                        SendToAllClientsExcept(netPeerId, interactableUsed.OwnerId, interactableUsed);
                        break;
                    case StartingChargingShrine startingChargingShrine:
                        EventManager.OnStartingChargingShrine(startingChargingShrine);
                        break;
                    case StoppingChargingShrine stoppingChargingShrine:
                        EventManager.OnStoppingChargingShrine(stoppingChargingShrine);
                        break;
                    case EnemyExploder enemyExploder:
                        EventManager.OnEnemyExploder(enemyExploder);
                        SendToAllClientsExcept(netPeerId, enemyExploder.SenderId, enemyExploder);
                        break;
                    case EnemyDamaged enemyDamaged:
                        EventManager.OnEnemyDamaged(enemyDamaged);
                        SendToAllClientsExcept(netPeerId, enemyDamaged.AttackerId, enemyDamaged);
                        break;
                    //case SpawnedEnemySpecialAttack spawnedEnemySpecialAttack:
                    //    EventManager.OnSpawnedEnemySpecialAttack(spawnedEnemySpecialAttack);
                    //    SendToAllClientsExcept(netPlayerId, spawnedEnemySpecialAttack);
                    //    break;
                    case StartingChargingPylon startingChargingPylon:
                        EventManager.OnStartingChargingPylon(startingChargingPylon);
                        SendToAllClientsExcept(netPeerId, startingChargingPylon.PlayerChargingId, startingChargingPylon);
                        break;
                    case StoppingChargingPylon stoppingChargingPylon:
                        EventManager.OnStoppingChargingPylon(stoppingChargingPylon);
                        SendToAllClientsExcept(netPeerId, stoppingChargingPylon.PlayerChargingId, stoppingChargingPylon);
                        break;
                    //case FinalBossOrbSpawned finalBossOrbSpawned:
                    //    EventManager.OnFinalBossOrbSpawned(finalBossOrbSpawned);
                    //    SendToAllClientsExcept(netPlayerId, finalBossOrbSpawned);
                    //    break;
                    case FinalBossOrbDestroyed finalBossOrbDestroyed:
                        EventManager.OnFinalBossOrbDestroyed(finalBossOrbDestroyed);
                        SendToAllClientsExcept(netPeerId, finalBossOrbDestroyed.SenderId, finalBossOrbDestroyed);
                        break;
                    case PlayerDied playerDied:
                        EventManager.OnPlayerDied(playerDied);
                        break;
                    case TomeAdded tomeAdded:
                        EventManager.OnTomeAdded(tomeAdded);
                        SendToAllClientsExcept(netPeerId, tomeAdded.OwnerId, tomeAdded);
                        break;
                    case InteractableCharacterFightEnemySpawned interactableCharacterFightEnemySpawned:
                        EventManager.OnInteractableCharacterFightEnemySpawned(interactableCharacterFightEnemySpawned);
                        break;
                    case WantToStartFollowingPickup wantToStartFollowingPickup:
                        EventManager.OnWantToStartFollowingPickup(wantToStartFollowingPickup);
                        break;
                    case ItemAdded itemAdded:
                        EventManager.OnItemAdded(itemAdded);
                        SendToAllClientsExcept(netPeerId, itemAdded.OwnerId, itemAdded);
                        break;
                    case ItemRemoved itemRemoved:
                        EventManager.OnItemRemoved(itemRemoved);
                        SendToAllClientsExcept(netPeerId, itemRemoved.OwnerId, itemRemoved);
                        break;
                    case WeaponToggled weaponToggled:
                        EventManager.OnWeaponToggled(weaponToggled);
                        SendToAllClientsExcept(netPeerId, weaponToggled.OwnerId, weaponToggled);
                        break;
                    //case SpawnedObjectInCrypt spawnedObjectInCrypt:
                    //    EventManager.OnSpawnedObjectInCrypt(spawnedObjectInCrypt);
                    //    SendToAllClientsExcept(netPlayerId, spawnedObjectInCrypt);
                    //    break;
                    case StartingChargingLamp startingChargingLamp:
                        EventManager.OnStartingChargingLamp(startingChargingLamp);
                        SendToAllClientsExcept(netPeerId, startingChargingLamp.PlayerChargingId, startingChargingLamp);
                        break;
                    case StoppingChargingLamp stoppingChargingLamp:
                        EventManager.OnStoppingChargingLamp(stoppingChargingLamp);
                        SendToAllClientsExcept(netPeerId, stoppingChargingLamp.PlayerChargingId, stoppingChargingLamp);
                        break;
                    case TimerStarted timerStarted:
                        EventManager.OnTimerStarted(timerStarted);
                        SendToAllClientsExcept(netPeerId, timerStarted.SenderId, timerStarted);
                        break;
                    case HatChanged hatChanged:
                        EventManager.OnHatChanged(hatChanged);
                        SendToAllClientsExcept(netPeerId, hatChanged.OwnerId, hatChanged);
                        break;
                    case PlayerDisconnected playerDisconnected: //Host only receives this message by the rdv server, normally its handled in LiteNet's PeerDisconnectedEvent
                        if (!usesRelay.Remove(playerDisconnected.ConnectionId))
                        {
                            logger.LogInfo($"PlayerDisconnected: ConnectionId {playerDisconnected.ConnectionId} was not using relay.");
                            return;
                        }

                        EventManager.OnPlayerDisconnected(playerDisconnected);
                        SendToAllClients(playerDisconnected, DeliveryMethod.ReliableOrdered);

                        break;
                    default:
                        Plugin.Log.LogWarning($"Unknown message type received {message}");
                        break;
                }
            }
        }

        public void Reset()
        {
            selfConnectionId = null;
            isHost = null;
            expectedPeerCount = 0;
            hasAllPeersConnected = false;
            isGameOver = false;
            gamePeers.Clear();
            netManager?.Stop();
            hasStarted = false;
            gamePeersIntroduced.Clear();
            gamePeersIntroducedByRelay.Clear();
            hasTriedForceRelay = false;

            lock (relayPeerLock)
            {
                relayPeer?.Disconnect();
                relayPeer = null;
            }
            usesRelay.Clear();
        }

        public void GameOver()
        {
            isGameOver = true;
        }

        private void OnLobbyUpdate(LobbyUpdates lobbyUpdate) //TODO: move to synchronizationService
        {

            foreach (var player in lobbyUpdate.Players)
            {
                var existingPlayer = playerManagerService.GetPlayer(player.ConnectionId);
                if (existingPlayer != null)
                {
                    var previousHp = existingPlayer.Hp;
                    if (previousHp == 0)
                    {
                        player.Hp = 0;
                    }
                    playerManagerService.UpdatePlayer(player);

                    var playerUpdate = new PlayerUpdate
                    {
                        Position = Quantizer.Dequantize(player.Position).ToNumericsVector3(),
                        MovementState = player.MovementState,
                        AnimatorState = player.AnimatorState,
                        ConnectionId = player.ConnectionId,
                        Hp = player.Hp,
                        MaxHp = player.MaxHp,
                        Shield = player.Shield,
                        MaxShield = player.MaxShield,
                        //Xp = player.Xp,
                        Name = player.Name,
                        Inventory = player.Inventory,
                    };

                    if (previousHp == 0)
                    {
                        playerUpdate.Hp = 0;
                    }

                    EventManager.OnPlayerUpdate(playerUpdate);
                }
            }

            EventManager.OnEnemiesUpdate(lobbyUpdate.Enemies);
            EventManager.OnFinalBossOrbsUpdate(lobbyUpdate.BossOrbs);
        }

        public async Task<bool> HandleMatch(MatchInfo matchInfo, uint selfConnectionId, string rdvServerHost, uint rdvServerPort)
        {
            this.rdvServerHost = rdvServerHost;
            this.rdvServerPort = (int)rdvServerPort;

            this.selfConnectionId = selfConnectionId;
            isHost = matchInfo.Peers.FirstOrDefault(p => p.ConnectionId == selfConnectionId)?.IsHost ?? false;

            foreach (var peer in matchInfo.Peers)
            {
                playerManagerService.AddPlayer(peer.ConnectionId, peer.IsHost.Value, peer.ConnectionId == selfConnectionId);
            }

            playerManagerService.SetSeed((int)matchInfo.Seed);

            Plugin.Log.LogInfo($"I am {(isHost.Value ? "HOST" : "CLIENT")}");

            if (isHost.Value)
            {
                expectedPeerCount = matchInfo.Peers.Count() - 1; // All clients except myself
                Plugin.Log.LogInfo($"Host expecting {expectedPeerCount} client connections");
            }
            else
            {
                expectedPeerCount = 1; // Only the host
                Plugin.Log.LogInfo("Client expecting connection to host");
            }

            var role = isHost.Value ? "host" : "client";
            var hostId = matchInfo.Peers.First(p => p.IsHost == true).ConnectionId.ToString();

            var uniqueToken = $"{role}|{hostId}|{selfConnectionId}";

            Plugin.Log.LogInfo($"Sending NAT punch request to rendezvous server with token {uniqueToken}");

            netManager.NatPunchModule.SendNatIntroduceRequest(rdvServerHost, (int)rdvServerPort, uniqueToken);

            Plugin.Log.LogInfo("Waiting for NAT introductions and P2P connections...");

            natPunchComplete = new TaskCompletionSource<bool>();

            var pollTask = Task.Run(async () =>
            {
                while (true)
                {
                    bool relayConnected;
                    lock (relayPeerLock)
                    {
                        relayConnected = relayPeer != null;
                    }
                    if (gamePeers.Count + usesRelay.Count >= expectedPeerCount && (!usesRelay.Any() || relayConnected))
                        break;
                    Poll();
                    await Task.Delay(POLL_INTERVAL_MS);
                }
                hasAllPeersConnected = true;
                natPunchComplete.SetResult(true);

            });

            var timeoutTask = Task.Delay(15000);
            var completedTask = await Task.WhenAny(natPunchComplete.Task, timeoutTask);

            if (completedTask == natPunchComplete.Task && await natPunchComplete.Task)
            {
                Plugin.Log.LogInfo($"P2P connections successful! Connected to {gamePeers.Count + usesRelay.Count} peers");
                return true;
            }
            else
            {
                if (!hasTriedForceRelay)
                {
                    Plugin.Log.LogWarning($"P2P connection timeout - only {gamePeers.Count + usesRelay.Count}/{expectedPeerCount} peers connected, retrying with forced relay mode...");
                    hasTriedForceRelay = true;

                    var forceRelayToken = $"{role}|{hostId}|{selfConnectionId}|force_relay";
                    Plugin.Log.LogInfo($"Sending NAT punch request with force relay: {forceRelayToken}");
                    netManager.NatPunchModule.SendNatIntroduceRequest(rdvServerHost, (int)rdvServerPort, forceRelayToken);

                    natPunchComplete = new TaskCompletionSource<bool>();

                    var retryPollTask = Task.Run(async () =>
                    {
                        while (true)
                        {
                            bool relayConnected;
                            lock (relayPeerLock)
                            {
                                relayConnected = relayPeer != null;
                            }
                            if (gamePeers.Count + usesRelay.Count >= expectedPeerCount && (!usesRelay.Any() || relayConnected))
                                break;
                            Poll();
                            await Task.Delay(POLL_INTERVAL_MS);
                        }
                        hasAllPeersConnected = true;
                        natPunchComplete.SetResult(true);
                    });

                    var retryTimeoutTask = Task.Delay(15000);
                    var retryCompletedTask = await Task.WhenAny(natPunchComplete.Task, retryTimeoutTask);

                    if (retryCompletedTask == natPunchComplete.Task && await natPunchComplete.Task)
                    {
                        Plugin.Log.LogInfo($"P2P connections successful with forced relay! Connected to {gamePeers.Count + usesRelay.Count} peers");
                        return true;
                    }
                }

                Plugin.Log.LogError($"P2P connection timeout - only {gamePeers.Count + usesRelay.Count}/{expectedPeerCount} peers connected");
                return gamePeers.Count + usesRelay.Count > 0;
            }
        }

        public void Poll()
        {
            if (hasStarted)
            {
                netManager?.PollEvents();
                netManager?.NatPunchModule.PollEvents();
            }
        }

        public void Update()
        {
            UpdateLocalPlayer();

            if (isGameOver)
            {
                return;
            }

            if (isHost.HasValue && isHost.Value)
            {
                if (playerManagerService.IsGameOver())
                {
                    isGameOver = true;
                    IGameNetworkMessage wsMessage = new GameOver();
                    SendToAllClients(wsMessage, DeliveryMethod.ReliableOrdered);
                    EventManager.OnGameOver(wsMessage as GameOver);

                    return;
                }

                SendLobbyUpdate();
            }
            else
            {
                IGameNetworkMessage playerUpdate = playerManagerService.GetLocalPlayerUpdate();
                if (playerUpdate == null)
                {
                    Plugin.Log.LogWarning("Local player update is null, cannot send to host");
                    return;
                }
                SendToHost(playerUpdate);
            }
        }

        public void UpdateEnemies()
        {
            if (isGameOver)
            {
                return;
            }

            if (!EnsureIsHost())
            {
                return;
            }

            SendEnemiesUpdate();
        }

        public void UpdateProjectiles()
        {
            if (isGameOver)
            {
                return;
            }

            if (!EnsureIsHost())
            {
                return;
            }

            SendProjectilesUpdate();
        }

        public void UpdateTumbleWeeds()
        {
            if (isGameOver)
            {
                return;
            }

            if (!EnsureIsHost())
            {
                return;
            }

            SendTumbleWeedsUpdate();
        }

        private void UpdateLocalPlayer()
        {
            var player = playerManagerService.GetLocalPlayer();
            var localPlayer = playerManagerService.GetLocalPlayerUpdate();

            if (player == null)
            {
                Plugin.Log.LogWarning("Local player is null");
                return;
            }

            if (localPlayer == null)
            {
                Plugin.Log.LogWarning("Local player update is null");
                return;
            }

            player.Position = Quantizer.Quantize(localPlayer.Position.ToUnityVector3());
            player.MovementState = localPlayer.MovementState;
            player.AnimatorState = localPlayer.AnimatorState;
            player.Hp = localPlayer.Hp;
            player.MaxHp = localPlayer.MaxHp;
            player.Shield = localPlayer.Shield;
            player.MaxShield = localPlayer.MaxShield;
            //player.Xp = localPlayer.Xp;
            player.Inventory = localPlayer.Inventory;
            player.Name = localPlayer.Name;

            playerManagerService.UpdatePlayer(player);
        }

        private void SendLobbyUpdate()
        {
            var players = playerManagerService.GetAllPlayers();

            var message = new LobbyUpdates
            {
                Players = players,
            };

            byte[] serialized = MemoryPackSerializer.Serialize<IGameNetworkMessage>(message);

            var deliveryMethod = DeliveryMethod.ReliableSequenced;

            if (serialized.Length >= MAX_PACKET_SIZE_BYTES)
            {
                deliveryMethod = DeliveryMethod.ReliableOrdered;
            }

            SendToAllClients(serialized, deliveryMethod);
        }

        private void SendEnemiesUpdate()
        {
            var enemies = enemyManagerService.GetAllEnemiesDeltaAndUpdate();
            var bossFinalOrb = finalBossOrbManagerService.GetAllOrbs();

            if (!enemies.Any() && !bossFinalOrb.Any())
            {
                return;
            }

            var message = new LobbyUpdates
            {
                Enemies = enemies,
                BossOrbs = bossFinalOrb,
            };

            byte[] serialized = MemoryPackSerializer.Serialize<IGameNetworkMessage>(message);

            var deliveryMethod = DeliveryMethod.ReliableSequenced;

            if (serialized.Length >= MAX_PACKET_SIZE_BYTES)
            {
                deliveryMethod = DeliveryMethod.ReliableOrdered;
            }

            SendToAllClients(serialized, deliveryMethod);
        }

        private void SendProjectilesUpdate()
        {
            var projectiles = projectileManagerService.GetAllProjectilesDeltaAndUpdate();

            if (!projectiles.Any())
            {
                return;
            }

            var message = new ProjectilesUpdate
            {
                Projectiles = [.. projectiles],
            };

            byte[] serialized = MemoryPackSerializer.Serialize<IGameNetworkMessage>(message);

            var deliveryMethod = DeliveryMethod.Unreliable;

            if (serialized.Length >= MAX_PACKET_SIZE_BYTES)
            {
                deliveryMethod = DeliveryMethod.ReliableOrdered;
            }

            SendToAllClients(serialized, deliveryMethod);
        }

        private void SendTumbleWeedsUpdate()
        {
            var tumbleWeeds = spawnedObjectManagerService.GetAllTumbleWeedsDeltaAndUpdate();

            if (!tumbleWeeds.Any())
            {
                return;
            }

            var message = new TumbleWeedsUpdate
            {
                TumbleWeeds = [.. tumbleWeeds],
            };

            byte[] serialized = MemoryPackSerializer.Serialize<IGameNetworkMessage>(message);

            var deliveryMethod = DeliveryMethod.Unreliable;

            if (serialized.Length >= MAX_PACKET_SIZE_BYTES)
            {
                deliveryMethod = DeliveryMethod.ReliableOrdered;
            }

            SendToAllClients(serialized, deliveryMethod);
        }

        public void SendToAllClients<T>(T data, DeliveryMethod deliveryMethod) where T : IGameNetworkMessage
        {
            if (!EnsureIsHost())
            {
                return;
            }

            var msgBytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(data);

            if (usesRelay.Any())
            {
                RelayEnvelope relayEnvelope = new()
                {
                    Payload = msgBytes,
                };
                var relayMsgBytes = MemoryPackSerializer.Serialize(relayEnvelope);
                lock (relayPeerLock)
                {
                    relayPeer?.Send(relayMsgBytes, deliveryMethod);
                }
            }

            if (gamePeers.Count == 0)
            {
                //Plugin.Log.LogWarning("No other clients connected");
                return;
            }

            NetDataWriter writer = new();
            writer.Put(msgBytes);

            foreach (var (_, peer) in gamePeers)
            {
                peer.Send(writer, deliveryMethod);
            }
        }

        public void SendToHost<T>(T data) where T : IGameNetworkMessage
        {
            if (!EnsureIsClient())
            {
                return;
            }

            var msgBytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(data);

            var deliveryMethod = DeliveryMethod.ReliableSequenced;

            if (msgBytes.Length >= MAX_PACKET_SIZE_BYTES)
            {
                deliveryMethod = DeliveryMethod.ReliableOrdered;
            }

            if (usesRelay.Any())
            {
                RelayEnvelope relayEnvelope = new()
                {
                    Payload = msgBytes,
                };
                var relayMsgBytes = MemoryPackSerializer.Serialize(relayEnvelope);
                lock (relayPeerLock)
                {
                    relayPeer?.Send(relayMsgBytes, DeliveryMethod.ReliableOrdered);
                }
                return;
            }

            NetDataWriter writer = new();
            writer.Put(msgBytes);

            if (gamePeers.Count == 0)
            {
                Plugin.Log.LogWarning("Not connected to host");
                return;
            }

            gamePeers[0].Send(writer, deliveryMethod);
        }

        public void SendToClient<T>(NetPeer client, T data, uint connectionId) where T : IGameNetworkMessage
        {
            if (!EnsureIsHost())
            {
                return;
            }

            var msgBytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(data);

            if (usesRelay.Contains(connectionId))
            {
                RelayEnvelope relayEnvelope = new()
                {
                    TargetConnectionId = connectionId,
                    HaveTarget = true,
                    Payload = msgBytes,
                };
                var relayMsgBytes = MemoryPackSerializer.Serialize(relayEnvelope);
                lock (relayPeerLock)
                {
                    relayPeer?.Send(relayMsgBytes, DeliveryMethod.ReliableOrdered);
                }
                return;
            }

            NetDataWriter writer = new NetDataWriter();
            writer.Put(msgBytes);
            client.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private bool EnsureIsHost()
        {
            if (isHost == null)
            {
                Plugin.Log.LogWarning("IsHost not set yet");
                return false;
            }
            if (!isHost.Value)
            {
                Plugin.Log.LogWarning("Only host can perform this action");
                return false;
            }
            return true;
        }

        private bool EnsureIsClient()
        {
            if (isHost == null)
            {
                Plugin.Log.LogWarning("IsHost not set yet");
                return false;
            }
            if (isHost.Value)
            {
                Plugin.Log.LogWarning("Only client can perform this action");
                return false;
            }
            return true;
        }

        public bool? IsHost()
        {
            return isHost;
        }

        public bool HasAllPeersConnected()
        {
            return hasAllPeersConnected;
        }

        public void SendToAllClientsExcept<T>(int netPlayerId, uint sender, T data) where T : IGameNetworkMessage
        {
            if (!EnsureIsHost())
            {
                return;
            }

            var msgBytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(data);

            if (usesRelay.Any())
            {
                var toExcept = gamePeersIntroducedByRelay[sender].ConnectionId;

                RelayEnvelope relayEnvelope = new()
                {
                    ToFilters = [toExcept],
                    Payload = msgBytes,
                };

                var relayMsgBytes = MemoryPackSerializer.Serialize(relayEnvelope);
                lock (relayPeerLock)
                {
                    relayPeer?.Send(relayMsgBytes, DeliveryMethod.ReliableOrdered);
                }
            }

            if (gamePeers.Count == 0)
            {
                //Plugin.Log.LogWarning("No clients connected");
                return;
            }

            NetDataWriter writer = new NetDataWriter();
            writer.Put(msgBytes);

            var filteredPeers = gamePeers.Where(p => p.Value.Id != netPlayerId);
            foreach (var (_, peer) in filteredPeers)
            {
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }

        public void SendToAllClients(byte[] data, DeliveryMethod deliveryMethod)
        {
            if (!EnsureIsHost())
            {
                return;
            }

            if (usesRelay.Any())
            {
                RelayEnvelope relayEnvelope = new()
                {
                    Payload = data,
                };
                var relayMsgBytes = MemoryPackSerializer.Serialize(relayEnvelope);
                lock (relayPeerLock)
                {
                    relayPeer?.Send(relayMsgBytes, deliveryMethod);
                }
            }

            if (gamePeers.Count == 0)
            {
                //Plugin.Log.LogWarning("No clients connected");
                return;
            }

            NetDataWriter writer = new NetDataWriter();
            writer.Put(data);

            try
            {
                foreach (var (_, peer) in gamePeers)
                {
                    peer.Send(writer, deliveryMethod);
                }
            }
            catch (LiteNetLib.TooBigPacketException ex)
            {
                Plugin.Log.LogError($"Failed to send message: {ex.Message}");
            }
        }
    }
}
