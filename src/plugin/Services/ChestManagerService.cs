
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Services
{
    public interface IChestManagerService
    {
        public uint AddChest(Object chestObject);

        public void PushNextChestId(uint chestId);
        public uint? SetNextChest(Object chestObject);
        public Object? GetChest(uint chestId);
        public void RemoveChest(uint chestId);
        public KeyValuePair<uint, Object> GetChestByReference(OpenChest instance);
        public void ResetForNextLevel();
    }
    public class ChestManagerService : IChestManagerService
    {
        private readonly ConcurrentDictionary<uint, Object> chests = [];
        private readonly ConcurrentQueue<uint> nextIds = [];
        private uint nextChestId = 0;
        public uint AddChest(Object chestObject)
        {
            var chestId = nextChestId++;

            chests.TryAdd(chestId, chestObject);

            return chestId;
        }

        public void PushNextChestId(uint chestId)
        {
            nextIds.Enqueue(chestId);
        }

        public uint? SetNextChest(Object chestObject)
        {
            if (nextIds.TryDequeue(out var chestId))
            {
                chests.TryAdd(chestId, chestObject);
                return chestId;
            }
            else
            {
                Plugin.Log.LogWarning($"No chest id available for spawned chest.");
                return null;
            }
        }

        public Object? GetChest(uint chestId)
        {
            chests.TryGetValue(chestId, out var chestObject);
            return chestObject;
        }
        public void RemoveChest(uint chestId)
        {
            chests.TryRemove(chestId, out _);
        }

        public KeyValuePair<uint, Object> GetChestByReference(OpenChest instance)
        {
            foreach (var kvp in chests)
            {
                if (kvp.Value == instance.gameObject)
                {
                    return kvp;
                }
            }

            return new KeyValuePair<uint, Object>(0, null);
        }

        public void ResetForNextLevel()
        {
            nextChestId = 0;
            nextIds.Clear();
            //chests.Select(kvp => kvp.Value).ToList().ForEach(GameObject.Destroy);
            chests.Clear();
        }
    }
}
