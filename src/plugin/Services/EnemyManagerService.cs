using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Extensions;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Enemies;
using MonoMod.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegabonkTogether.Services
{
    public interface IEnemyManagerService
    {
        public IEnumerable<(uint, uint)> ReTargetEnemies(uint oldTargetId, IEnumerable<uint> currentPlayersExcludingOldOneId);
        public IEnumerable<EnemyModel> GetAllEnemiesDeltaAndUpdate();
        public uint AddSpawnedEnemy(Enemy enemy);
        public void SetSpawnedEnemy(uint enemyId, Enemy enemy);
        public Enemy GetEnemyById(uint id);
        public KeyValuePair<uint, Enemy> GetEnemyByReference(Enemy enemy);
        public void RemoveEnemyById(uint id);
        public void ResetForNextLevel();
        public void ApplyRetargetedEnemies(IEnumerable<(uint, uint)> enemy_NewTargetids, IEnumerable<(uint, Rigidbody)> playerId_rigidbody);
        public void InitializeSwitcher(TargetSwitcher switcher, EEnemyFlag enemyFlag, EEnemy enemyName);

        public void AddReviverEnemy_Name(Enemy enemy, string netplayName);
        public string GetReviverEnemy_Name(Enemy enemy);
        public void RemoveReviverEnemy_Name(Enemy enemy);
    }
    internal class EnemyManagerService : IEnemyManagerService
    {
        private readonly ConcurrentDictionary<uint, Enemy> spawnedEnemies = [];
        private ConcurrentBag<EnemyModel> previousSpawnedEnemiesDelta = [];
        private readonly ConcurrentDictionary<Enemy, string> reviverEnemies_NetplayNames = [];
        private uint currentEnemyId = 0; //TODO: concurrency?

        private const float POSITION_TRESHOLD = 0.1f;
        private const float YAW_TRESHOLD = 5.0f;
        private const ushort HP_TRESHOLD = 1;

        /// <summary>
        /// Server side, retarget enemies when a player dies (or other use case ? )
        /// </summary>
        public IEnumerable<(uint, uint)> ReTargetEnemies(uint oldTargetId, IEnumerable<uint> currentPlayersAliveExcludingOldOneId)
        {
            var retargetedEnemies = new List<(uint, uint)>();
            var oldTargetEnemies = spawnedEnemies.Values.Where(enemy =>
            {
                var currentTargetid = DynamicData.For(enemy).Get<uint?>("targetId");
                if (currentTargetid.HasValue && currentTargetid.Value == oldTargetId)
                {
                    return true;
                }
                return false;
            });


            foreach (var oldEnemy in oldTargetEnemies)
            {
                var randomIndex = Random.Range(0, currentPlayersAliveExcludingOldOneId.Count());
                var randomNewTargetId = currentPlayersAliveExcludingOldOneId.ElementAt(randomIndex);

                DynamicData.For(oldEnemy).Set("targetId", randomNewTargetId);
                var enemyId = GetEnemyByReference(oldEnemy).Key;

                retargetedEnemies.Add((enemyId, randomNewTargetId));
            }

            return retargetedEnemies;
        }

        public void ApplyRetargetedEnemies(IEnumerable<(uint, uint)> enemy_NewTargetids, IEnumerable<(uint, Rigidbody)> playerId_rigidbody)
        {
            foreach (var (enemyId, newTargetId) in enemy_NewTargetids)
            {
                var enemy = GetEnemyById(enemyId);
                if (enemy != null)
                {
                    var playerRigidbody = playerId_rigidbody.FirstOrDefault(pr => pr.Item1 == newTargetId).Item2;
                    if (playerRigidbody != null)
                    {
                        DynamicData.For(enemy).Set("targetId", newTargetId);
                        enemy.target = playerRigidbody;
                    }
                }
                else
                {
                    Plugin.Log.LogWarning($"Failed to retarget enemy {enemyId} to new target {newTargetId} - enemy not found");
                }
            }
        }

        /// <summary>
        /// This should be called once per server tick
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EnemyModel> GetAllEnemiesDeltaAndUpdate()
        {
            var currentEnemies = spawnedEnemies.Select(kv => kv.Value.ToModel(kv.Key)).ToList();

            if (previousSpawnedEnemiesDelta.Count == 0)
            {
                previousSpawnedEnemiesDelta = [.. currentEnemies];
                return currentEnemies;
            }

            var deltas = new List<EnemyModel>();

            foreach (var current in currentEnemies)
            {
                var previous = previousSpawnedEnemiesDelta.FirstOrDefault(e => e.Id == current.Id);

                if (previous == null || HasDelta(previous, current))
                {
                    deltas.Add(current);
                }
            }

            previousSpawnedEnemiesDelta = [.. currentEnemies];

            return deltas;
        }

        private bool HasDelta(EnemyModel previous, EnemyModel current)
        {
            float positionDelta = Vector3.Distance(
                Quantizer.Dequantize(previous.Position),
                Quantizer.Dequantize(current.Position)
            );

            float yawDelta = Mathf.Abs(
                Quantizer.DequantizeYaw(previous.Yaw)
                - Quantizer.DequantizeYaw(current.Yaw)
            );

            float hpDelta = Mathf.Abs(previous.Hp - current.Hp);

            return positionDelta > POSITION_TRESHOLD ||
                   yawDelta > YAW_TRESHOLD ||
                   hpDelta > HP_TRESHOLD;
        }

        public Enemy GetEnemyById(uint id)
        {
            if (spawnedEnemies.TryGetValue(id, out var enemy))
            {
                return enemy;
            }
            return null;
        }


        /// <summary>
        /// Server side
        /// </summary>
        public uint AddSpawnedEnemy(Enemy enemy)
        {
            currentEnemyId++;
            if (!spawnedEnemies.TryAdd(currentEnemyId, enemy))
            {
                Plugin.Log.LogWarning($"Attempted to add an enemy that already exists. EnemyId: {currentEnemyId}");
                return 0;
            }

            return currentEnemyId;
        }

        /// <summary>
        /// Client side
        /// </summary>
        public void SetSpawnedEnemy(uint enemyId, Enemy enemy)
        {
            if (!spawnedEnemies.TryAdd(enemyId, enemy))
            {
                Plugin.Log.LogWarning($"Attempted to add an enemy that already exists. EnemyId: {enemyId}");
            }
        }

        public KeyValuePair<uint, Enemy> GetEnemyByReference(Enemy enemy)
        {
            return spawnedEnemies.FirstOrDefault(kv => kv.Value == enemy);
        }

        public void RemoveEnemyById(uint id)
        {
            if (!spawnedEnemies.TryRemove(id, out var enemy))
            {
                return;
            }
        }

        public void ResetForNextLevel()
        {
            currentEnemyId = 0;
            //spawnedEnemies.Select(Enemy => Enemy.Value).ToList().ForEach(enemy => GameObject.Destroy(enemy.gameObject));
            spawnedEnemies.Clear();
            previousSpawnedEnemiesDelta.Clear();
        }

        //TODO: the applied values should be stored in GameBalanceService
        public void InitializeSwitcher(TargetSwitcher switcher, EEnemyFlag enemyFlag, EEnemy enemyName)
        {
            switch (enemyName)
            {
                case EEnemy.GhostGrave1:
                case EEnemy.GhostGrave2:
                case EEnemy.GhostGrave3:
                case EEnemy.GhostGrave4:
                    switcher.UpdateSwitchIntervalRange(7f, 12f);
                    switcher.UpdateSwitchMaxDistance(30f);
                    return;
                default:
                    break;
            }

            switch (enemyFlag)
            {
                case EEnemyFlag.FinalBoss:
                    switcher.UpdateSwitchIntervalRange(30f, 50f);
                    switcher.UpdateSwitchMaxDistance(300f);
                    break;
                case EEnemyFlag.StageBoss:
                    switcher.UpdateSwitchIntervalRange(20f, 40f);
                    switcher.UpdateSwitchMaxDistance(300f);
                    break;
                default:
                    switcher.UpdateSwitchIntervalRange(40f, 60f);
                    switcher.UpdateSwitchMaxDistance(50f);
                    break;
            }
        }

        public void AddReviverEnemy_Name(Enemy enemy, string netplayName)
        {
            reviverEnemies_NetplayNames.TryAdd(enemy, netplayName);
        }

        public string GetReviverEnemy_Name(Enemy enemy)
        {
            if (reviverEnemies_NetplayNames.TryGetValue(enemy, out var name))
            {
                return name;
            }
            return null;
        }

        public void RemoveReviverEnemy_Name(Enemy enemy)
        {
            reviverEnemies_NetplayNames.TryRemove(enemy, out _);
        }

    }
}
