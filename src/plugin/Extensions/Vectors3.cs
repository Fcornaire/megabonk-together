using UnityEngine;

namespace MegabonkTogether.Extensions
{
    internal static class VectorsExtensions
    {
        public static Vector3 ToUnityVector3(this System.Numerics.Vector3 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vector2 ToUnityVector2(this System.Numerics.Vector2 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static Quaternion ToUnityQuaternion(this System.Numerics.Quaternion vector)
        {
            return new Quaternion(vector.X, vector.Y, vector.Z, vector.W);
        }

        public static System.Numerics.Vector3 ToNumericsVector3(this Vector3 vector)
        {
            float x = (float)System.Math.Round(vector.x, 2);
            float y = (float)System.Math.Round(vector.y, 2);
            float z = (float)System.Math.Round(vector.z, 2);
            return new System.Numerics.Vector3(x, y, z);
        }

        public static System.Numerics.Quaternion ToNumericsQuaternion(this Quaternion quaternion)
        {
            float x = (float)System.Math.Round(quaternion.x, 2);
            float y = (float)System.Math.Round(quaternion.y, 2);
            float z = (float)System.Math.Round(quaternion.z, 2);
            float w = (float)System.Math.Round(quaternion.w, 2);
            return new System.Numerics.Quaternion(x, y, z, w);
        }

        public static System.Numerics.Vector2 ToNumericsVector2(this Vector2 vector)
        {
            float x = (float)System.Math.Round(vector.x, 2);
            float y = (float)System.Math.Round(vector.y, 2);
            return new System.Numerics.Vector2(x, y);
        }

    }
}
