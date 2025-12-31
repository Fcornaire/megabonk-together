using MegabonkTogether.Common.Models;
using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class ProjectilesUpdate : IGameNetworkMessage
    {
        public ICollection<Projectile> Projectiles { get; set; } = new List<Projectile>();
    }
}
