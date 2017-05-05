namespace Dogma.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public  sealed class ToTypeScriptAttribute : System.Attribute
    {
        public string ModuleName { get; }

        public bool MakePropertiesNullable { get; }

        public ToTypeScriptAttribute(string moduleName, bool makePropertiesNullable = false)
        {
            ModuleName = moduleName;
            MakePropertiesNullable = makePropertiesNullable;
        }
    }
}