using MemoryPack;
using System.Numerics;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class SpawnedObject : IGameNetworkMessage
    {
        public Vector3 Position = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Scale = Vector3.One;
        public string PrefabName = string.Empty;
        public uint Id { get; set; }
        public Specific SpecificData = new();
    }

    [MemoryPackable]
    public partial class Specific
    {
        public int ShadyGuyRarity { get; set; }
    }
}
