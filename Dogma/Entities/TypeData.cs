using System.Reflection;
using Dogma.Attributes;

namespace Dogma.Entities
{
    /// <summary>
    /// Contains information about a class or interface decorated with the <see cref="Dogma.Attributes.ToTypeScriptAttribute" />.
    /// </summary>
    internal class TypeData
    {
        public TypeInfo TypeInfo { get; set; }

        public ToTypeScriptAttribute Attribute { get; set; }
    }
}