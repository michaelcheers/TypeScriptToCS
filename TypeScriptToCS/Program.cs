using Microsoft.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Namespace name?");
            string nameSpaceName = Console.ReadLine();

            Console.WriteLine("Typescript file location?");
            string tsFileLocation = Console.ReadLine();
            string tsFile = "";

            try
            {
                tsFile = File.ReadAllText(tsFileLocation);
            }
            catch (Exception e)
            {
                Console.WriteLine(e + " occured when trying to read file. Press enter to exit. Press t to rethrow.");
                ConsoleKey key;
                while ((key = Console.ReadKey(true).Key) != ConsoleKey.Enter)
                    if (key == ConsoleKey.R)
                        throw;
                Environment.Exit(0);
            }

            List<NamespaceDefinition> nameSpaceDefinitions = new List<NamespaceDefinition>();

            int index = 0;
            ReadTypeScriptFile(tsFile, ref index, nameSpaceDefinitions);

            string endFile = "using Bridge;\n\n\n";

            foreach (var namespaceItem in nameSpaceDefinitions)
            {
                if ((namespaceItem.name ?? "") != "")
                    endFile += $"namespace {namespaceItem.name}\n{ "{" }\n";

                foreach (var rItem in namespaceItem.typeDefinitions)
                {
                    if (rItem is ClassDefinition)
                    {
                        ClassDefinition classItem = (ClassDefinition)rItem;
                        if (classItem.name == "GlobalClass" && classItem.fields.Count == 0 && classItem.methods.Count == 0 && classItem.properties.Count == 0)
                            continue;
                        string extendString = classItem.extends.Count != 0 ? " : " : string.Empty;
                        string interfaceString = classItem.type == TypeType.@interface ? "\t[ObjectLiteral]\n" : "";

                        endFile += $"\t[External]\n{interfaceString}\tpublic class {ChangeName(classItem.name)}{extendString}{string.Join(", ", classItem.extends.ConvertAll(GetType)) + "\n\t{"}";

                        foreach (var item in classItem.fields)
                            if (!string.IsNullOrEmpty(item.typeAndName.name))
                                endFile += "\n\t\tpublic " + (item.@static ? "static " : "") + $"{item.typeAndName.type}{item.typeAndName.optional} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)};";

                        foreach (var item in classItem.methods)
                            if (!string.IsNullOrEmpty(item.typeAndName.name))
                                endFile += "\n\t\tpublic " + (item.@static ? "static " : "") + "extern " + $"{item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)} (" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + (v.optional ? "? " : " ") + ChangeName(v.name))) + ");";

                        foreach (var item in classItem.properties)
                            endFile += "\n\t\tpublic " + (item.@static ? "static " : "") + $"extern {item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)}" + "{ " + (item.get ? "get; " : "") + (item.set ? "set; " : "") + "}";
                    }
                    else if (rItem is EnumDefinition)
                    {
                        EnumDefinition enumItem = (EnumDefinition)rItem;
                        endFile += $"\t[External]\n\tpublic enum {ChangeName(enumItem.name) + "\n\t{"}\n\t\t{string.Join(",\n\t\t", enumItem.members.ConvertAll(ChangeName))}";
                    }

                    endFile += "\n\t}\n";
                }

                if ((namespaceItem.name ?? "") != "")
                    endFile += "\n}\n";
            }

            File.WriteAllText("output.cs", endFile);
        }

        public static string GetType (string value)
        {
            return value.Replace("any", "object").Replace("number", "double").Replace("Number", "Double").Replace("boolean", "bool");
        }

        private static void ReadTypeScriptFile(string tsFile, ref int index, List<NamespaceDefinition> namespaces)
        {
            NamespaceDefinition global = new NamespaceDefinition();
            namespaces.Add(global);

            List<NamespaceDefinition> namespaceTop = new List<NamespaceDefinition>();
            List<TypeDefinition> typeTop = new List<TypeDefinition>();

            for (; index < tsFile.Length; index++)
            {
                BeginLoop:
                SkipEmpty(tsFile, ref index);
                if (index >= tsFile.Length) return;
                while (tsFile[index] == '/')
                {
                    index++;

                    if (tsFile[index] == '/')
                        index = tsFile.IndexOf('\n', index);
                    else if (tsFile[index] == '*')
                    {
                        index = tsFile.IndexOf("*/", index);
                        index += 2;
                        if (index >= tsFile.Length)
                            return;
                    }

                    SkipEmpty(tsFile, ref index);
                }

                SkipEmpty(tsFile, ref index);

                if (index >= tsFile.Length)
                    break;

                BracketLoop:
                if (tsFile[index] == '}')
                {
                    if (typeTop.Count != 0)
                    {
                        if (typeTop.Last() is ClassDefinition)
                        {
                            if ((typeTop.Last() as ClassDefinition).name == "GlobalClass")
                                goto EndIf;
                        }

                        if (namespaceTop.Count == 0)
                        {
                            global.typeDefinitions.Add(typeTop.Last());
                            typeTop.RemoveAt(typeTop.Count - 1);
                            goto OutIfBreak;
                        }

                        namespaceTop.Last().typeDefinitions.Add(typeTop.Last());
                        typeTop.RemoveAt(typeTop.Count - 1);
                        goto OutIfBreak;
                    }
                    EndIf:

                    namespaces.Add(namespaceTop.Last());
                    namespaceTop.RemoveAt(namespaceTop.Count - 1);
                    goto OutIfBreak;
                }

                if (tsFile[index] == '{')
                {
                    index++;
                    SkipEmpty(tsFile, ref index);
                }

                goto After;

                OutIfBreak:
                if (++index >= tsFile.Length)
                    return;
                SkipEmpty(tsFile, ref index);
                goto BeginLoop;
                After:
                string word;
                bool @static = false;
                bool get = false;
                bool set = false;

                do
                {
                    word = SkipToEndOfWord(tsFile, ref index);
                    switch (word)
                    {
                        case "static":
                            @static = true;
                            break;
                        case "get":
                            get = true;
                            break;
                        case "set":
                            set = true;
                            break;
                    }
                    SkipEmpty(tsFile, ref index);
                }
                while (word == "export" || word == "declare" || word == "static" || word == "get" || word == "set" || word == "function" || word == "var");
                switch (word)
                {
                    case "class":
                    case "interface":
                        typeTop.Add(new ClassDefinition
                        {
                            name = SkipToEndOfWord(tsFile, ref index),
                            type = (TypeType)Enum.Parse(typeof(TypeType), word)
                        });
                        SkipEmpty(tsFile, ref index);

                        var nWord = SkipToEndOfWord(tsFile, ref index);
                        if (nWord == "extends")
                        {
                            SkipEmpty(tsFile, ref index);
                            (typeTop.Last() as ClassDefinition).extends.Add(SkipToEndOfWord(tsFile, ref index));
                        }
                        break;

                    case "enum":
                        typeTop.Add(new EnumDefinition
                        {
                            name = SkipToEndOfWord(tsFile, ref index)
                        });
                        break;

                    case "module":
                    case "namespace":
                        if (tsFile[index] == '\'')
                            index++;
                        namespaceTop.Add(new NamespaceDefinition
                        {
                            name = SkipToEndOfWord(tsFile, ref index)
                        });
                        ClassDefinition globalClass = new ClassDefinition
                        {
                            name = "GlobalClass",
                            type = TypeType.@class
                        };
                        Array.ForEach(new Action<ClassDefinition>[] { typeTop.Add, namespaceTop.Last().typeDefinitions.Add }, v => v(globalClass));
                        if (tsFile[index] == '\'')
                            index++;
                        break;

                    default:
                        char item = tsFile[index++];
                        switch (item)
                        {
                            case ',':
                            case '}':
                                var enumItem = typeTop.Last() as EnumDefinition;
                                if (enumItem != null)
                                    enumItem.members.Add(word);

                                switch (item)
                                {
                                    case '}':
                                        index--;
                                        goto BracketLoop;
                                    case ',':
                                        goto After;
                                    default:
                                        break;
                                }
                                break;

                            case ':':
                                {
                                    SkipEmpty(tsFile, ref index);
                                    var type = SkipToEndOfWord(tsFile, ref index);
                                    SkipEmpty(tsFile, ref index);
                                    bool optional = tsFile[index] == '?';
                                    if (optional)
                                    {
                                        index++;
                                        SkipEmpty(tsFile, ref index);
                                    }


                                (typeTop.Last() as ClassDefinition).fields.Add(new Field
                                {
                                    @static = @static,
                                    typeAndName = new TypeNameAndOptional
                                    {
                                        type = type,
                                        name = word,
                                        optional = optional
                                    }
                                });
                                    continue;
                                }
                            default:
                                continue;

                            case '(':
                                {
                                    Method method = new Method();
                                    method.typeAndName.name = word;
                                    method.@static = @static;

                                    SkipEmpty(tsFile, ref index);
                                    if (tsFile[index] == ')')
                                    {
                                        index++;
                                        SkipEmpty(tsFile, ref index);
                                        goto Break;
                                    }

                                    for (; index < tsFile.Length; index++)
                                    {
                                        SkipEmpty(tsFile, ref index);
                                        bool optional = false;
                                        bool @params = false;
                                        if (tsFile[index] == '.')
                                        {
                                            index += 3;
                                            SkipEmpty(tsFile, ref index); @params = true;
                                        }

                                        string word2 = SkipToEndOfWord(tsFile, ref index);
                                        SkipEmpty(tsFile, ref index);

                                        if (tsFile[index] == '?')
                                        {
                                            optional = true;
                                            index++;
                                        }

                                        SkipEmpty(tsFile, ref index);

                                        switch (tsFile[index])
                                        {
                                            case ':':
                                                index++;
                                                SkipEmpty(tsFile, ref index);
                                                string type2 = SkipToEndOfWord(tsFile, ref index);

                                                method.parameters.Add(new TypeNameOptionalAndParams
                                                {
                                                    optional = optional,
                                                    @params = @params,
                                                    name = word2,
                                                    type = type2
                                                });
                                                SkipEmpty(tsFile, ref index);

                                                if (tsFile[index] != ',')
                                                    goto case ')';

                                                break;

                                            case ')':
                                                index++;
                                                SkipEmpty(tsFile, ref index);
                                                goto Break;
                                        }
                                    }

                                    Break:
                                    if (tsFile[index] == ':')
                                    {
                                        index++;
                                        SkipEmpty(tsFile, ref index);
                                        method.typeAndName.type = SkipToEndOfWord(tsFile, ref index);
                                        SkipEmpty(tsFile, ref index);
                                    }
                                    else
                                    {
                                        method.typeAndName.type = "object";
                                    }

                                    if (get || set)
                                    {
                                        (typeTop.Last() as ClassDefinition).properties.Add(new Property
                                        {
                                            get = get,
                                            set = set,
                                            @static = @static,
                                            typeAndName = method.typeAndName
                                        });
                                    }
                                    else
                                    {
                                        (typeTop.Last() as ClassDefinition).methods.Add(method);
                                    }
                                    goto DoubleBreak;
                                }
                        }
                        break;
                }
                DoubleBreak:;
            }
        }

        private static string SkipToEndOfWord (string tsFile, ref int index)
        {
            if (!char.IsLetter(tsFile, index))
                SkipEmpty(tsFile, ref index);

            string result = "";
            for (; index < tsFile.Length; index++)
            {
                var item = tsFile[index];
                if (char.IsLetterOrDigit(item) || item == '[' || item == ']' || item == '<' || item == '>' || item == '_')
                    result += item;
                else
                    return result;
            }
            return result;
        }

        public static string ChangeName (string name)
        {
            return new CSharpCodeProvider().CreateEscapedIdentifier(name);
        }

        private static void SkipEmpty (string tsFile, ref int index)
        {
            for (; index < tsFile.Length; index++)
            {
                switch (tsFile[index])
                {
                    case '\n':
                    case '\r':
                    case '\t':
                    case ' ':
                        break;
                    default:
                        return;
                }
            }
        }
    }
}
