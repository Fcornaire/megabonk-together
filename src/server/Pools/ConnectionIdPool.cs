using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace MegabonkTogether.Server.Pools
{
    public static class ConnectionIdPool
    {
        private static readonly ConcurrentDictionary<uint, byte> used = new();
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

        public static uint NewId()
        {
            Span<byte> buffer = stackalloc byte[4];
            rng.GetBytes(buffer);
            uint id = BitConverter.ToUInt32(buffer);

            while (!used.TryAdd(id, 0))
            {
                rng.GetBytes(buffer);
                id = BitConverter.ToUInt32(buffer);
            }
            return id;
        }

        public static void Release(uint id)
        {
            used.Remove(id, out _);
        }
    }
}
