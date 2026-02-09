using BepInEx.Configuration;

namespace MegabonkTogether.Configuration
{
    public static class ModConfig
    {
        private static ConfigFile configFile;

        // DEV_URL is "ws://127.0.0.1:5000"

        public static ConfigEntry<string> PlayerName { get; private set; }
        public static ConfigEntry<bool> CheckForUpdates { get; private set; }
        public static ConfigEntry<string> ServerUrl { get; private set; }
        public static ConfigEntry<uint> RDVServerPort { get; private set; }
        public static ConfigEntry<bool> ShowChangelog { get; private set; }
        public static ConfigEntry<string> PreviousVersion { get; private set; }
        public static ConfigEntry<bool> AllowSavesDuringNetplay { get; private set; }

        public static void Initialize(ConfigFile config)
        {
            configFile = config;

            PlayerName = config.Bind(
                "Player",
                "PlayerName",
                "Player",
                "Your display name shown to other players. Please be respectful!"
            );
            CheckForUpdates = config.Bind(
                "Updates",
                "CheckForUpdates",
                true,
                "Check for updates on startup . Recommend leaving this enabled"
            );
            ServerUrl = config.Bind(
                "Network",
                "ServerUrl",
                "wss://megabonk-together-matchmaking.balatro-vs-matchmaking.eu",
                "The URL of the matchmaking server. Do not change this unless you know what you're doing (e.g. for self-hosting). Use ws://127.0.0.1:5000 on localhost for testing purpose"
            );
            RDVServerPort = config.Bind(
                "Network",
                "RDVServerPort",
                (uint)5678,
                "The port of the relay server. Do not change this unless you know what you're doing"
            );
            ShowChangelog = config.Bind(
                "Updates",
                "ShowChangelog",
                false,
                "Internal flag to show changelog after an update. Do not modify manually."
            );
            PreviousVersion = config.Bind(
                "Updates",
                "PreviousVersion",
                "",
                "Internal flag to store the previous version before an update. Do not modify manually."
            );
            AllowSavesDuringNetplay = config.Bind(
                "Gameplay",
                "AllowSavesDuringNetplay",
                false,
                "Allow game saves during netplay sessions."
            );
        }

        public static void Save()
        {
            configFile?.Save();
        }
    }
}
