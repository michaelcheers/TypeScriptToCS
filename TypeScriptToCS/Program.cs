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

            int index = 0;
            ReadTypeScriptFile(tsFile, ref index, nameSpaceDefinitions);

            string endFile = "using Bridge;\nusing System;\n\n\n";

            foreach (var namespaceItem in nameSpaceDefinitions)
            {
                if ((namespaceItem.name ?? "") != "")
                    endFile += $"namespace {namespaceItem.name}\n{ "{" }\n";

                foreach (var rItem in namespaceItem.typeDefinitions)
                    ProcessTypeDefinition(rItem, ref endFile);

                if ((namespaceItem.name ?? "") != "")
                    endFile += "\n}\n";
            }

            File.WriteAllText("output.cs", endFile);
        }

        static List<NamespaceDefinition> nameSpaceDefinitions = new List<NamespaceDefinition>();

        public static void ProcessTypeDefinition (TypeDefinition rItem, ref string endFile)
        {
            if (rItem is ClassDefinition)
            {
                ClassDefinition classItem = (ClassDefinition)rItem;
                if (classItem.name == "GlobalClass" && classItem.fields.Count == 0 && classItem.methods.Count == 0/* && classItem.properties.Count == 0*/)
                    return;
                string extendString = classItem.extends.Count != 0 ? " : " : string.Empty;

                if (classItem.type == TypeType.@interface && !(classItem.fields.Count == 0 && classItem.methods.Count == 0/* && classItem.properties.Count == 0*/))
                {
                    List<Field> fields = new List<Field>(classItem.fields);
                    List<Method> methods = new List<TypeScriptToCS.Method>(classItem.methods);
                    var extends = GetExtends(classItem);
                    foreach (var item in extends)
                    {
                        fields.AddRange(item.fields);
                        methods.AddRange(item.methods);
                    }
                    endFile += $"\t[ObjectLiteral]\n\tpublic class {classItem.name}ObjectLiteral : {classItem.name}\n\t{"{"}";
                    foreach (var item in fields)
                        endFile += $"\n#pragma warning disable CS0626\n\t\tpublic extern {item.typeAndName.type}{item.typeAndName.OptionalString} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)}" + " { get; set; }\n#pragma warning restore CS0626";
                    foreach (var item in methods)
                    {
                        var itemClone = item.Clone();
                        itemClone.typeAndName = itemClone.typeAndName.Clone();
                        itemClone.typeAndName.name += "Delegate";
                        endFile += "\n";
                        ProcessTypeDefinition(itemClone, ref endFile);
                        endFile += $"\n#pragma warning disable CS0626\n\t\tpublic extern {item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)} (" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + (v.optional ? "? " : " ") + ChangeName(v.name))) + ");\n#pragma warning restore CS0626";
                        endFile += "\n#pragma warning disable CS0626\n\t\tpublic extern ";
                        endFile += $"{item.typeAndName.name}Delegate";
                        endFile += " ";
                        endFile += $"{char.ToLower(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)}";
                        endFile += " { get; set; }\n#pragma warning restore CS0626";
                    }
                    endFile += "\n\t}\n";
                }

                string abstractString = classItem.@abstract ? "abstract " : string.Empty;

                endFile += $"\t[External]\n\tpublic {abstractString}{classItem.type} {ChangeName(classItem.name)}{extendString}{string.Join(", ", classItem.extends.ConvertAll(GetType)) + "\n\t{"}";

                string interfacePublic = classItem.type != TypeType.@interface ? "public extern " : string.Empty;

                foreach (var item in classItem.fields)
                    endFile += $"\n#pragma warning disable CS0626\n\t\t[FieldProperty]\n\t\t{interfacePublic}" + (item.@static ? "static " : "") + $"{item.typeAndName.type}{item.typeAndName.OptionalString} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)}" + " { get; set; }\n#pragma warning restore CS0626";

                foreach (var item in classItem.methods)
                    if (item.typeAndName.name == "constructor")
                        endFile += $"\n#pragma warning disable CS0824\n\t\tpublic extern {classItem.name} (" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + " " + ChangeName(v.name) + (v.optional ? $" = default({v.type})" : string.Empty))) + ");\n#pragma warning restore CS0824";
                    else
                        endFile += $"\n#pragma warning disable CS0626\n\t\t{interfacePublic}" + (item.@static ? "static " : "") + $"{item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)} (" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + " " + ChangeName(v.name) + (v.optional ? $" = default({v.type})" : string.Empty))) + $"){GetWhereString(item.typeWheres)};\n#pragma warning restore CS0626";
                /*foreach (var item in classItem.properties)
                    endFile += "\n\t\tpublic " + (item.@static ? "static " : "") + $"extern {item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)}" + "{ " + (item.get ? "get; " : "") + (item.set ? "set; " : "") + "}";*/
            }
            else if (rItem is EnumDefinition)
            {
                EnumDefinition enumItem = (EnumDefinition)rItem;
                endFile += $"\t[External]\n\tpublic enum {ChangeName(enumItem.name) + "\n\t{"}\n\t\t{string.Join(",\n\t\t", enumItem.members.ConvertAll(ChangeName))}";
            }
            else if (rItem is Method)
            {
                Method item = (Method)rItem;
                endFile += $"\t[External]\n\tpublic delegate {item.typeAndName.type} {item.typeAndName.name} (" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + (v.optional ? "? " : " ") + ChangeName(v.name))) + ");\n";
                return;
            }

            endFile += "\n\t}\n";
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
                    if (type.name == name)
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

                if (index >= tsFile.Length) return;
                if (tsFile[index] == ']')
                {
                    index++;
                    SkipEmpty(tsFile, ref index);
                }
                SkipEmpty(tsFile, ref index);
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
                    if (index >= tsFile.Length) return;
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
                if (index >= tsFile.Length) return;
                if (tsFile[index] == ';')
                {
                    index++;
                    SkipEmpty(tsFile, ref index);
                }
                goto BeginLoop;
                After:
                string word;
                bool @static = false;
                bool @abstract = false;
                bool optionalField = false;
                /*bool get = false;
                bool set = false;*/
                SkipEmpty(tsFile, ref index);
                if (tsFile[index] == '[')
                {
                    optionalField = true;
                    index++;
                    SkipEmpty(tsFile, ref index);
                }
                do
                {
                    word = SkipToEndOfWord(tsFile, ref index);
                    switch (word)
                    {
                        case "static":
                        case "function":
                            @static = true;
                            break;
                        case "abstract":
                            @abstract = true;
                            break;
                        /*case "get":
                            get = true;
                            break;
                        case "set":
                            set = true;
                            break;*/
                    }
                    SkipEmpty(tsFile, ref index);
                }
                while (word == "export" || word == "declare" || word == "static" /*|| word == "get" || word == "set"*/ || word == "function" || word == "var" || word == "const" || word == "abstract");
                bool ext = false;
                Dictionary<string, string> whereTypesExt = new Dictionary<string, string>();
                List<string> typeArguments = new List<string>();
                if (word.Contains('<') && !word.Contains('>'))
                {
                    index -= word.Length - word.IndexOf('<');
                    word = word.Substring(0, word.IndexOf('<') + 1);
                    while (true)
                    {
                        var toAdd = SkipToEndOfWord(tsFile, ref index);
                        if (toAdd == "extends" || toAdd == "implements")
                            ext = true;
                        else if (ext)
                        {
                            var last = toAdd.EndsWith(">");
                            whereTypesExt.Add(typeArguments.Last(), last ? toAdd.Substring(0, toAdd.Length - 1) : toAdd);
                            if (last)
                            {
                                word += ">";
                                break;
                            }
                            ext = false;
                        }
                        else
                        {
                            var last = toAdd.EndsWith(">");
                            typeArguments.Add(last ? toAdd.Substring(0, toAdd.Length - 1) : toAdd);
                            word += typeArguments.Last();
                            if (last)
                            {
                                word += ">";
                                break;
                            }
                        }
                    }
                }
                switch (word)
                {
                    case "class":
                    case "interface":
                        typeTop.Add(new ClassDefinition
                        {
                            name = SkipToEndOfWord(tsFile, ref index),
                            type = (TypeType)Enum.Parse(typeof(TypeType), word),
                            @abstract = @abstract
                        });
                        SkipEmpty(tsFile, ref index);

                        string nWord;
                        while ((nWord = SkipToEndOfWord(tsFile, ref index)) == "extends" || nWord == "implements")
                        {
                            SkipEmpty(tsFile, ref index);
                            (typeTop.Last() as ClassDefinition).extends.Add(SkipToEndOfWord(tsFile, ref index));
                            SkipEmpty(tsFile, ref index);
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
                        bool optional = tsFile[index] == '?';
                        if (optional)
                        {
                            index++;
                            SkipEmpty(tsFile, ref index);
                        }

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
                                    string type = null;
                                    bool bracket = tsFile[index] == '{';
                                    if (bracket)
                                        type = char.ToUpper(word[0]) + word.Substring(1) + "Interface";
                                    else if (!ReadFunctionType(tsFile, ref index, ref type, word + "Delegate", typeTop, namespaceTop))
                                        type = SkipToEndOfWord(tsFile, ref index);
                                    SkipEmpty(tsFile, ref index);


                                    (typeTop.Last() as ClassDefinition).fields.Add(new Field
                                    {
                                        @static = @static,
                                        typeAndName = new TypeNameAndOptional
                                        {
                                            type = type,
                                            name = word,
                                            optional = optionalField
                                        }
                                    });
                                    if (bracket)
                                        typeTop.Add(new ClassDefinition
                                        {
                                            type = TypeType.@interface,
                                            name = type
                                        });
                                    if (tsFile[index] == '}')
                                        index--;
                                    continue;
                                }
                            default:
                                continue;

                            case '(':
                                {
                                    Method method = new Method
                                    {
                                        typeWheres = whereTypesExt,
                                        @static = @static
                                    };
                                    method.typeAndName.name = word;

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
                                        optional = false;
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
                                                bool bracketIn = tsFile[index] == '{';
                                                int endBracketArrIndex = tsFile.IndexOf('}', index) + 1;
                                                string arr = "";
                                                if (bracketIn)
                                                while (tsFile[endBracketArrIndex] == '[')
                                                {
                                                    arr += "[]";
                                                    tsFile = tsFile.Remove(endBracketArrIndex, 2);
                                                }
                                                string type2 = null;
                                                if (bracketIn)
                                                    type2 = word2 + "Interface" + arr;
                                                else if (!ReadFunctionType(tsFile, ref index, ref type2, method.typeAndName.name + "Param" + method.parameters.Count + 1 + "Delegate", typeTop, namespaceTop))
                                                    type2 = SkipToEndOfWord(tsFile, ref index);

                                                method.parameters.Add(new TypeNameOptionalAndParams
                                                {
                                                    optional = optional,
                                                    @params = @params,
                                                    name = word2,
                                                    type = type2
                                                });
                                                SkipEmpty(tsFile, ref index);



                                                if (bracketIn)
                                                {
                                                    typeTop.Add(new ClassDefinition
                                                    {
                                                        type = TypeType.@interface,
                                                        name = type2.Substring(0, type2.Length - arr.Length)
                                                    });
                                                    goto BracketLoop;
                                                }

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
                                    bool bracket = false;
                                    string type = "object";
                                    if (tsFile[index] == ':')
                                    {
                                        index++;
                                        SkipEmpty(tsFile, ref index);
                                        bracket = tsFile[index] == '{';
                                        if (bracket)
                                            type = char.ToUpper(word[0]) + word.Substring(1) + "Interface";
                                        else if (!ReadFunctionType(tsFile, ref index, ref type, method.typeAndName.name + "Delegate", typeTop, namespaceTop))
                                            type = SkipToEndOfWord(tsFile, ref index);
                                        method.typeAndName.type = type;
                                        SkipEmpty(tsFile, ref index);
                                    }
                                    else
                                    {
                                        method.typeAndName.type = "object";
                                    }

                                    /*if (get || set)
                                    {
                                        (typeTop.Last() as ClassDefinition).properties.Add(new Property
                                        {
                                            get = get,
                                            set = set,
                                            @static = @static,
                                            typeAndName = method.typeAndName
                                        });
                                    }
                                    else */if (string.IsNullOrEmpty(method.typeAndName.name))
                                    {
                                        var oldTypeName = (typeTop.Last() as ClassDefinition).name;
                                        typeTop.RemoveAt(typeTop.Count - 1);
                                        method.typeAndName.name = oldTypeName;
                                        typeTop.Add(method);
                                    }
                                    else
                                    {
                                        (typeTop.Last() as ClassDefinition).methods.Add(method);
                                    }
                                    if (bracket)
                                        typeTop.Add(new ClassDefinition
                                        {
                                            type = TypeType.@interface,
                                            name = type
                                        });
                                    goto DoubleBreak;
                                }
                        }
                        break;
                }
                DoubleBreak:;
            }
        }

        public static bool ReadFunctionType (string tsFile, ref int index, ref string outputType, string delegateName, List<TypeDefinition> typeTop, List<NamespaceDefinition> namespaceTop)
        {
            int i = 0;
            foreach (var item in namespaceTop.Last().typeDefinitions)
            {
                if (item is Method && (item as Method).typeAndName.name == delegateName)
                {
                    delegateName = (i == 0 ? delegateName : delegateName.Substring(0, delegateName.Length - i.ToString().Length));
                    i++;
                }
            }
            delegateName = delegateName.Replace(">", "").Replace("<", "");
            List<TypeNameOptionalAndParams> parameters = new List<TypeNameOptionalAndParams>();
            if (tsFile[index] == '(')
            {
                SkipEmpty(tsFile, ref index);
                if (tsFile[++index] == ')')
                {
                    index++;
                    goto EndWhile;
                }
                while (true)
                {
                    SkipEmpty(tsFile, ref index);
                    bool @params = tsFile[index] == '.';
                    if (@params)
                    {
                        index += 3;
                        SkipEmpty(tsFile, ref index);
                    }
                    var word = SkipToEndOfWord(tsFile, ref index);
                    SkipEmpty(tsFile, ref index);
                    bool optional = tsFile[index] == '?';
                    if (optional)
                    {
                        index++;
                        SkipEmpty(tsFile, ref index);
                    }
                    index++;
                    SkipEmpty(tsFile, ref index);
                    var type = SkipToEndOfWord(tsFile, ref index);
                    parameters.Add(new TypeNameOptionalAndParams
                    {
                        name = word,
                        type = type,
                        optional = optional,
                        @params = @params
                    });
                    SkipEmpty(tsFile, ref index);
                    var nextItem = tsFile[index++];
                    if (nextItem == ')')
                        break;
                }
                EndWhile:;
            }
            else
                return false;
            SkipEmpty(tsFile, ref index);
            string returnType = "object";
            if (tsFile[index] == '=')
            {
                index++;
                if (tsFile[index] != '>')
                    goto EndIf;
                index++;
                SkipEmpty(tsFile, ref index);
                returnType = SkipToEndOfWord(tsFile, ref index);
            }
            EndIf:
            namespaceTop.Last().typeDefinitions.Add(new Method
            {
                typeAndName = new TypeAndName
                {
                    name = delegateName,
                    type = returnType
                },
                parameters = parameters
            });
            outputType = delegateName;
            SkipEmpty(tsFile, ref index);
            while (tsFile[index] == '[')
            {
                index += 2;
                outputType += "[]";
            }
            return true;
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
