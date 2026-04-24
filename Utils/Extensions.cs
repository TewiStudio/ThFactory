using UnityEngine;

namespace Tewi.Helpers.Extensions
{
    public static class Extensions
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
}