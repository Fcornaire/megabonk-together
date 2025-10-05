using Assets.Scripts.Actors.Enemies;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Snapshot;

namespace MegabonkTogether.Extensions
{
    public static class EnemyExtensions
    {
        public static EnemyModel ToModel(this Enemy enemy, uint enemyId)
        {
            return new EnemyModel()
            {
                Id = enemyId,
                Position = Quantizer.Quantize(enemy.transform.position),
                Yaw = Quantizer.QuantizeYaw(enemy.transform.eulerAngles.y),
                Hp = enemy.hp
            };
        }

        public static EnemySnapshot ToSnapshot(this EnemyModel enemy, double timestamp)
        {
            return new EnemySnapshot()
            {
                Timestamp = timestamp,
                Position = Quantizer.Dequantize(enemy.Position),
                Rotation = Quantizer.Dequantize(enemy.Yaw),
                Hp = enemy.Hp
            };
        }
    }
}
