using MegabonkTogether.Common.Models;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MegabonkTogether.Helpers
{
    public static class Quantizer
    {
        private static float GetWorldMin()
        {
            return Plugin.Instance.GetWorldSize().x / -2f;
        }

        private static float GetWorldMax()
        {
            return Plugin.Instance.GetWorldSize().x / 2f;
        }

        private static float GetRange()
        {
            return GetWorldMax() - GetWorldMin();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Dequantize(QuantizedRotation rotation)
        {
            ushort QYaw = rotation.QuantizedYaw;
            float yaw = DequantizeYaw(QYaw);
            return Quaternion.Euler(0, yaw, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Quantize(float value)
        {
            float t = (value - GetWorldMin()) / GetRange();
            return (short)(t * short.MaxValue);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedVector4 Quantize(Quaternion rotation)
        {
            return new QuantizedVector4
            {
                QuantizedX = Quantize(rotation.x),
                QuantizedY = Quantize(rotation.y),
                QuantizedZ = Quantize(rotation.z),
                QuantizedW = Quantize(rotation.w)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Dequantize(QuantizedVector4 qRot)
        {
            return new Quaternion(
                Dequantize(qRot.QuantizedX),
                Dequantize(qRot.QuantizedY),
                Dequantize(qRot.QuantizedZ),
                Dequantize(qRot.QuantizedW)
            );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedVector3 Quantize(Vector3 position)
        {
            return new QuantizedVector3
            {
                QuantizedX = Quantize(position.x),
                QuantizedY = Quantize(position.y),
                QuantizedZ = Quantize(position.z)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Dequantize(QuantizedVector3 qPos)
        {
            return new Vector3(
                Dequantize(qPos.QuantizedX),
                Dequantize(qPos.QuantizedY),
                Dequantize(qPos.QuantizedZ)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedVector2 Quantize(Vector2 position)
        {
            return new QuantizedVector2
            {
                QuantizedX = Quantize(position.x),
                QuantizedY = Quantize(position.y)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Dequantize(QuantizedVector2 qPos)
        {
            return new Vector2(
                Dequantize(qPos.QuantizedX),
                Dequantize(qPos.QuantizedY)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dequantize(short q)
        {
            float t = q / (float)short.MaxValue;
            return GetWorldMin() + t * GetRange();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedRotation QuantizeYaw(float yawDeg)
        {
            yawDeg = Mathf.Repeat(yawDeg, 360f);
            var res = (ushort)(yawDeg / 360f * ushort.MaxValue);
            return new QuantizedRotation { QuantizedYaw = res };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DequantizeYaw(QuantizedRotation qRot)
        {
            return DequantizeYaw(qRot.QuantizedYaw);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DequantizeYaw(ushort qYaw)
        {
            return (qYaw / (float)ushort.MaxValue) * 360f;
        }
    }
}
