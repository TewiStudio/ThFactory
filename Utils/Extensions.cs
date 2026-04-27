using UnityEngine;

namespace Tewi.Helpers.Extensions
{
    public static class VectorExtensions
    {
        public static Vector3 AddX(this Vector3 v, float x)
        {
            v.x += x;
            return v;
        }

        public static Vector3 AddY(this Vector3 v, float y)
        {
            v.y += y;
            return v;
        }

        public static Vector3 AddZ(this Vector3 v, float z)
        {
            v.z += z;
            return v;
        }

        public static Vector3 SetX(this Vector3 v, float x)
        {
            v.x = x;
            return v;
        }

        public static Vector3 SetY(this Vector3 v, float y)
        {
            v.y = y;
            return v;
        }

        public static Vector3 SetZ(this Vector3 v, float z)
        {
            v.z = z;
            return v;
        }

        /// <summary>
        /// 绕世界 Y 轴旋转向量指定角度（度）。
        /// </summary>
        public static Vector3 RotateY(this Vector3 v, float angle)
        {
            return Quaternion.Euler(0f, angle, 0f) * v;
        }
    }

    public static class ExtemsionMethods
    {
        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}