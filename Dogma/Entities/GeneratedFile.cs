namespace Dogma.Entities
{
    public class GeneratedFile
    {
        public GeneratedFile(string moduleName, string code)
        {
            ModuleName = moduleName;
            Code = code;
        }

        public string ModuleName { get; set; }

        public string Code { get; set; }
    }
}