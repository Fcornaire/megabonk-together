using MemoryPack;
using System.Numerics;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class SpawnedPickup : IGameNetworkMessage
    {
        public uint Id { get; set; }
        public int Pickup { get; set; }
        public Vector3 Position { get; set; }
        public int Value { get; set; }
    }
}
