using MegabonkTogether.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Scripts.Snapshot
{
    //TODO: Find a way to make abstract class work with IL2CPP because its not working for some reason ¯\_(ツ)_/¯
    public class PlayerInterpolator : MonoBehaviour
    {
        protected float interpolationDelayMs = 0.10f;
        protected int maxBufferSize = 30;

        private Transform modelTransform;
        private Animator animator;
        private HoverAnimations hoverAnimations; //Thanks TonyMcZoom !

        private readonly List<PlayerSnapshot> snapshotsBuffer = new List<PlayerSnapshot>();

        protected void Update()
        {
            if (!HasEnoughSnapshots())
            {
                return;
            }

            double renderTime = Time.timeAsDouble - interpolationDelayMs;
            PerformInterpolation(renderTime);
            CleanupOldSnapshots(renderTime);
        }

        public void Initialize(Transform modelTransform, Animator animator, HoverAnimations hoverAnimations = null)
        {
            this.modelTransform = modelTransform;
            this.animator = animator;
            this.hoverAnimations = hoverAnimations;
        }

        public void AddSnapshot(PlayerSnapshot snapshot)
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
            if (!FindSnapshotPair(renderTime, out PlayerSnapshot older, out PlayerSnapshot newer))
            {
                return;
            }

            float t = CalculateInterpolationFactor(renderTime, older.Timestamp, newer.Timestamp);
            t = Mathf.Clamp01(t);
            InterpolateSnapshot(older, newer, t);
        }

        private bool FindSnapshotPair(double renderTime, out PlayerSnapshot older, out PlayerSnapshot newer)
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

        private void InterpolateSnapshot(PlayerSnapshot older, PlayerSnapshot newer, float t)
        {
            if (modelTransform == null || animator == null)
            {
                return;
            }

            if (hoverAnimations != null)
            {
                hoverAnimations.defaultPos = Vector3.Lerp(older.Position, newer.Position, t);
            }
            else
            {
                modelTransform.position = Vector3.Lerp(older.Position, newer.Position, t);
            }

            if (newer.Rotation != Quaternion.identity)
            {
                if (hoverAnimations != null)
                {
                    hoverAnimations.defaultRotation = Quaternion.Slerp(older.Rotation, newer.Rotation, t).eulerAngles;
                }
                else
                {
                    modelTransform.rotation = Quaternion.Slerp(older.Rotation, newer.Rotation, t);
                }
            }

            animator.UpdateAnimator(newer.AnimatorState);
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
