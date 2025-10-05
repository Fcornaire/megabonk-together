using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Scripts.Snapshot
{
    internal class BossOrbInterpolator : MonoBehaviour
    {
        private GameObject gameObject;

        private readonly List<BossOrbSnapshot> snapshotsBuffer = [];

        protected float interpolationDelayMs = 0.05f;
        protected int maxBufferSize = 200;

        private int lastUsedSnapshotIndex = 0;

        protected void Update()
        {
            if (!HasEnoughSnapshots())
                return;

            double renderTime = Time.timeAsDouble - interpolationDelayMs;
            PerformInterpolation(renderTime);
            CleanupOldSnapshots(renderTime);
        }

        public void Initialize(GameObject go)
        {
            this.gameObject = go;
        }

        public void AddSnapshot(BossOrbSnapshot snapshot)
        {
            snapshotsBuffer.Add(snapshot);

            if (snapshotsBuffer.Count > maxBufferSize)
            {
                snapshotsBuffer.RemoveAt(0);
                lastUsedSnapshotIndex = Mathf.Max(0, lastUsedSnapshotIndex - 1);
            }
        }

        protected bool HasEnoughSnapshots()
        {
            return snapshotsBuffer.Count >= 2;
        }

        protected void PerformInterpolation(double renderTime)
        {
            if (!FindSnapshotPair(renderTime, out BossOrbSnapshot older, out BossOrbSnapshot newer))
                return;

            float t = CalculateInterpolationFactor(renderTime, older.Timestamp, newer.Timestamp);
            t = Mathf.Clamp01(t);

            if (gameObject.transform == null)
            {
                return;
            }

            gameObject.transform.position = Vector3.Lerp(older.Position, newer.Position, t);

            //switch(newer.Orb)
            //{
            //    case Orb.Shooty:
            //        var orbShooty = gameObject.GetComponent<BossOrbShooty>();
            //        if (orbShooty != null)
            //        {
            //            orbShooty.isFired = newer.IsFired;
            //        }
            //        break;
            //    case Orb.Following:
            //        var orbFollowing = gameObject.GetComponent<BossOrb>();
            //        if (orbFollowing != null)
            //        {
            //            orbFollowing.isFired = newer.IsFired;
            //        }
            //        break;
            //    case Orb.Bleed:
            //        var orbBleed = gameObject.GetComponent<BossOrbBleed>();
            //        if (orbBleed != null)
            //        {
            //            orbBleed.isFired = newer.IsFired;
            //        }
            //        break;
            //}
        }

        private bool FindSnapshotPair(double renderTime, out BossOrbSnapshot older, out BossOrbSnapshot newer)
        {
            older = null;
            newer = null;

            int startIndex = Mathf.Max(0, lastUsedSnapshotIndex);


            for (int i = startIndex; i < snapshotsBuffer.Count - 1; i++)
            {
                if (snapshotsBuffer[i].Timestamp <= renderTime &&
                    snapshotsBuffer[i + 1].Timestamp >= renderTime)
                {
                    older = snapshotsBuffer[i];
                    newer = snapshotsBuffer[i + 1];
                    lastUsedSnapshotIndex = i;
                    return true;
                }
            }

            // Si pas trouvé, extrapoler depuis les 2 derniers snapshots disponibles
            if (snapshotsBuffer.Count >= 2)
            {
                older = snapshotsBuffer[snapshotsBuffer.Count - 2];
                newer = snapshotsBuffer[snapshotsBuffer.Count - 1];
                lastUsedSnapshotIndex = snapshotsBuffer.Count - 2;
                return true;
            }

            return false;
        }

        private float CalculateInterpolationFactor(double renderTime, double olderTime, double newerTime)
        {
            return (float)((renderTime - olderTime) / (newerTime - olderTime));
        }

        protected void CleanupOldSnapshots(double renderTime)
        {
            // Compter combien d'éléments à supprimer d'abord
            int removeCount = 0;
            double threshold = renderTime - interpolationDelayMs;

            for (int i = 0; i < snapshotsBuffer.Count - 2; i++)
            {
                if (snapshotsBuffer[i].Timestamp < threshold)
                    removeCount++;
                else
                    break;
            }

            // Supprimer en une seule opération (plus efficace)
            if (removeCount > 0)
            {
                snapshotsBuffer.RemoveRange(0, removeCount);
                lastUsedSnapshotIndex = Mathf.Max(0, lastUsedSnapshotIndex - removeCount);
            }
        }
    }
}
