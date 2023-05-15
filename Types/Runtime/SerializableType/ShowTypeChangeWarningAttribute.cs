using System;

namespace nickeltin.Core.Runtime
{
    /// <summary>
    /// Valid on <see cref="SerializableType"/> fields to show warning when changing type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowTypeChangeWarningAttribute : Attribute
    {
        public ShowTypeChangeWarningAttribute()
        {
        }
    }
}