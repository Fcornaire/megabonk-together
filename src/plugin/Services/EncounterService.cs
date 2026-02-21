using System.Collections.Concurrent;
using System.Linq;

namespace MegabonkTogether.Services
{
    public interface IEncounterService
    {
        public bool IsClosable();

        public void AddClosedEncounterForPlayer(uint playerId);

        public void ClearClosedEncounters();

        public void Close();
        public void Unclose();
    }

    internal class EncounterService(IPlayerManagerService playerManagerService) : IEncounterService
    {
        private readonly ConcurrentDictionary<uint, byte> closedEncounterPerPlayer = new();
        private bool forceClose = false;

        public void AddClosedEncounterForPlayer(uint playerId)
        {
            closedEncounterPerPlayer.TryAdd(playerId, 0);
        }

        public void ClearClosedEncounters()
        {
            closedEncounterPerPlayer.Clear();
            forceClose = false;
        }

        public bool IsClosable()
        {
            var allPlayerCount = playerManagerService.GetAllPlayers().Count();
            return closedEncounterPerPlayer.Count >= allPlayerCount || forceClose;
        }

        public void Close()
        {
            forceClose = true;
        }

        public void Unclose()
        {
            forceClose = false;
        }
    }
}
