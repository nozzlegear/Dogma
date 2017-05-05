using System;

namespace Dogma.Entities
{
    public class GeneratedInterface
    {
        public GeneratedInterface(string moduleName, string code, Type fromObject)
        {
            ModuleName = moduleName;
            Code = code;
            FromObject = fromObject;
        }

        public string ModuleName { get; set; }

        public string Code { get; set; }

        public Type FromObject { get; set; }
    }
}