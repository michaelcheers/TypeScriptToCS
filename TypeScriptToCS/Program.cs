using Microsoft.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    static class Program
    {
        static string GetTypeWithOutGenerics (string value)
        {
            var index = value.IndexOf('<');
            if (index == -1)
                return value;
            else
                return value.Substring(0, index);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Typescript file location?");
            string tsFileLocation = Console.ReadLine();
            string tsFile = "";

            ReadFile:
            try
            {
                tsFile = File.ReadAllText(tsFileLocation);
            }
            catch (Exception e)
            {
                Console.WriteLine(e + " occured when trying to read file. Press enter to exit. Press t to retry.");
                ConsoleKey key;
                while ((key = Console.ReadKey(true).Key) != ConsoleKey.Enter)
                    if (key == ConsoleKey.R)
                        throw;
                    else if (key == ConsoleKey.T)
                        goto ReadFile;
                Environment.Exit(0);
            }

            int index = 0;
            ReadTypeScriptFile(tsFile, ref index, nameSpaceDefinitions);

            string endFile = "using System;\nusing Bridge;\nusing Bridge.Html5;\nusing Bridge.WebGL;\nusing any = System.Object;\nusing boolean = System.Boolean;\nusing Function = System.Delegate;\nusing RegExp = Bridge.Text.RegularExpressions.Regex;\nusing number = System.Double;\nusing Number = System.Double;\n\n\n";

            foreach (var namespaceItem in nameSpaceDefinitions)
            {
                if (string.IsNullOrEmpty(namespaceItem.name))
                    continue;
                if (namespaceItem.typeDefinitions.Count == 1)
                {
                    if (namespaceItem.typeDefinitions[0].name == "GlobalClass")
                    {
                        namespaceItem.typeDefinitions[0].name = namespaceItem.name;
                        (namespaceItem.typeDefinitions[0] as ClassDefinition).@static = true;
                        ProcessTypeDefinition(namespaceItem.typeDefinitions[0], ref endFile, namespaceItem);
                        continue;
                    }
                }
                if ((namespaceItem.name ?? "") != "")
                    endFile += $"namespace {namespaceItem.name}\n{ "{" }\n";

                List<string> names = new List<string>();
                foreach (var rItem in namespaceItem.typeDefinitions)
                {
                    if (!names.Contains(rItem.name))
                    {
                        names.Add(rItem.name);
                        ProcessTypeDefinition(rItem, ref endFile, namespaceItem);
                    }
                }

                if ((namespaceItem.name ?? "") != "")
                    endFile += "\n}\n";
            }

            Console.WriteLine("Choose a file name...");
            File.WriteAllText(Console.ReadLine(), endFile);
        }

        static List<NamespaceDefinition> nameSpaceDefinitions = new List<NamespaceDefinition>();

        public static void ProcessTypeDefinition (TypeDefinition rItem, ref string endFile, NamespaceDefinition @namespace)
        {
            if (rItem is ClassDefinition)
            {
                ClassDefinition classItem = (ClassDefinition)rItem;
                if (classItem.name == "GlobalClass" && classItem.fields.Count == 0 && classItem.methods.Count == 0/* && classItem.properties.Count == 0*/)
                    return;
                string extendString = classItem.extends.Count != 0 ? " : " : string.Empty;

                List<Field> fields;
                List<Method> methods;
                if (classItem.type == TypeType.@class && !classItem.extends.All(v =>
                {
                    var type = FindType(v) as ClassDefinition;
                    if (type == null)
                        return false;
                    else
                        return type.type == TypeType.@class;
                }))
                    GetMethodsAndFields(classItem, out fields, out methods);
                else
                {
                    fields = new List<Field>(classItem.fields);
                    methods = new List<Method>(classItem.methods);
                }

                if (classItem.type == TypeType.@interface && !(classItem.fields.Count == 0 && classItem.methods.Count == 0/* && classItem.properties.Count == 0*/))
                {
                    List<Field> mFields;
                    List<Method> mMethods;
                    GetMethodsAndFields(classItem, out mFields, out mMethods);
                    endFile += $"\t[ObjectLiteral]\n\tpublic class JSON{classItem.name} : {classItem.name}\n\t{"{"}";
                    List<string> fieldNames = new List<string>();
                    List<Method> methodsDone = new List<Method>();
                    foreach (var item in mFields)
                        if (!fieldNames.Contains(item.typeAndName.name))
                        {
                            fieldNames.Add(item.typeAndName.name);
                            endFile += $"\n#pragma warning disable CS0626\n\t\tpublic extern {item.typeAndName.type.Replace("$", "DollarSign")}{item.typeAndName.OptionalString} {item.CSName}" + " { get; set; }\n#pragma warning restore CS0626";
                        }
                    foreach (var item in mMethods)
                    {
                        if (methodsDone.Any(v => item.typeAndName.name == v.typeAndName.name))
                            continue;
                        var itemClone = item.Clone();
                        itemClone.typeAndName = itemClone.typeAndName.Clone();
                        if (itemClone.typeAndName.name == "")
                            itemClone.typeAndName.name = classItem.name + "IndexerDelegate";
                        else
                            itemClone.typeAndName.name += "Delegate";
                        endFile += "\n";
                        ProcessTypeDefinition(itemClone, ref endFile, @namespace);
                        endFile += $"\n#pragma warning disable CS0626\n\t\tpublic extern {item.typeAndName.type.Replace("$", "DollarSign")} " + (item.indexer ? "this" : item.CSName) + $" {item.StartBracket}" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + " " + ChangeName(v.name) + (v.optional ? " = default(" + v.type + ")" : ""))) + item.EndBracket + (item.indexer ? " { get; set; }" : ";") + "\n#pragma warning restore CS0626";
                        endFile += "\n#pragma warning disable CS0626\n\t\tpublic extern ";
                        endFile += itemClone.name;
                        endFile += " ";
                        if (string.IsNullOrEmpty(item.typeAndName.name))
                            endFile += "indexer";
                        else
                            endFile += ChangeName(char.ToLower(item.CSName[0]) + item.CSName.Substring(1));
                        endFile += " { get; set; }\n#pragma warning restore CS0626";
                        methodsDone.Add(item);
                    }
                    endFile += "\n\t}\n";
                }

                string abstractString = classItem.@abstract ? "abstract " : string.Empty;
                string staticString = classItem.@static ? "static " : string.Empty;

                endFile += "\t[External]" + (classItem.name == "GlobalClass" ? $"\n\t[Name(\"{@namespace.name}\")]" : "") + $"\n\tpublic {staticString}{abstractString}{classItem.type} {ChangeName(classItem.name)}{extendString}{string.Join(", ", classItem.extends.ConvertAll(GetType)) + GetWhereString(classItem.typeWheres) + "\n\t{"}";

                string interfacePublic = classItem.type != TypeType.@interface ? "extern " : string.Empty;
                string pragmaStart = (classItem.type == TypeType.@class ? "\n#pragma warning disable CS0626" : string.Empty);
                string pragmaEnd = (classItem.type == TypeType.@class ? "\n#pragma warning restore CS0626" : string.Empty);
                
                foreach (var item in fields)
                    endFile += pragmaStart + "\n\t\t[FieldProperty]\n\t\t" + (char.IsUpper(item.typeAndName.name[0]) ? "[Name(false)]\n\t\t" : "") + (classItem.type != TypeType.@interface ? "public " : string.Empty) + $"{interfacePublic}" + (item.@static || classItem.name == "GlobalClass" ? "static " : "") + $"{item.typeAndName.type}{item.typeAndName.OptionalString} {item.CSName}" + " { get; set; }" + pragmaEnd;

                List<Method> methoders = new List<Method>();
                foreach (var item in methods)
                    if (!methoders.Any(v => v.typeAndName.name == item.typeAndName.name && v.parameters.SequenceEqual(item.parameters) && v.typeAndName.type == item.typeAndName.type))
                    {
                        methoders.Add(item);
                        if (item.typeAndName.name == "constructor")
                            endFile += $"\n#pragma warning disable CS0824\n\t\tpublic extern {GetTypeWithOutGenerics(classItem.name)} (" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + " " + ChangeName(v.name) + (v.optional ? $" = default({v.type})" : string.Empty))) + ");\n#pragma warning restore CS0824";
                        else
                            endFile += pragmaStart + "\n\t\t" + (item.Dollar ? $"[Name(\"{item.typeAndName.name}\")]\n\t\t" : string.Empty) + (((!(item is ImplicitMethod)) && classItem.type != TypeType.@interface) ? "public " : string.Empty) + interfacePublic + (item.@static || classItem.name == "GlobalClass" ? "static " : "") + item.typeAndName.type + " " + (item.indexer ? "this" : ((item is ImplicitMethod ? ((item as ImplicitMethod).@interface + ".") : ""))) + $"{item.CSName} {item.StartBracket}" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + " " + ChangeName(v.name) + (v.optional && !(item is ImplicitMethod) ? $" = default({v.type})" : string.Empty))) + $"{item.EndBracket}{GetWhereString(item.typeWheres)}" + (item.indexer ? " { get; set; }" : ";") + pragmaEnd;
                    }
                if (classItem.type == TypeType.@class && !classItem.@static && !classItem.methods.Any(v => v.typeAndName.name == "constructor") && classItem.name != "GlobalClass")
                    endFile += $"\n#pragma warning disable CS0824\n\t\textern {GetTypeWithOutGenerics(classItem.name)} ();\n#pragma warning restore CS0824";
                /*foreach (var item in classItem.properties)
                    endFile += "\n\t\tpublic " + (item.@static ? "static " : "") + $"extern {item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)}" + "{ " + (item.get ? "get; " : "") + (item.set ? "set; " : "") + "}";*/
            }
            else if (rItem is EnumDefinition)
            {
                EnumDefinition enumItem = (EnumDefinition)rItem;
                string emitString = enumItem.emit == EnumDefinition.Emit.Value ? "" : "[Enum(Emit." + enumItem.emit + ")]\n\t";
                endFile += $"\t[External]\n\t{emitString}public enum {ChangeName(enumItem.name) + "\n\t{"}\n\t\t{string.Join(",\n\t\t", enumItem.members.ConvertAll(ChangeName))}";
            }
            else if (rItem is Method)
            {
                Method item = (Method)rItem;
                endFile += $"\t[External]\n\tpublic delegate {item.typeAndName.type} {item.name} (" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + " " + ChangeName(v.name) + (v.optional ? $" = default({v.type})" : ""))) + ");\n";
                return;
            }

            endFile += "\n\t}\n";
        }

        static void GetMethodsAndFields (ClassDefinition classItem, out List<Field> fields, out List<Method> methods)
        {
            fields = new List<Field>(classItem.fields);
            methods = new List<Method>(classItem.methods);
            var extends = GetExtends(classItem);
                foreach (var item in extends)
                {
                    if (item.type == TypeType.@interface)
                    {
                        foreach (var fieldItem in item.fields)
                            if (!fields.Any(v => v.typeAndName.name == fieldItem.typeAndName.name))
                                fields.Add(fieldItem);
                        foreach (var methodItem in item.methods)
                            if (!methods.Any(v => v.typeAndName.name == methodItem.typeAndName.name && v.typeAndName.type == methodItem.typeAndName.type && v.parameters.SequenceEqual(methodItem.parameters)))
                                if (classItem.methods.Any(v => v.typeAndName.name == methodItem.typeAndName.name))
                                    methods.Add(new ImplicitMethod
                                    {
                                        name = methodItem.name,
                                        @interface = item.name,
                                        parameters = methodItem.parameters,
                                        @static = methodItem.@static,
                                        typeAndName = methodItem.typeAndName,
                                        typeWheres = methodItem.typeWheres
                                    });
                                else
                                    methods.Add(methodItem);
                    }
                }
        }

        static string GetWhereString (KeyValuePair<string, string> value) => $"where {value.Key} : {value.Value}";
        static string GetWhereString(Dictionary<string, string> value)
        {
            if (value.Count == 0) return "";
            else
            {
                string result = "";
                foreach (var item in value)
                    result += " " + GetWhereString(item);
                return result;
            }
        }

        public static List<ClassDefinition> GetExtends(TypeDefinition definition)
        {
            List<ClassDefinition> result = new List<ClassDefinition>();
            ClassDefinition definitionClass = definition as ClassDefinition;
            if (definitionClass != null)
            {
                foreach (var item in definitionClass.extends)
                {
                    var type = FindType(item) as ClassDefinition;
                    if (type != null)
                    {
                        result.AddRange(GetExtends(type));
                        result.Add(type);
                    }
                }
            }
            return result;
        }

        public static TypeDefinition FindType (string name)
        {
            foreach (var @namespace in nameSpaceDefinitions)
                foreach (var type in @namespace.typeDefinitions)
                {
                    var tName = type.name;
                    if (type is Method)
                        tName = (type as Method).typeAndName.name;
                    if (tName == name)
                        return type;
                }
            return null;
        }

        public static string GetType (string value)
        {
            if (value.Length > 1)
                if (value.EndsWith("]") && value[value.Length - 2] != '[') value = value.Substring(0, value.Length - 1);
            if (value.StartsWith("Array<"))
                return GetType(value.Substring(6, value.Length - 7) + "[]");//Array<int>
            else if (value.EndsWith("[]"))
                return GetType(value.Substring(0, value.Length - 2)) + "[]";
            return value;
        }

        private static void ReadTypeScriptFile(string tsFile, ref int index, List<NamespaceDefinition> namespaces)
        {
            
        }

        public static bool ReadFunctionType (string tsFile, ref int index, ref string outputType, string delegateName, List<TypeDefinition> typeTop, List<NamespaceDefinition> namespaceTop, NamespaceDefinition global)
        {
        }

        public static string ChangeName (string name)
        {
            return new CSharpCodeProvider().CreateEscapedIdentifier(name);
        }
    }
}
