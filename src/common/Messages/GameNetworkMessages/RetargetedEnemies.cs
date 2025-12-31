using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class RetargetedEnemies : IGameNetworkMessage
    {
        public IEnumerable<(uint, uint)> Enemy_NewTargetids { get; set; } = Enumerable.Empty<(uint, uint)>();
    }
}
