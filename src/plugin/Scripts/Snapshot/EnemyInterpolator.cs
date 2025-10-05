using Assets.Scripts.Actors.Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Scripts.Snapshot
{
    //TODO: Find a way to make abstract class work with IL2CPP because its not working for some reason ¯\_(ツ)_/¯
    public class EnemyInterpolator : MonoBehaviour
    {
        private Enemy enemy;

        private readonly List<EnemySnapshot> snapshotsBuffer = new List<EnemySnapshot>();

        protected float interpolationDelayMs = 0.1f;
        protected int maxBufferSize = 200;

        protected void Update()
        {
            if (!HasEnoughSnapshots())
                return;

            double renderTime = Time.timeAsDouble - interpolationDelayMs;
            PerformInterpolation(renderTime);
            CleanupOldSnapshots(renderTime);
        }

        public void Initialize(Enemy enemy)
        {
            this.enemy = enemy;
        }

        public void AddSnapshot(EnemySnapshot snapshot)
        {
            snapshotsBuffer.Add(snapshot);

            if (snapshotsBuffer.Count > maxBufferSize)
            {
                snapshotsBuffer.RemoveAt(0);
            }
        }

        protected bool HasEnoughSnapshots()
        {
            return snapshotsBuffer.Count >= 2;
        }

        protected void PerformInterpolation(double renderTime)
        {
            if (!FindSnapshotPair(renderTime, out EnemySnapshot older, out EnemySnapshot newer))
                return;

            enemy.hp = newer.Hp;

            float dist = Vector3.Distance(older.Position, newer.Position);
            if (dist > 2.0f)
            {
                enemy.transform.position = newer.Position;
                enemy.transform.rotation = newer.Rotation;
                return;
            }

            float t = CalculateInterpolationFactor(renderTime, older.Timestamp, newer.Timestamp);
            t = Mathf.Clamp01(t);

            if (enemy.transform == null)
            {
                return;
            }

            enemy.transform.position = Vector3.Lerp(older.Position, newer.Position, t);
            enemy.transform.rotation = Quaternion.Slerp(older.Rotation, newer.Rotation, t);
        }

        private bool FindSnapshotPair(double renderTime, out EnemySnapshot older, out EnemySnapshot newer)
        {
            older = null;
            newer = null;

            for (int i = 0; i < snapshotsBuffer.Count - 1; i++)
            {
                if (snapshotsBuffer[i].Timestamp <= renderTime &&
                    snapshotsBuffer[i + 1].Timestamp >= renderTime)
                {
                    older = snapshotsBuffer[i];
                    newer = snapshotsBuffer[i + 1];
                    return true;
                }
            }

            return false;
        }

        private float CalculateInterpolationFactor(double renderTime, double olderTime, double newerTime)
        {
            return (float)((renderTime - olderTime) / (newerTime - olderTime));
        }

        protected void CleanupOldSnapshots(double renderTime)
        {
            while (snapshotsBuffer.Count > 2 &&
                   snapshotsBuffer[0].Timestamp < renderTime - interpolationDelayMs)
            {
                snapshotsBuffer.RemoveAt(0);
            }
        }
    }
}
