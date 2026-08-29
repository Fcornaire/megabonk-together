using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace MegabonkTogether.Server.Services
{

    public interface IMetricsService
    {
        void RegisterConnectedClientsProvider(Func<int> provider);
        void RegisterLobbiesProvider(Func<(int shared, int regular)> provider);
        void RegisterMatchmakingQueueProvider(Func<(int shared, int regular)> provider);
        void RegisterRelaySessionsProvider(Func<int> provider);
        void ClientConnected(string? ipAddress);
        void MatchCreated(bool isSharedExperience, int playerCount);
        void RunStarted(int playerCount, string mapName, int stageLevel, List<string> characters);
    }

    public class MetricsService : IMetricsService, IDisposable
    {
        private readonly Meter meter;
        private readonly Counter<int> matchesCreated;
        private Func<int>? connectedClientsProvider;
        private Func<(int shared, int regular)>? lobbiesProvider;
        private Func<(int shared, int regular)>? matchmakingQueueProvider;
        private Func<int>? relaySessionsProvider;
        private readonly ConcurrentDictionary<string, DateTime> dailyUniqueConnections = new(); //No logs !
        private int dailyTotalConnections = 0;
        private int allTimeTotalConnections = 0;
        private int peakUniqueConnections = 0;
        private readonly ConcurrentDictionary<string, int> dailyRunsByMapAndStage = new();
        private readonly ConcurrentDictionary<string, int> dailyCharacterUsage = new();
        private DateTime lastResetDate = DateTime.UtcNow.Date;
        private readonly object resetLock = new();
        private readonly Timer resetTimer;

        public MetricsService()
        {
            meter = new Meter("MegabonkTogether.Server", "1.0.0");

            meter.CreateObservableGauge(
                "megabonk.connected_clients",
                () => connectedClientsProvider?.Invoke() ?? 0,
                description: "Number of currently connected clients");

            meter.CreateObservableGauge(
                "megabonk.daily_unique_clients",
                () => GetDailyUniqueClientsCount(),
                description: "Number of unique clients connected today");

            meter.CreateObservableGauge(
                "megabonk.daily_total_connections",
                () => GetDailyTotalConnections(),
                description: "Total number of connections today (including reconnections)");

            meter.CreateObservableGauge(
                "megabonk.alltime_total_connections",
                () => allTimeTotalConnections,
                description: "Total number of connections all time (including reconnections)");

            meter.CreateObservableGauge(
                "megabonk.peak_unique_connections",
                () => peakUniqueConnections,
                description: "Peak number of unique clients connected in a single day");

            meter.CreateObservableGauge(
                "megabonk.daily_runs_by_map_stage",
                () => GetDailyRunsByMapAndStage(),
                description: "Number of runs started today grouped by map and stage");

            meter.CreateObservableGauge(
                "megabonk.daily_character_usage",
                () => GetDailyCharacterUsage(),
                description: "Number of times each character was picked today");

            meter.CreateObservableGauge(
                "megabonk.active_shared_experience_lobbies",
                () => lobbiesProvider?.Invoke().shared ?? 0,
                description: "Number of active lobbies in shared experience mode");

            meter.CreateObservableGauge(
                "megabonk.active_regular_lobbies",
                () => lobbiesProvider?.Invoke().regular ?? 0,
                description: "Number of active lobbies in regular mode");

            meter.CreateObservableGauge(
                "megabonk.matchmaking_queue",
                GetMatchmakingQueue,
                description: "Players waiting in the random matchmaking queue");

            meter.CreateObservableGauge(
                "megabonk.relay_sessions",
                () => relaySessionsProvider?.Invoke() ?? 0,
                description: "Games currently relayed through the server");

            matchesCreated = meter.CreateCounter<int>(
                "megabonk.matches_created",
                description: "Random matches formed by the matchmaker");

            resetTimer = new Timer(CheckAndResetIfNewDay, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        public void RegisterConnectedClientsProvider(Func<int> provider)
        {
            connectedClientsProvider = provider;
        }

        public void RegisterLobbiesProvider(Func<(int shared, int regular)> provider)
        {
            lobbiesProvider = provider;
        }

        public void RegisterMatchmakingQueueProvider(Func<(int shared, int regular)> provider)
        {
            matchmakingQueueProvider = provider;
        }

        public void RegisterRelaySessionsProvider(Func<int> provider)
        {
            relaySessionsProvider = provider;
        }

        public void MatchCreated(bool isSharedExperience, int playerCount)
        {
            matchesCreated.Add(1,
                new KeyValuePair<string, object?>("mode", isSharedExperience ? "shared" : "regular"),
                new KeyValuePair<string, object?>("players", playerCount));
        }

        private IEnumerable<Measurement<int>> GetMatchmakingQueue()
        {
            var (shared, regular) = matchmakingQueueProvider?.Invoke() ?? (0, 0);

            yield return new Measurement<int>(shared, new KeyValuePair<string, object?>("mode", "shared"));
            yield return new Measurement<int>(regular, new KeyValuePair<string, object?>("mode", "regular"));
        }

        public void ClientConnected(string? ipAddress)
        {
            ResetIfNewDay();

            Interlocked.Increment(ref dailyTotalConnections);
            Interlocked.Increment(ref allTimeTotalConnections);

            if (!string.IsNullOrEmpty(ipAddress))
            {
                dailyUniqueConnections.TryAdd(ipAddress, DateTime.UtcNow);
            }
        }

        public void RunStarted(int playerCount, string mapName, int stageLevel, List<string> characters)
        {
            ResetIfNewDay();

            var key = $"{mapName}_stage_{stageLevel}_players_{playerCount}";
            dailyRunsByMapAndStage.AddOrUpdate(key, 1, (_, count) => count + 1);

            foreach (var character in characters)
            {
                dailyCharacterUsage.AddOrUpdate(character, 1, (_, count) => count + 1);
            }
        }

        private int GetDailyUniqueClientsCount()
        {
            return dailyUniqueConnections.Count;
        }

        private int GetDailyTotalConnections()
        {
            return dailyTotalConnections;
        }

        private IEnumerable<Measurement<int>> GetDailyRunsByMapAndStage()
        {
            foreach (var kvp in dailyRunsByMapAndStage)
            {
                var parts = kvp.Key.Split('_');
                if (parts.Length >= 5)
                {
                    var mapName = parts[0];
                    var stage = parts[2];
                    var players = parts[4];

                    yield return new Measurement<int>(
                        kvp.Value,
                        new KeyValuePair<string, object?>("map", mapName),
                        new KeyValuePair<string, object?>("stage", stage),
                        new KeyValuePair<string, object?>("players", players)
                    );
                }
            }
        }

        private IEnumerable<Measurement<int>> GetDailyCharacterUsage()
        {
            foreach (var kvp in dailyCharacterUsage)
            {
                yield return new Measurement<int>(
                    kvp.Value,
                    new KeyValuePair<string, object?>("character", kvp.Key)
                );
            }
        }

        private void CheckAndResetIfNewDay(object? state)
        {
            ResetIfNewDay();
        }

        private void ResetIfNewDay()
        {
            var today = DateTime.UtcNow.Date;

            if (lastResetDate < today)
            {
                lock (resetLock)
                {
                    if (lastResetDate < today)
                    {
                        var currentUniqueCount = dailyUniqueConnections.Count;
                        if (currentUniqueCount > peakUniqueConnections)
                        {
                            peakUniqueConnections = currentUniqueCount;
                        }

                        dailyUniqueConnections.Clear();
                        dailyTotalConnections = 0;
                        dailyRunsByMapAndStage.Clear();
                        dailyCharacterUsage.Clear();
                        lastResetDate = today;
                    }
                }
            }
        }

        public void Dispose()
        {
            resetTimer?.Dispose();
            meter?.Dispose();
        }
    }
}
