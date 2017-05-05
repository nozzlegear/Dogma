using System;
using Xunit;
using Dogma.Attributes;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dogma.Tests
{
    public class Tests
    {
        [Fact]
        public void Generates_Interfaces()
        {
            var assemblyName = DependencyContext.Default
                .GetDefaultAssemblyNames()
                .Where(a => a.Name == "Dogma.Tests")
                .First();
            var assembly = Assembly.Load(assemblyName);
            var generator = new Generator();
            var files = generator.GenerateFiles(assembly);

            foreach (var file in files)
            {
                System.IO.File.WriteAllText($"../../../{file.ModuleName}.generated.d.ts", file.Code);            
            }
        }
    }

    [ToTypeScript("test-module", true)]
    public class TestClass : InterfaceBoy
    {
        public string Foo { get; set; }

        public bool Bar => false;

        public IEnumerable<string> Baz { get; set; }

        public int[] Bat { get; set; }

        public Collection<bool> Collection { get; set; }

        public List<int> List { get; set; }

        public SubClass SubBoy { get; set; }

        public DateTime Date { get; set; }

        public DateTimeOffset DateOffset { get; set; }
    }

    public class SubClass
    {
        public string SubFoo { get; set; }

        public bool SubBar { get; set; }

        public SubClass2 SubSubBoy { get; set; }
    }

    public class InterfaceBoy 
    {
        public string _Id { get; set; }

        public string _Rev { get; set; }
    }

    public class SubClass2 : InterfaceBoy
    {
        public string Hello { get; set; }

        public bool World { get; set; }
    }
}
