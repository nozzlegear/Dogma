namespace Dogma.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public  sealed class ToTypeScriptAttribute : System.Attribute
    {
        public string ModuleName { get; }

        public ToTypeScriptAttribute(string moduleName)
        {
            ModuleName = moduleName;
        }
    }
}