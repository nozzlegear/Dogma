using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Dogma.Attributes;
using Dogma.Entities;
using Newtonsoft.Json;

namespace Dogma
{
    public static class Generator
    {
        /// <summary>
        /// Discovers all classes and interfaces in the <paramref name="assembly" /> and converts them to TypeScript module declarations.
        /// </summary>
        public static IEnumerable<GeneratedModule> GenerateModules(Assembly assembly)
        {
            // Keep references to all classes that we create. We'll prune duplicates after
            // all classes have been generated and then combine them into single modules.
            List<GeneratedInterface> interfaces = new List<GeneratedInterface>();

            // Keep looping through and building interfaces while the discovered list has classes that aren't in the finished interfaces list. 
            List<(Type Type, string ModuleName, bool NullableProps)> discovered = DiscoverTypesWithAttributes(assembly).ToList();

            while (true)
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

                    return new GeneratedModule(moduleName, sb.ToString());
                });
        }

        /// <summary>
        /// Discovers all types using the <see cref="ToTypeScriptAttribute" /> in the assembly.
        /// </summary>
        private static IEnumerable<(Type Type, string ModuleName, bool NullableProps)> DiscoverTypesWithAttributes(Assembly assembly)
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

        /// <summary>
        /// Takes a class or interface type and builds a TypeScript interface out of it, while simultaneously discovering interfaces and classes used by the parent.
        /// </summary>
        private static (string Code, List<Type> DiscoveredClasses) BuildInterfaceCode(Type type, bool nullableProperties)
        {
            TypeInfo info = type.GetTypeInfo();
            List<Type> discovered = new List<Type>();
            string nl = Environment.NewLine;
            string tab = "\t";

            if (info.IsEnum)
            {
                string enumCode = string.Join(" | ", Enum.GetNames(type).Select(name => $"\"{name}\""));
                return (tab + $"export type {info.Name} = ({enumCode});" + nl, discovered);
            }

            StringBuilder sb = new StringBuilder();
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
                if (prop.Name == "NullableIntArray")
                {
                    string breaker = "";
                }

                var tsType = GetTSType(prop.PropertyType);
                string propName = GetName(prop);
                string nullable = nullableProperties || tsType.IsNullable ? "?" : string.Empty;

                sb.AppendLine(tab + tab + $"{propName}{nullable}: {tsType.TypeName};");

                if (tsType.DiscoveredClass != null)
                {
                    discovered.Add(tsType.DiscoveredClass);
                }
            }
            
            sb.AppendLine(tab + "}");

            string code = sb.ToString();

            return (code, discovered);
        }

        /// <summary>
        /// Attempts to get the JsonProperty name from the type. If the property doesn't have a JsonProperty name, this method will instead return the default name.
        /// </summary>
        private static string GetName(PropertyInfo prop)
        {
            var jsonProperty = prop.GetCustomAttribute(typeof(JsonPropertyAttribute)) as JsonPropertyAttribute;

            return jsonProperty?.PropertyName ?? prop.Name;
        }

        /// <summary>
        /// Translates a type to a TypeScript type, while also discovering underlying classes or interfaces.
        /// </summary>
        private static TSType GetTSType(Type type, bool parentIsEnumerable = false)
        {
            var output = new TSType()
            {
                TypeName = type.Name   
            };

            if (type.IsArray)
            {
                var arrayType = GetTSType(type.GetElementType());
                arrayType.TypeName += "[]";

                return arrayType;
            }

            if (type == typeof(String))
            {
                output.TypeName = "string";

                return output;
            }

            if (type == typeof(Boolean))
            {
                output.TypeName = "boolean";

                return output;
            }

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                output.TypeName = "Date";

                return output;
            }

            if (IsNumber(type))
            {
                output.TypeName = "number";

                return output;
            }

            var info = type.GetTypeInfo();

            if (info.IsEnum)
            {
                output.TypeName = type.Name;
                output.DiscoveredClass = type;

                return output;
            }

            if (info.IsGenericType && info.ImplementedInterfaces.Any(i => i == typeof(System.Collections.IEnumerable)))
            {
                var genericType = GetTSType(type.GenericTypeArguments.First(), true);
                genericType.TypeName += "[]";

                return genericType;
            }

            if (info.IsGenericType && info.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var genericType = GetTSType(type.GenericTypeArguments.First());
                genericType.IsNullable = !parentIsEnumerable;

                return genericType;
            }

            if (info.IsClass)
            {
                output.TypeName = type.Name;
                output.DiscoveredClass = type;

                return output;
            }

            // Unknown type. This will probably break the definition, but we'll cross our fingers and return it anyway.
            return output;
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
    }
}