using Actors.Enemies;

namespace MegabonkTogether.Extensions
{
    internal static class DataManagerExtensions
    {
        //TODO: remove this extension, there is already one in the base game
        public static EnemyData GetEnemyDataByName(this DataManager dataManager, EEnemy enemyEnum)
        {
            if (!dataManager.enemyData.TryGetValue(enemyEnum, out var enemyData))
            {
                Plugin.Log.LogWarning($"EnemyManager.GetEnemyDataByName: Could not find EnemyData for enemy '{enemyEnum}'.");
                return null;
            }

            return enemyData;
        }

    }
}
