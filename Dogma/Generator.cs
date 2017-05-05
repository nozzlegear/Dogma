using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dogma.Attributes;
using Dogma.Entities;

namespace Dogma
{
    public class Generator
    {
        public async Task<IEnumerable<GeneratedFile>> GenerateFiles(Assembly assembly)
        {
            // Keep references to all classes that we create. We'll prune duplicates after
            // all classes have been generated and then combine them into single modules.
            List<GeneratedInterface> interfaces = new List<GeneratedInterface>();
            var assemblyTypes = GetTypesWithAttribute(assembly);
            bool firstRun = true;
            
            // While DiscoveredClasses has classes that aren't in the finishedClasses list,
            // keep looping through and build interfaces. 
            List<(Type Type, string ModuleName)> discovered = new List<(Type Type, string ModuleName)>();

            while (true)
            {
                if (firstRun)
                {
                    foreach (var data in assemblyTypes)
                    {
                        var generated = GenerateInterface(data.Type, data.ModuleName);

                        discovered.AddRange(generated.DiscoveredClasses.Select(disc => (disc, data.ModuleName)));
                        interfaces.Add(generated.Interface);
                    }

                    firstRun = false;
                }
                else
                {
                    // Get unique types that haven't already been generated.
                    var unique = discovered
                        .GroupBy(t => t.Type.FullName)
                        .Select(t => t.First())
                        .Where(t => ! interfaces.Any(generated => generated.FromObject == t.Type));
                    discovered = new List<(Type type, string ModuleName)>();

                    foreach (var discovery in unique)
                    {
                        var generated = GenerateInterface(discovery.Type, discovery.ModuleName);

                        discovered.AddRange(generated.DiscoveredClasses.Select(t => (t, discovery.ModuleName)));
                        interfaces.Add(generated.Interface);
                    }
                }

                if (discovered == null || discovered.Count() == 0)
                {
                    break;
                }
            }

            return interfaces.GroupBy(c => c.ModuleName)
                .Select(module => 
                {
                    string moduleName = module.First().ModuleName;
                    string code = string.Join("", module.Select(m => m.Code));
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("/// <auto-generated>");
                    sb.AppendLine($"/// This code was auto-generated by Dogma for .NET Core on {DateTime.UtcNow} UTC. Do not manually edit this file.");
                    sb.AppendLine("/// </auto-generated>");
                    sb.AppendLine($"declare module \"{moduleName}\" {{");
                    sb.Append(code);
                    sb.Append("}");

                    return new GeneratedFile(moduleName, sb.ToString());
                });
        }

        private (GeneratedInterface Interface, List<Type> DiscoveredClasses) GenerateInterface(Type type, string moduleName)
        {
            TypeInfo info = type.GetTypeInfo();
            StringBuilder sb = new StringBuilder();
            List<Type> discovered = new List<Type>();
            string nl = Environment.NewLine;
            string tab = "\t";
            
            sb.AppendLine(tab + $"export interface {info.Name} {{");

            foreach (var prop in info.DeclaredProperties)
            {
                var tsType = GetTSType(prop.PropertyType);

                sb.AppendLine(tab + tab + $"{prop.Name}?: {tsType.TSTypeName};");

                if (tsType.DiscoveredClass != null)
                {
                    discovered.Add(tsType.DiscoveredClass);
                }
            }
            
            sb.AppendLine(tab + "}");

            string code = sb.ToString();

            return (new GeneratedInterface(moduleName, code, type), discovered);
        }

        private IEnumerable<(Type Type, string ModuleName)> GetTypesWithAttribute(Assembly assembly)
        {
            foreach (TypeInfo info in assembly.DefinedTypes)
            {
                var attribute = info.GetCustomAttribute(typeof(ToTypeScriptAttribute), true) as ToTypeScriptAttribute;

                if (attribute != null)
                {
                    var data = new TypeData()
                    {
                        TypeInfo = info,
                        Attribute = attribute
                    };

                    yield return (info.AsType(), attribute.ModuleName);
                }
            }
        }

        private (string TSTypeName, Type DiscoveredClass) GetTSType(Type type)
        {
            if (type.IsArray)
            {
                var arrayType = GetTSType(type.GetElementType());
                string typeName = arrayType.TSTypeName + "[]";

                return (typeName, arrayType.DiscoveredClass);
            }

            if (type == typeof(String))
            {
                return ("string", null);
            }

            if (type == typeof(Boolean))
            {
                return ("boolean", null);
            }

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return ("Date", null);
            }

            if (IsNumber(type))
            {
                return ("number", null);
            }

            if (IsEnumerable(type) && type.IsConstructedGenericType)
            {
                var genericType = GetTSType(type.GenericTypeArguments.First());
                string typeName = genericType.TSTypeName + "[]";

                return (typeName, genericType.DiscoveredClass);
            }

            if (IsClass(type))
            {
                return (type.Name, type);
            }

            return (type.Name, null);
        }

        private bool IsNumber(Type type)
        {
            Type[] numberTypes = { 
                typeof(sbyte),
                typeof(byte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(decimal)
            };

            return numberTypes.Contains(type);
        }

        private bool IsEnumerable(Type type)
        {
            var info = type.GetTypeInfo();

            return info.ImplementedInterfaces.Contains(typeof(System.Collections.IEnumerable));
        }

        private bool IsClass(Type type)
        {
            var info = type.GetTypeInfo();

            return info.IsClass;
        }
    }
}