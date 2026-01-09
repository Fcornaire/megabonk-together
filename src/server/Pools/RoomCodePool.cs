using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace MegabonkTogether.Server.Pools
{
    public static class RoomCodePool
    {
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;

        private static readonly ConcurrentDictionary<string, byte> used = new();
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

        public static string NewCode()
        {
            Span<byte> buffer = stackalloc byte[CodeLength];
            Span<char> code = stackalloc char[CodeLength];

            rng.GetBytes(buffer);
            for (int i = 0; i < CodeLength; i++)
            {
                code[i] = Characters[buffer[i] % Characters.Length];
            }

            string result = new(code);

            while (!used.TryAdd(result, 0))
            {
                rng.GetBytes(buffer);
                for (int i = 0; i < CodeLength; i++)
                {
                    code[i] = Characters[buffer[i] % Characters.Length];
                }
                result = new(code);
            }

            return result;
        }

        public static void Release(string code)
        {
            used.Remove(code, out _);
        }
    }
}
