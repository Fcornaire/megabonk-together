using MegabonkTogether.Common.Messages;
using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Scripts.Snapshot
{
    public interface ISnapshot
    {
        double Timestamp { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }

    public class PlayerSnapshot : ISnapshot
    {
        public double Timestamp { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public AnimatorState AnimatorState { get; set; }
        public List<ProjectileSnapshot> Projectiles { get; set; }
    }

    public class ProjectileSnapshot
    {
        public double Timestamp { get; set; }
        public uint Id { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }

    public class EnemySnapshot : ISnapshot
    {
        public double Timestamp { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float Hp { get; internal set; }
    }

    public class BossOrbSnapshot : ISnapshot
    {
        public uint Id { get; set; }

        public double Timestamp { get; set; }
        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; }

        public Orb Orb { get; set; }
        public bool IsFired { get; set; }
    }

    public class TumbleWeedSnapshot
    {
        public double Timestamp { get; set; }
        public uint Id { get; set; }
        public Vector3 Position { get; set; }
    }
}
