using System;

namespace Dogma.Entities
{
    internal class TSType
    {
        public string TypeName { get; set; }

        public Type DiscoveredClass { get; set; }

        public bool IsNullable { get; set; }
    }
}