using Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Snapshot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegabonkTogether.Services
{
    public interface ISpawnedObjectManagerService
    {
        public uint AddSpawnedObject(GameObject obj);
        public GameObject GetSpawnedObject(uint value);
        public void SetSpawnedObject(uint id, GameObject spawned);
        public uint? GetByReference(GameObject obj);
        public T GetSpecific<T>() where T : MonoBehaviour;
        public uint? GetByReferenceInChildren<T>(GameObject obj) where T : MonoBehaviour;
        public void ResetForNextLevel();
        public Material GetExtraEnemyMaterial(EEnemy enemyName);
        public IEnumerable<TumbleWeedModel> GetAllTumbleWeedsDeltaAndUpdate();
        public void RegisterTumbleWeedForInterpolation(uint id, GameObject tumbleWeed);
        public void UpdateTumbleWeedSnapshots(List<TumbleWeedSnapshot> snapshots);
        public void UnregisterTumbleWeedFromInterpolation(uint id);
        public void RemoveSpawnedObject(uint id, GameObject obj, bool destroyObject = true);
        public void AddShadyGuyRarityRequest(EItemRarity rarity);
        public EItemRarity? UnqueueShadyGuyRarityRequest();
        public IEnumerable<(uint netplayId, GameObject obj)> GetAllSpawnedObjects();
        public uint SetSpawnedObjectAtLast(GameObject gameObject);
    }

    internal class SpawnedObjectManagerService : ISpawnedObjectManagerService
    {
        private readonly ConcurrentDictionary<uint, GameObject> spawnedObjects = [];
        private readonly ConcurrentDictionary<EEnemy, Material> extraEnemyMaterial = [];
        private readonly ConcurrentDictionary<uint, TumbleWeedModel> previousTumbleWeedsDelta = [];
        private ConcurrentQueue<EItemRarity> shadyGuyRarityRequest = [];
        private TumbleWeedInterpolator tumbleWeedInterpolator;
        private uint currentObjectId = 0; //TODO: concurrency?

        private const float TUMBLEWEED_POSITION_THRESHOLD = 0.1f;

        /// <summary>
        /// Server side
        /// </summary>
        public uint AddSpawnedObject(GameObject obj)
        {
            currentObjectId++;
            if (!spawnedObjects.TryAdd(currentObjectId, obj))
            {
                Plugin.Log.LogWarning($"Attempted to add an object that already exists. ObjectId: {currentObjectId} , stackTrace: {System.Environment.StackTrace}");
                return 0;
            }
            return currentObjectId;
        }

        /// <summary>
        /// Client side
        /// </summary>
        public void SetSpawnedObject(uint id, GameObject spawned)
        {
            if (!spawnedObjects.TryAdd(id, spawned))
            {
                Plugin.Log.LogWarning($"Attempted to add an object that already exists. ObjectId: {id}");
            }

            //Check for InteractableCharacterFight to save material
            var interactableFight = spawned.GetComponentInChildren<InteractableCharacterFight>();
            if (interactableFight != null)
            {
                var material = interactableFight.enemyMat2;
                var enemyType = interactableFight.enemyData.enemyName;
                extraEnemyMaterial.TryAdd(enemyType, material);
            }
        }

        /// <summary>
        /// Client side also
        /// </summary>
        public uint SetSpawnedObjectAtLast(GameObject gameObject)
        {
            var id = spawnedObjects.Keys.Max() + 1;
            spawnedObjects.TryAdd(id, gameObject);
            return id;
        }

        public IEnumerable<(uint netplayId, GameObject obj)> GetAllSpawnedObjects()
        {
            return spawnedObjects.Select(kv => (kv.Key, kv.Value));
        }

        public Material GetExtraEnemyMaterial(EEnemy enemyName)
        {
            if (extraEnemyMaterial.TryGetValue(enemyName, out var mat))
            {
                return mat;
            }
            return null;
        }

        public GameObject GetSpawnedObject(uint value)
        {
            if (spawnedObjects.TryGetValue(value, out var obj))
            {
                return obj;
            }
            return null;
        }

        public uint? GetByReference(GameObject obj)
        {
            foreach (var kv in spawnedObjects)
            {
                if (kv.Value == obj)
                {
                    return kv.Key;
                }
            }
            return null;
        }

        public T GetSpecific<T>() where T : MonoBehaviour
        {
            return spawnedObjects.Values
                    .Where(o => o != null)
                    .Select(o => o.GetComponent<T>())
                    .FirstOrDefault(c => c != null);
        }

        public void ResetForNextLevel()
        {
            currentObjectId = 0;
            previousTumbleWeedsDelta.Clear();
            //spawnedObjects.Values.ToList().ForEach(Object.Destroy);
            spawnedObjects.Clear();
        }

        public uint? GetByReferenceInChildren<T>(GameObject obj) where T : MonoBehaviour
        {
            foreach (var kv in spawnedObjects)
            {
                if (kv.Value == null)
                {
                    continue;
                }

                var childComponent = kv.Value.GetComponentInChildren<T>();
                if (childComponent != null && childComponent.gameObject == obj)
                {
                    return kv.Key;
                }
            }
            return null;
        }

        public IEnumerable<TumbleWeedModel> GetAllTumbleWeedsDeltaAndUpdate()
        {
            var currentTumbleWeeds = new List<TumbleWeedModel>();

            foreach (var kv in spawnedObjects)
            {
                if (kv.Value != null)
                {
                    var interactable = kv.Value.GetComponent<InteractableTumbleWeed>();
                    if (interactable != null)
                    {
                        currentTumbleWeeds.Add(new TumbleWeedModel
                        {
                            NetplayId = kv.Key,
                            Position = Quantizer.Quantize(interactable.transform.position)
                        });
                    }
                }
            }

            if (previousTumbleWeedsDelta.Count == 0)
            {
                foreach (var tumbleWeed in currentTumbleWeeds)
                {
                    previousTumbleWeedsDelta.TryAdd(tumbleWeed.NetplayId, tumbleWeed);
                }
                return currentTumbleWeeds;
            }

            var deltas = new List<TumbleWeedModel>();

            foreach (var current in currentTumbleWeeds)
            {
                if (!previousTumbleWeedsDelta.TryGetValue(current.NetplayId, out var previous) || HasTumbleWeedDelta(previous, current))
                {
                    deltas.Add(current);
                    previousTumbleWeedsDelta[current.NetplayId] = current;
                }
            }

            var currentIds = currentTumbleWeeds.Select(t => t.NetplayId).ToHashSet();
            var keysToRemove = previousTumbleWeedsDelta.Keys.Where(id => !currentIds.Contains(id)).ToList();
            foreach (var key in keysToRemove)
            {
                previousTumbleWeedsDelta.TryRemove(key, out _);
            }

            return deltas;
        }

        private bool HasTumbleWeedDelta(TumbleWeedModel previous, TumbleWeedModel current)
        {
            float positionDelta = Vector3.Distance(
                Quantizer.Dequantize(previous.Position),
                Quantizer.Dequantize(current.Position)
            );

            return positionDelta > TUMBLEWEED_POSITION_THRESHOLD;
        }

        public void RegisterTumbleWeedForInterpolation(uint id, GameObject tumbleWeed)
        {
            EnsureInterpolatorExists();

            tumbleWeedInterpolator.RegisterTumbleWeed(id, tumbleWeed);
        }

        public void UpdateTumbleWeedSnapshots(List<TumbleWeedSnapshot> snapshots)
        {
            EnsureInterpolatorExists();
            tumbleWeedInterpolator.UpdateTumbleWeeds(snapshots);
        }

        public void UnregisterTumbleWeedFromInterpolation(uint id)
        {
            if (tumbleWeedInterpolator != null)
            {
                tumbleWeedInterpolator.UnregisterTumbleWeed(id);
            }
        }

        public void RemoveSpawnedObject(uint id, GameObject obj, bool destroyObject = true)
        {
            spawnedObjects.TryRemove(id, out _);
            if (obj != null && destroyObject)
            {
                GameObject.DestroyImmediate(obj);
            }
        }

        private void EnsureInterpolatorExists()
        {
            if (tumbleWeedInterpolator == null)
            {
                var interpolatorGameObject = new GameObject("TumbleWeedInterpolator");
                tumbleWeedInterpolator = interpolatorGameObject.AddComponent<TumbleWeedInterpolator>();
                Object.DontDestroyOnLoad(interpolatorGameObject);
            }
        }

        public void AddShadyGuyRarityRequest(EItemRarity rarity)
        {
            shadyGuyRarityRequest.Enqueue(rarity);
        }

        public EItemRarity? UnqueueShadyGuyRarityRequest()
        {
            if (shadyGuyRarityRequest.TryDequeue(out var rarity))
            {
                return rarity;
            }
            return null;
        }
    }
}
