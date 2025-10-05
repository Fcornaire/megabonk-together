using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class InteractableCharacterFightEnemySpawned : IGameNetworkMessage
    {
        public uint NetplayId { get; set; }
    }
}
