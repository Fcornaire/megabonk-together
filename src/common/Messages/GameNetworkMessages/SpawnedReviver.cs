using MemoryPack;
using System.Numerics;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class SpawnedReviver : IGameNetworkMessage
    {
        public Vector3 Position { get; set; }
        public uint OwnerConnectionId { get; set; }
        public uint ReviverId { get; set; }
    }
}
