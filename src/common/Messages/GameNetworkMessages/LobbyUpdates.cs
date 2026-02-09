using MegabonkTogether.Common.Models;
using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class LobbyUpdates : IGameNetworkMessage
    {
        public IEnumerable<Player> Players = new List<Player>();
        public IEnumerable<EnemyModel> Enemies = new List<EnemyModel>();
        public IEnumerable<BossOrbModel> BossOrbs = new List<BossOrbModel>();
    }
}
