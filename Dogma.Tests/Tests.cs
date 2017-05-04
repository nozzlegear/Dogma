using System;
using Xunit;
using Dogma.Attributes;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using System.Linq;
using System.Collections.Generic;

namespace Dogma.Tests
{
    public class Tests
    {
        [Fact]
        public async Task Generates_Interfaces()
        {
            var assemblyName = DependencyContext.Default
                .GetDefaultAssemblyNames()
                .Where(a => a.Name == "Dogma.Tests")
                .First();
            var assembly = Assembly.Load(assemblyName);
            var generator = new Generator();
            var files = await generator.GenerateFiles(assembly);

            foreach (var file in files)
            {
                System.IO.File.WriteAllText($"../../../{file.ModuleName}.generated.d.ts", file.Code);            
            }
        }
    }

    [ToTypeScriptAttribute("test-module")]
    public class TestClass
    {
        public string Foo { get; set; }

        public bool Bar => false;

        public IEnumerable<string> Baz { get; set; }
    }
}
