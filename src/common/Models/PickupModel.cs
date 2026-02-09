using MemoryPack;
using System.Numerics;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class PickupModel
    {
        public uint Id { get; set; }
        public Vector3 Position = Vector3.Zero;
        //public Vector3 Rotation = Vector3.Zero;
    }
}
