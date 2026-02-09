using System.Collections.Generic;
using System.Linq;

namespace MegabonkTogether.Services
{
    public interface ILocalizationService
    {
        public string GetCustomLocalizedString(string category, string key);
        public void EnqueueNextLocalizedDescription(IEnumerable<string> descriptionArgs);

        public string GetNextCustomLocalizedDescription(string category, string key);
    }
    public class LocalizationService : ILocalizationService
    {
        public readonly Dictionary<(string, string), string> LocalizationOverrides = new()
        {
            { ("MegabonkTogether", "MatchSuccess"), "Match" },
            { ("MegabonkTogether", "MatchSuccessDesc"), "Joined as {0} a lobby of {1} players successfully!" },
            { ("MegabonkTogether", "PlayerDisconnected"), "Disconnected" },
            { ("MegabonkTogether", "PlayerDisconnected_Description"), "Player {0} disconnected" },
            { ("MegabonkTogether", "AllPlayerDisconnected"), "Disconnected" },
            { ("MegabonkTogether", "AllPlayerDisconnected_Description"), "All players left!" },
            { ("MegabonkTogether", "HostDisconnected"), "Disconnected" },
            { ("MegabonkTogether", "HostDisconnected_Description"), "Host left!" },
            { ("MegabonkTogether", "ClientDisconnected"), "Disconnected" },
            { ("MegabonkTogether", "ClientDisconnected_Description"), "Client disconnected" },
            { ("MegabonkTogether", "FriendliesHostSuccess"), "Friendlies" },
            { ("MegabonkTogether", "FriendliesHostSuccessDesc"), "Hosting a friendly lobby" },
            { ("MegabonkTogether", "FriendliesClientSuccess"), "Friendlies" },
            { ("MegabonkTogether", "FriendliesClientSuccessDesc"), "Joined {0} lobby" },
            { ("MegabonkTogether", "FriendliesClientJoinSuccess"), "Friendlies" },
            { ("MegabonkTogether", "FriendliesClientJoinSuccessDesc"), "{0} joined " }
        };

        private readonly Queue<IEnumerable<string>> NextLocalizedDescription = new();

        public string GetCustomLocalizedString(string category, string key)
        {
            if (LocalizationOverrides.TryGetValue((category, key), out var localizedValue))
            {
                return localizedValue;
            }
            return string.Empty;
        }

        public void EnqueueNextLocalizedDescription(IEnumerable<string> descriptionArgs)
        {
            NextLocalizedDescription.Enqueue(descriptionArgs);
        }

        public string GetNextCustomLocalizedDescription(string category, string key)
        {
            var customString = GetCustomLocalizedString(category, key);
            if (string.IsNullOrEmpty(customString))
            {
                return "";
            }

            if (NextLocalizedDescription.Count > 0)
            {
                var args = NextLocalizedDescription.Dequeue();
                try
                {
                    return string.Format(customString, args.ToArray());
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogError($"Error formatting localized string: {ex.Message}");
                    return customString;
                }
            }

            return customString;
        }
    }
}
