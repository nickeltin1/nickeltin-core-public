using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace nickeltin.Core.Runtime
{
    public static class VectorExt
    {
        private static readonly System.Random _random = new System.Random();
        
        public static string ToString(this Vector3 vector, int digits)
        {
            return $"{vector.x.ToString("F"+digits)} " +
                   $"{vector.y.ToString("F"+digits)} " +
                   $"{vector.z.ToString("F"+digits)}";
        }

        public static Vector3Int ToInt(this Vector3 vector)
        {
            return new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z);
        }

        public static float GetRandom(this Vector2 vector)
        {
            return vector.GetRandom(_random);
        }
        
        public static float GetRandom(this Vector2 vector, System.Random random)
        {
            return random.NextFloat(vector.x, vector.y);
        }
        
        public static int GetRandom(this Vector2Int vector)
        {
            return vector.GetRandom(_random);
        }
        
        public static int GetRandom(this Vector2Int vector, System.Random random)
        {
            return random.Next(vector.x, vector.y + 1);
        }

        public static Vector3 ToVector3(this Vector2 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }

        /// <summary>
        /// Clamps each axis within range
        /// </summary>
        /// <returns>Clamped vector</returns>
        public static Vector3 Clamp(this ref Vector3 vector, float min, float max)
        {
            return vector = new Vector3(Mathf.Clamp(vector.x, min, max), 
                Mathf.Clamp(vector.y, min, max), 
                Mathf.Clamp(vector.z, min, max));
        }

        /// <summary>
        /// Clamps each axis within range of [-1 - 1]
        /// </summary>
        public static Vector3 Clamp1(this ref Vector3 vector) => vector.Clamp(-1, 1);

        public static bool Approximately(this Vector3 a, Vector3 b, float tolerance = float.Epsilon)
        {
            return (Math.Abs(a.x - b.x) < tolerance && Math.Abs(a.y - b.y) < tolerance && Math.Abs(a.z - b.z) < tolerance);
        }
        
        
        public static Vector3 Parabola(this Vector3 start, Vector3 highest, Vector3 end, float t)
        {
            t.Clamp01();
            return start.ParabolaUclamped(highest, end, t);
        }
        
        public static Vector3 ParabolaUclamped(this Vector3 start, Vector3 highest, Vector3 end, float t)
        {
            return Vector3.LerpUnclamped(Vector3.LerpUnclamped( start, highest, t), 
                Vector3.LerpUnclamped( highest, end, t), t);
        }

        public static Vector3 Abs(this ref Vector3 vector)
        {
            return vector = new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        }

        public static Vector3 AbsNoRef(this Vector3 vector)
        {
            return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        }
    }
}