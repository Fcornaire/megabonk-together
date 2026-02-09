using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class RelayEnvelope
    {
        public uint TargetConnectionId { get; set; }
        public bool HaveTarget { get; set; } = false;

        public ICollection<uint> ToFilters = Array.Empty<uint>();
        public byte[] Payload = Array.Empty<byte>();
    }
}
