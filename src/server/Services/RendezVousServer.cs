using LiteNetLib;
using LiteNetLib.Utils;
using MegabonkTogether.Common.Messages;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;

namespace MegabonkTogether.Server.Services
{
    public class HostInfo
    {
        public IPEndPoint Local;
        public IPEndPoint Remote;
        public DateTime RegisteredAt = DateTime.UtcNow;
    }

    public class PendingClient
    {
        public string ClientId;
        public IPEndPoint Local;
        public IPEndPoint Remote;
        public DateTime RegisteredAt = DateTime.UtcNow;
    }

    public class ProcessedPair
    {
        public DateTime ProcessedAt = DateTime.UtcNow;
    }

    public class RelayPeer
    {
        public uint ConnectionId;
        public NetPeer NetPeer;
        public RelaySession Session;
        public bool IsHost;
        public IPEndPoint EndPoint;
    }

    public class RelaySession
    {
        public uint HostConnectionId;

        public RelayPeer Host;
        public ConcurrentDictionary<uint, RelayPeer> Clients = new();
        public ConcurrentQueue<PendingRelayMessage> PendingToHost = new();
    }

    public class PendingRelayMessage
    {
        public byte[] Payload;
        public DeliveryMethod DeliveryMethod;
        public DateTime EnqueuedAt;
    }

    public interface IRendezVousServer
    {
        public void CleanRelaySession(uint connectionId);
    }

    public class RendezVousServer : IRendezVousServer
    {
        private NetManager udpServer;
        private EventBasedNetListener listener;
        private readonly ILogger<RendezVousServer> logger;

        private readonly ConcurrentDictionary<string, HostInfo> registeredHosts = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<PendingClient>> pendingClients = new();
        private readonly ConcurrentDictionary<string, ProcessedPair> processedPairs = new();

        private readonly ConcurrentDictionary<uint, RelaySession> sessions = new();
        private readonly ConcurrentDictionary<NetPeer, RelayPeer> peerLookup = new();

        private bool hasStarted = false;

        private const int RENDEZVOUS_PORT = 5678;
        private const int POOLING_INTERVAL_MS = 5;

        private static readonly TimeSpan HOST_RETENTION = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan PENDING_CLIENT_RETENTION = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan PROCESSED_PAIR_RETENTION = TimeSpan.FromSeconds(45);

        public RendezVousServer(ILogger<RendezVousServer> logger)
        {
            this.logger = logger;
            InitializeUDPRendezvous();

            Task.Run(async () =>
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    if (hasStarted)
                    {
                        Update();
                    }
                    await Task.Delay(POOLING_INTERVAL_MS, token);
                }
            });

