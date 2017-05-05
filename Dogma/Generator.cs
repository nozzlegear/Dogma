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
    public static class Generator
    {
        public static IEnumerable<GeneratedFile> GenerateFiles(Assembly assembly)
        {
            // Keep references to all classes that we create. We'll prune duplicates after
            // all classes have been generated and then combine them into single modules.
            List<GeneratedInterface> interfaces = new List<GeneratedInterface>();
            var assemblyTypes = GetTypesWithAttribute(assembly);
            bool firstRun = true;
            
            // While DiscoveredClasses has classes that aren't in the finishedClasses list,
            // keep looping through and build interfaces. 
            List<(Type Type, string ModuleName, bool NullableProps)> discovered = new List<(Type Type, string ModuleName, bool NullableProps)>();

            while (true)
            {
                if (firstRun)
                {
                    foreach (var data in assemblyTypes)
                    {
                        var generated = BuildInterfaceCode(data.Type, data.NullableProps);

                        discovered.AddRange(generated.DiscoveredClasses.Select(disc => (disc, data.ModuleName, data.NullableProps)));
                        interfaces.Add(new GeneratedInterface(data.ModuleName, generated.Code, data.Type));
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
                    discovered = new List<(Type type, string ModuleName, bool NullableProps)>();

                    foreach (var discovery in unique)
                    {
                        var generated = BuildInterfaceCode(discovery.Type, discovery.NullableProps);

                        discovered.AddRange(generated.DiscoveredClasses.Select(t => (t, discovery.ModuleName, discovery.NullableProps)));
                        interfaces.Add(new GeneratedInterface(discovery.ModuleName, generated.Code, discovery.Type));
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

        private static IEnumerable<(Type Type, string ModuleName, bool NullableProps)> GetTypesWithAttribute(Assembly assembly)
        {
            foreach (TypeInfo info in assembly.DefinedTypes)
            {
                var attribute = info.GetCustomAttribute(typeof(ToTypeScriptAttribute), true) as ToTypeScriptAttribute;

                if (attribute != null)
                {
                    yield return (info.AsType(), attribute.ModuleName, attribute.MakePropertiesNullable);
                }
            }
        }

        private static (string Code, List<Type> DiscoveredClasses) BuildInterfaceCode(Type type, bool nullableProperties)
        {
            TypeInfo info = type.GetTypeInfo();
            StringBuilder sb = new StringBuilder();
            List<Type> discovered = new List<Type>();
            string nl = Environment.NewLine;
            string tab = "\t";
            string extends = string.Empty;

            if (info.BaseType != null && info.BaseType != typeof(System.Object))
            {
                // This object extends another class or interface
                discovered.Add(info.BaseType);
                extends = $"extends {info.BaseType.Name} ";
            }
            
            sb.AppendLine(tab + $"export interface {info.Name} {extends}{{");

            foreach (var prop in info.DeclaredProperties)
            {
                var tsType = GetTSType(prop.PropertyType);
                string nullable = nullableProperties ? "?" : string.Empty;

                sb.AppendLine(tab + tab + $"{prop.Name}{nullable}: {tsType.TSTypeName};");

                if (tsType.DiscoveredClass != null)
                {
                    discovered.Add(tsType.DiscoveredClass);
                }
            }
            
            sb.AppendLine(tab + "}");

            string code = sb.ToString();

            return (code, discovered);
        }

        private static (string TSTypeName, Type DiscoveredClass) GetTSType(Type type)
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

        private static bool IsNumber(Type type)
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

        private static bool IsEnumerable(Type type)
        {
            var info = type.GetTypeInfo();

            return info.ImplementedInterfaces.Contains(typeof(System.Collections.IEnumerable));
        }

        private static bool IsClass(Type type)
        {
            var info = type.GetTypeInfo();

            return info.IsClass;
        }
    }
}