using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Extensions;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Snapshot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegabonkTogether.Services
{
    public interface IProjectileManagerService
    {
        public IEnumerable<Projectile> GetAllProjectiles();
        public IEnumerable<Projectile> GetAllProjectilesDeltaAndUpdate();
        public uint AddSpawnedProjectile(ProjectileBase projectile);
        public ProjectileBase GetProjectileById(uint id);
        public KeyValuePair<uint, ProjectileBase> GetProjectileByReference(ProjectileBase projectile);
        public ProjectileBase RemoveProjectileById(uint id);
        public void ResetForNextLevel();
        public void RemoveProjectile(Projectile projectileId);
        public void RegisterProjectileForInterpolation(uint id, GameObject projectile);
        public void UnregisterProjectileFromInterpolation(uint id);
        public void UpdateProjectileSnapshots(List<ProjectileSnapshot> projectileSnapshots);
        public void RemoveProjectilesByOwnerId(uint connectionId);
    }

    internal class ProjectileManagerService : IProjectileManagerService
    {
        private readonly ConcurrentDictionary<uint, ProjectileBase> spawnedProjectile = [];
        private List<Projectile> previousSpawnedProjectilesDelta = [];
        private uint currentProjectileId = 0;
        private ProjectileInterpolator projectileInterpolator;

        private const float POSITION_THRESHOLD = 0.05f;

        public IEnumerable<Projectile> GetAllProjectiles()
        {
            RemoveAllDeadProjectiles();
            return spawnedProjectile.Select(kv => kv.Value.ToModel(kv.Key)).ToList();
        }

        public IEnumerable<Projectile> GetAllProjectilesDeltaAndUpdate()
        {
            var currentProjectiles = spawnedProjectile.Select(kv => kv.Value.ToModel(kv.Key)).ToList();

            if (previousSpawnedProjectilesDelta.Count == 0)
            {
                previousSpawnedProjectilesDelta = [.. currentProjectiles];
                return currentProjectiles;
            }

            var deltas = new List<Projectile>();

            foreach (var current in currentProjectiles)
            {
                var previous = previousSpawnedProjectilesDelta.FirstOrDefault(p => p.Id == current.Id);

                if (previous == null || HasDelta(previous, current))
                {
                    deltas.Add(current);
                }
            }

            previousSpawnedProjectilesDelta = currentProjectiles.ToList();

            return deltas;
        }

        private bool HasDelta(Projectile previous, Projectile current)
        {
            float positionDelta = Vector3.Distance(
                Quantizer.Dequantize(previous.Position),
                Quantizer.Dequantize(current.Position)
            );

            return positionDelta > POSITION_THRESHOLD;
        }

        public ProjectileBase GetProjectileById(uint id)
        {
            if (spawnedProjectile.TryGetValue(id, out var projo))
            {
                return projo;
            }
            return null;
        }

        private void RemoveAllDeadProjectiles()
        {
            var toRemove = spawnedProjectile.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList();
            foreach (var id in toRemove)
            {
                spawnedProjectile.TryRemove(id, out var _);
            }
        }

        public uint AddSpawnedProjectile(ProjectileBase projectile)
        {
            currentProjectileId++;
            if (!spawnedProjectile.TryAdd(currentProjectileId, projectile))
            {
                Plugin.Log.LogWarning($"Attempted to add a projectile that already exists. Id: {currentProjectileId}");
                return 0;
            }

            return currentProjectileId;
        }

        public KeyValuePair<uint, ProjectileBase> GetProjectileByReference(ProjectileBase projectile)
        {
            return spawnedProjectile.FirstOrDefault(kv => kv.Value == projectile);
        }

        public ProjectileBase RemoveProjectileById(uint id)
        {
            if (!spawnedProjectile.TryRemove(id, out var projectile))
            {
                Plugin.Log.LogWarning($"Attempted to remove an projectile that does not exist {id}");
                return null;
            }

            return projectile;
        }

        public void ResetForNextLevel()
        {
            currentProjectileId = 0;
            spawnedProjectile.Clear();
            previousSpawnedProjectilesDelta.Clear();

            if (projectileInterpolator != null)
            {
                Object.Destroy(projectileInterpolator.gameObject);
                projectileInterpolator = null;
            }
        }

        public void RemoveProjectile(Projectile projectileId)
        {
            var removed = RemoveProjectileById(projectileId.Id);

            if (removed == null)
            {
                Plugin.Log.LogWarning($"Tried to remove projectile with id {projectileId.Id} but it was not found.");
                return;
            }

            GameObject.Destroy(removed.gameObject);
        }

        public void RegisterProjectileForInterpolation(uint id, GameObject projectile)
        {
            EnsureInterpolatorExists();
            projectileInterpolator.RegisterProjectile(id, projectile);
        }

        public void UnregisterProjectileFromInterpolation(uint id)
        {
            if (projectileInterpolator != null)
            {
                projectileInterpolator.UnregisterProjectile(id);
            }
        }

        public void UpdateProjectileSnapshots(List<ProjectileSnapshot> projectileSnapshots)
        {
            EnsureInterpolatorExists();

            if (projectileSnapshots != null && projectileSnapshots.Count > 0)
            {
                projectileInterpolator.UpdateProjectiles(projectileSnapshots);
            }
        }

        public void RemoveProjectilesByOwnerId(uint connectionId)
        {
            var projectilesToRemove = spawnedProjectile
                .Where(kv => kv.Value != null && kv.Key == connectionId)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var projectileId in projectilesToRemove)
            {
                var removedProjectile = RemoveProjectileById(projectileId);
                if (removedProjectile != null)
                {
                    GameObject.DestroyImmediate(removedProjectile.gameObject);
                }
            }
        }

        private void EnsureInterpolatorExists()
        {
            if (projectileInterpolator == null)
            {
                var interpolatorGameObject = new GameObject("ProjectileInterpolator");
                projectileInterpolator = interpolatorGameObject.AddComponent<ProjectileInterpolator>();
                Object.DontDestroyOnLoad(interpolatorGameObject);
            }
        }
    }
}
