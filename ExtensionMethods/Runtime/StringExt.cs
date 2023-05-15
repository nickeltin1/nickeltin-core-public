using System.Text;
using UnityEngine;

using C = UnityEngine.Color;

namespace nickeltin.Core.Runtime
{
    public static class StringExt
    {
        private static readonly StringBuilder _sb = new StringBuilder();
        
        private const string _close = ">";
        
        private const string _boldOp = "<b>";
        private const string _boldCls = "</b>";
        private const string _colorOp = "<color=";
        private const string _colorCls = "</color>";
        private const string _italicOp = "<i>";
        private const string _italicCls = "</i>";
        private const string _sizeOp = "<size=";
        private const string _sizeCls = "</size>";
        
        public static string Bold(this string str)
        {
            _sb.Clear();
            _sb.Append(_boldOp);
            _sb.Append(str);
            _sb.Append(_boldCls);
            return _sb.ToString();
        }

        public static string Italic(this string str)
        {
            _sb.Clear();
            _sb.Append(_italicOp);
            _sb.Append(str);
            _sb.Append(_italicCls);
            return _sb.ToString();
        }

        public static string Size(this string str, int size)
        {
            _sb.Clear();
            _sb.Append(_sizeOp);
            _sb.Append(size);
            _sb.Append(_close);
            _sb.Append(str);
            _sb.Append(_sizeCls);
            return _sb.ToString();
        }

        public static string Color(this string str, string color)
        {
            _sb.Clear();
            _sb.Append(_colorOp);
            _sb.Append(color);
            _sb.Append(_close);
            _sb.Append(str);
            _sb.Append(_colorCls);
            return _sb.ToString();
        }

        public static string Color(this string str, Color color)
        {
            return Color(str, "#" + ColorUtility.ToHtmlStringRGB(color).ToLower());
        }
        
        public static string Green(this string str) => str.Color(C.green);
        public static string Red(this string str) => str.Color(C.red);
        public static string Blue(this string str) => str.Color(C.blue);
        public static string Yellow(this string str) => str.Color(C.yellow);
        public static string Magenta(this string str) => str.Color(C.magenta);
        public static string Orange(this string str) => str.Color(new Color(1f, 0.5f, 0f));
        public static string Cyan(this string str) => str.Color(C.cyan);
    }
}