            Task.Run(async () =>
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                    CleanupStaleEntries();
                }
            });
        }

        private void InitializeUDPRendezvous()
        {
            listener = new EventBasedNetListener();
            udpServer = new NetManager(listener)
            {
                UpdateTime = POOLING_INTERVAL_MS,
                IPv6Enabled = true,
                UnconnectedMessagesEnabled = true,
                NatPunchEnabled = true,
                UseNativeSockets = true
            };

            var natListener = new EventBasedNatPunchListener();

            natListener.NatIntroductionRequest += (localEndpoint, remoteEndpoint, token) =>
            {
                try
                {
                    logger.LogInformation($"NAT introduction request from {remoteEndpoint}, token: {token}");

                    string[] parts = token.Split('|');
                    if (parts.Length < 3)
                    {
                        logger.LogWarning($"Invalid token format: {token}");
                        return;
                    }

                    string role = parts[0];
                    string hostId = parts[1];
                    string peerId = parts[2];
                    bool forceRelay = parts.Length > 3 && parts[3] == "force_relay";

                    if (forceRelay)
                    {
                        logger.LogInformation($"Force relay mode requested for {role} {peerId}");
                    }

                    if (role == "host")
                    {
                        HandleHostRegistration(hostId, localEndpoint, remoteEndpoint, forceRelay);
                    }
                    else
                    {
                        HandleClientRegistration(hostId, peerId, localEndpoint, remoteEndpoint, forceRelay);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing NAT introduction request: {token}");
                }
            };

            natListener.NatIntroductionSuccess += (target, natType, token) =>
            {
                // Ignore on server side
            };

            listener.PeerConnectedEvent += peer =>
            {
                try
                {
                    var ipEndPoint = IPEndPoint.Parse($"{peer.Address}:{peer.Port}");
                    var isHostRelay = sessions.Values.Any(session => session.Host.EndPoint.Equals(ipEndPoint));
                    var isClientRelay = sessions.Values.Any(session => session.Clients.Values.Any(relayInfo => relayInfo.EndPoint.Equals(ipEndPoint)));

                    if (isHostRelay || isClientRelay)
                    {
                        logger.LogInformation($"Peer connected for relay mode: {peer.Address}");
                        if (isHostRelay)
                        {
                            logger.LogInformation($"  -> Identified as HOST relay");
                            var hostRelayPeer = sessions.Values.First(session => session.Host.EndPoint.Equals(ipEndPoint)).Host;
                            hostRelayPeer.NetPeer = peer;

                            peerLookup[peer] = hostRelayPeer;

                            var session = hostRelayPeer.Session;
                            while (session.PendingToHost.TryDequeue(out var pending))
                            {
                                logger.LogInformation($"  -> Sending pending message to HOST relay queued at {pending.EnqueuedAt} !!!");
                                peer.Send(pending.Payload, pending.DeliveryMethod);
                            }
                        }
                        else
                        {
                            logger.LogInformation($"  -> Identified as CLIENT relay");
                            var clientRelayPeer = sessions.Values
                                .SelectMany(session => session.Clients.Values)
                                .First(relayInfo => relayInfo.EndPoint.Equals(ipEndPoint));
                            clientRelayPeer.NetPeer = peer;

                            peerLookup[peer] = clientRelayPeer;
                        }
                    }
                    else
                    {
                        logger.LogWarning($"Unexpected peer connected: {peer.Address}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error handling peer connection: {peer.Address}");
                }
            };

            listener.ConnectionRequestEvent += request =>
            {
                try
                {
                    logger.LogInformation($"Connection request from {request.RemoteEndPoint}, accepting");
                    request.Accept();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error handling connection request from {request.RemoteEndPoint}");
                }
            };


            listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
            {
                try
                {
                    if (!peerLookup.TryGetValue(peer, out var relayPeer)) return;

                    var session = relayPeer.Session;
                    var data = reader.GetRemainingBytes();

                    var envelope = MemoryPackSerializer.Deserialize<RelayEnvelope>(data);
                    if (envelope == null)
                    {
                        logger.LogWarning($"Failed to deserialize RelayEnvelope from host {peer.Address}");

                        return;
                    }

                    if (!relayPeer.IsHost)
                    {
                        var netPeer = session.Host.NetPeer;
                        if (netPeer == null)
                        {
                            logger.LogWarning($"Host NetPeer is null for session of client {peer.Address}, enqueuing for latter");

                            session.PendingToHost.Enqueue(new PendingRelayMessage
                            {
                                Payload = envelope.Payload,
                                DeliveryMethod = deliveryMethod,
                                EnqueuedAt = DateTime.UtcNow
                            });
                            return;
                        }
                        session.Host.NetPeer.Send(envelope.Payload, deliveryMethod);
                        return;
                    }

                    if (envelope.HaveTarget)
                    {
                        if (session.Clients.TryGetValue(envelope.TargetConnectionId, out var target))
                        {
                            var netPeer = target.NetPeer;
                            if (netPeer == null)
                            {
                                logger.LogWarning($"Client NetPeer is null for target {envelope.TargetConnectionId} from host {peer.Address}");
                                return;
                            }
                            target.NetPeer.Send(envelope.Payload, deliveryMethod);
                        }

                        return;
                    }

                    foreach (var client in session.Clients.Values)
                    {
                        if (envelope.ToFilters.Contains(client.ConnectionId)) continue;

                        var netPeer = client.NetPeer;
                        if (netPeer == null)
                        {
                            logger.LogWarning($"Client NetPeer is null for client {client.ConnectionId} from host {peer.Address}");
                            continue;
                        }

                        client.NetPeer.Send(envelope.Payload, deliveryMethod);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error handling network receive from {peer.Address}");
                }
            };

            udpServer.NatPunchModule.Init(natListener);

            hasStarted = udpServer.Start(RENDEZVOUS_PORT);

            if (!hasStarted)
            {
                logger.LogError($"Failed to start UDP Rendezvous server on port {RENDEZVOUS_PORT}");
            }
            else
            {
                logger.LogInformation($"UDP Rendezvous server started on port {RENDEZVOUS_PORT}");
            }
        }

        private void HandleHostRegistration(string hostId, IPEndPoint localEndpoint, IPEndPoint remoteEndpoint, bool forceRelay = false)
        {
            registeredHosts[hostId] = new HostInfo
            {
                Local = localEndpoint,
                Remote = remoteEndpoint
            };

            logger.LogInformation($"HOST registered: {hostId} | Local: {localEndpoint} | External: {remoteEndpoint}{(forceRelay ? " | Force Relay: true" : "")}");

            if (pendingClients.TryGetValue(hostId, out var waitingClients))
            {
                logger.LogInformation($"Found {waitingClients.Count} pending clients for host {hostId}, connecting them now");

                var clientsList = waitingClients.ToList();

                foreach (var client in clientsList)
                {
                    ConnectHostAndClient(hostId, client.ClientId, registeredHosts[hostId], client.Local, client.Remote, forceRelay);
                }

                pendingClients.TryRemove(hostId, out _);
            }
            else
            {
                logger.LogInformation($"No pending clients for host {hostId} yet");
            }
        }

        private void HandleClientRegistration(string hostId, string clientId, IPEndPoint localEndpoint, IPEndPoint remoteEndpoint, bool forceRelay = false)
        {
            if (registeredHosts.TryGetValue(hostId, out var host))
            {
                logger.LogInformation($"{hostId} already registered, connecting client {clientId} immediately");
                ConnectHostAndClient(hostId, clientId, host, localEndpoint, remoteEndpoint, forceRelay);
            }
            else
            {
                logger.LogInformation($"HOST {hostId} not found yet, queueing client {clientId}");

                var pendingClient = new PendingClient
                {
                    ClientId = clientId,
                    Local = localEndpoint,
                    Remote = remoteEndpoint
                };

                var clientQueue = pendingClients.GetOrAdd(hostId, _ => new ConcurrentBag<PendingClient>());
                clientQueue.Add(pendingClient);
            }
        }

        private void ConnectHostAndClient(string hostId, string clientId, HostInfo host, IPEndPoint clientLocal, IPEndPoint clientRemote, bool forceRelay = false)
        {
            var pairKey = $"{hostId}_{clientId}";

            if (forceRelay)
            {
                pairKey += "_forceRelay";
            }

            if (!processedPairs.TryAdd(pairKey, new ProcessedPair()))
            {
                logger.LogWarning($"Pair {pairKey} already processed, ignoring duplicate");
                return;
            }

            try
            {
                IPEndPoint hostInternal = host.Local;
                IPEndPoint hostExternal = host.Remote;
                IPEndPoint clientInternal = clientLocal;
                IPEndPoint clientExternal = clientRemote;

                if (forceRelay ||
                    hostExternal.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ||
                    clientExternal.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ||
                    hostInternal.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ||
                    clientInternal.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    UseRelayMode(hostId, clientId, hostExternal, clientExternal);
                    return;
                }

                logger.LogInformation($"  -> Introducing CLIENT {clientId} ({clientExternal}/{clientInternal}) TO HOST {hostId}");
                udpServer.NatPunchModule.NatIntroduce(
                    clientInternal, clientExternal,
                    hostInternal, hostExternal,
                    pairKey
                );

                logger.LogInformation($"  -> Introducing HOST {hostId} ({hostExternal}/{hostInternal}) TO CLIENT {clientId}");
                udpServer.NatPunchModule.NatIntroduce(
                    hostInternal, hostExternal,
                    clientInternal, clientExternal,
                    pairKey
                );

                logger.LogInformation($"Successfully sent bidirectional NAT introduction for pair {pairKey}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to send NAT introduction for pair {pairKey}");
                processedPairs.TryRemove(pairKey, out _);
            }
        }

        private void UseRelayMode(string hostId, string clientId, IPEndPoint hostExternal, IPEndPoint clientExternal)
        {
            logger.LogInformation($"  -> Using RELAY mode to connect CLIENT {clientId} ({clientExternal}) TO HOST {hostId} ({hostExternal})");
            var pairKey = $"{hostExternal}_{clientExternal}";

            var writerExternal = new NetDataWriter();
            writerExternal.Put($"USE_RELAY|{clientId}|{clientExternal}");

            var writer = new NetDataWriter();
            writer.Put($"USE_RELAY|{hostId}|{hostExternal}");

            uint hostConnectionId = uint.Parse(hostId);
            uint clientConnectionId = uint.Parse(clientId);

            if (sessions.TryGetValue(hostConnectionId, out var session))
            {
                session.Clients[clientConnectionId] = new RelayPeer
                {
                    ConnectionId = clientConnectionId,
                    NetPeer = null,
                    IsHost = false,
                    EndPoint = clientExternal,
                    Session = session
                };
            }
            else
            {
                session = new RelaySession
                {
                    HostConnectionId = hostConnectionId,
                };

                var hostPeer = new RelayPeer
                {
                    ConnectionId = hostConnectionId,
                    NetPeer = null,
                    IsHost = true,
                    EndPoint = hostExternal,
                    Session = session
                };
                session.Host = hostPeer;

                session.Clients[clientConnectionId] = new RelayPeer
                {
                    ConnectionId = clientConnectionId,
                    NetPeer = null,
                    IsHost = false,
                    EndPoint = clientExternal,
                    Session = session
                };
                sessions[hostConnectionId] = session;
            }

            udpServer.SendUnconnectedMessage(writer, clientExternal);
            udpServer.SendUnconnectedMessage(writerExternal, hostExternal);
        }

        private void CleanupStaleEntries()
        {
            var now = DateTime.UtcNow;
            int hostsRemoved = 0;
            int pendingClientsRemoved = 0;
            int pairsRemoved = 0;

            foreach (var kvp in registeredHosts)
            {
                if (now - kvp.Value.RegisteredAt > HOST_RETENTION)
                {
                    if (registeredHosts.TryRemove(kvp.Key, out _))
                    {
                        hostsRemoved++;
                        logger.LogInformation($"Removed stale host {kvp.Key}");
                    }
                }
            }

            foreach (var kvp in pendingClients)
            {
                var clientBag = kvp.Value;
                var validClients = new ConcurrentBag<PendingClient>();
                int removedFromBag = 0;

                foreach (var client in clientBag)
                {
                    if (now - client.RegisteredAt <= PENDING_CLIENT_RETENTION)
                    {
                        validClients.Add(client);
                    }
                    else
                    {
                        removedFromBag++;
                    }
                }

                if (removedFromBag > 0)
                {
                    if (validClients.IsEmpty)
                    {
                        if (pendingClients.TryRemove(kvp.Key, out _))
                        {
                            pendingClientsRemoved += removedFromBag;
                            logger.LogInformation($"Removed all {removedFromBag} stale pending clients for host {kvp.Key}");
                        }
                    }
                    else
                    {
                        pendingClients[kvp.Key] = validClients;
                        pendingClientsRemoved += removedFromBag;
                        logger.LogInformation($"Removed {removedFromBag} stale pending clients for host {kvp.Key}, {validClients.Count} remain");
                    }
                }
            }

            foreach (var kvp in processedPairs)
            {
                if (now - kvp.Value.ProcessedAt > PROCESSED_PAIR_RETENTION)
                {
                    if (processedPairs.TryRemove(kvp.Key, out _))
                    {
                        pairsRemoved++;
                    }
                }
            }

            if (hostsRemoved > 0 || pendingClientsRemoved > 0 || pairsRemoved > 0)
            {
                logger.LogInformation($"RendezVous Server Cleanup Report at {now}:");
                logger.LogInformation($"    Cleanup complete: {hostsRemoved} hosts, {pendingClientsRemoved} pending clients, {pairsRemoved} processed pairs removed");
                logger.LogInformation($"    Current state: {registeredHosts.Count} hosts, {pendingClients.Count} pending queues, {processedPairs.Count} processed pairs");
            }
        }

        private void Update()
        {
            udpServer?.PollEvents();
            udpServer?.NatPunchModule.PollEvents();
        }

        public void CleanRelaySession(uint connectionId)
        {
            if (sessions.TryRemove(connectionId, out var session))
            {
                logger.LogInformation($"Cleaning relay session for host {connectionId}");

                session.PendingToHost.Clear();

                foreach (var client in session.Clients.Values)
                {
                    DisconnectRelayPeer(client);
                }

                DisconnectRelayPeer(session.Host);
                return;
            }

            foreach (var sess in sessions.Values)
            {
                if (sess.Clients.TryRemove(connectionId, out var clientPeer))
                {
                    logger.LogInformation($"Cleaning relay client {connectionId} from host {sess.HostConnectionId}");
                    DisconnectRelayPeer(clientPeer);

                    if (sess.Clients.IsEmpty)
                    {
                        logger.LogInformation($"No more clients for host {sess.HostConnectionId}, cleaning up host relay peer");
                        DisconnectRelayPeer(sess.Host);
                        sessions.TryRemove(sess.HostConnectionId, out _);
                    }
                    else
                    {
                        var disconnectedPlayer = new PlayerDisconnected
                        {
                            ConnectionId = connectionId
                        };

                        NetDataWriter writer = new();
                        var msgBytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(disconnectedPlayer);
                        writer.Put(msgBytes);

                        sess.Host.NetPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                    break;
                }
            }
        }

        private void DisconnectRelayPeer(RelayPeer peer)
        {
            if (peer?.NetPeer == null) return;

            peerLookup.TryRemove(peer.NetPeer, out _);
            peer.NetPeer.Disconnect();
            peer.NetPeer = null;
        }
    }
}
