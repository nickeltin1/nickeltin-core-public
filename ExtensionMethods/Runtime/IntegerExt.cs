using System;
using System.Collections;
using UnityEngine;

namespace nickeltin.Core.Runtime
{
    public static class IntegerExt
    {
        public static int Clamp(this ref int i, int min, int max) => i = Mathf.Clamp(i, min, max);
        public static int ClampNoRef(this int i, int min, int max) => Mathf.Clamp(i, min, max);
        
        public static int Clamp0(this ref int i) => i.Clamp(0, i);
        public static int Clamp0NoRef(this int i) => i.ClampNoRef(0, i);
        public static int Clamp0(this ref int i, int max) => i.Clamp(0, max);
        public static int Clamp0NoRef(this int i, int max) => i.ClampNoRef(0, max);

        public static int MapAsIndex(this int i, int step, int length)
        {
            return Mathf.FloorToInt((float) i / step % length);
        }

        private static readonly string[] _suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };  
        public static string FormatAsSize(this long bytes)
        {
            var counter = 0;  
            var number = (decimal)bytes;  
            while (Math.Round(number / 1024) >= 1)  
            {  
                number /= 1024;  
                counter++;  
            }  
            return $"{number:n1}{_suffixes[counter]}";
        }
    }
}