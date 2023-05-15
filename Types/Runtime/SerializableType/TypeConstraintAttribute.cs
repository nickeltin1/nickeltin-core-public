using System;

namespace nickeltin.Core.Runtime
{
    /// <summary>
    /// Valid on <see cref="SerializableType"/> fields to constrain base type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TypeConstraintAttribute : Attribute
    {
        public readonly Type constrainedBaseType;
        
        public TypeConstraintAttribute(Type baseType)
        {
            constrainedBaseType = baseType;
        }
    }
}