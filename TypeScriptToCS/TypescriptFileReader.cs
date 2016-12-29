using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    public class TypescriptFileReader
    {
        public TypescriptFileReader ()
        {

        }

        public string tsFile;
        int index;
        char Current => End ? '\0' : tsFile[index];

        private string SkipToEndOfWord()
        {
            SkipEmpty();
            if (!char.IsLetter(tsFile, index))
                SkipEmpty();
            if (tsFile[index] == '[')
                return string.Empty;
            string result = "";
            for (; !End; index++)
            {
                if (char.IsLetterOrDigit(Current) || Current == '_' || Current == '.' || Current == '$')
                    result += Current;
                else
                {
                    if (result.EndsWith("]") && !result.EndsWith("[]"))
                    {
                        GoForward(-1);
                        result = result.Substring(0, result.Length - 1);
                    }
                    return result;
                }
            }
            SkipEmpty();
            return result;
        }

        private void SkipEmpty()
        {
            for (; !End; index++)
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

        private void GotoNextOccurenceOf(string value) => index = NextOccurenceOf(value);
        private int NextOccurenceOf(string value) => tsFile.IndexOf(value, index);
        private bool End => tsFile.Length >= index;
        private void GoForward(int value) => index += value;
        private bool IfAt_Skip(char value) { bool result; if (result = Current == value) GoForward(1); return result; }

        private ComplexType ReadType()
        {
            ComplexType result = new ComplexType { complexPart = new List<ComplexType.ComplexPart>() };
            {
                ComplexType.NormalComplexType type;
                if (ReadNormalComplexType(out type))
                {
                    result.complexPart.Add(type);
                    goto Continue;
                }
            }
            {
                ComplexType.FunctionType type;
                if (ReadFunctionType(out type))
                {
                    result.complexPart.Add(type);
                    goto Continue;
                }
            }
            Continue:
            SkipEmpty();
        }

        public bool ReadFunctionType (out ComplexType.FunctionType result)
        {
            SkipEmpty();
            if (IfAt_Skip('('))
            {

            }
            return true;
        }
        
        public Method ParseMethod ()
        {
            Method method = new Method
            {
                typeWheres = ReadType(),
                @static = @static,
                indexer = Current == '[',
                from = typeTop.Last(v => v is ClassDefinition) as ClassDefinition
            };
            var startBracket = Current;
            var endBracket = Current == '[' ? ']' : ')';
            method.typeAndName.name = word;

            SkipEmpty();
            if (IfAt_Skip(endBracket))
                goto Break;

            for (; index < tsFile.Length; index++)
            {
                SkipEmpty();
                optional = false;
                bool @params = false;
                if (IfAt_Skip('.'))
                {
                    GoForward(2);
                    @params = true;
                }

                string word2 = SkipToEndOfWord();

                if (IfAt_Skip('?'))
                    optional = true;

                SkipEmpty();

                switch (Current)
                {
                    case ':':
                        GoForward(1);
                        SkipEmpty();
                        bool bracketIn = Current == '{';
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
                        else if (!ReadFunctionType(tsFile, ref index, ref type2, method.typeAndName.name + "Param" + (method.parameters.Count + 1) + "Delegate", typeTop, namespaceTop, global))
                        {
                            List<string> anys = new List<string>();
                            do
                            {
                                if (tsFile[index] == '\'' || tsFile[index] == '"')
                                {
                                    SkipEmpty();
                                    anys.Add("string");
                                    index = tsFile.IndexOf(Current, index + 1) + 1;
                                    SkipEmpty();
                                }
                                else
                                    anys.Add(SkipToEndOfWord());
                            }
                            while (tsFile[index++] == '|');
                            index--;
                            type2 = string.Join(", ", anys);
                            if (anys.Count > 1)
                                type2 = "Any<" + type2 + ">";
                        }

                        method.parameters.Add(new TypeNameOptionalAndParams
                        {
                            optional = optional,
                            @params = @params,
                            name = word2,
                            type = type2
                        });
                        SkipEmpty();



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
                }

        public bool ReadNormalComplexType (out ComplexType.NormalComplexType result)
        {
            string value = SkipToEndOfWord();
            result = null;
            if (value == "")
                return false;
            List<ComplexType.NormalComplexType.Part> genericParts = new List<ComplexType.NormalComplexType.Part>();
            if (IfAt_Skip('<'))
                while (!IfAt_Skip('>'))
                {
                    string parameter = SkipToEndOfWord();
                    ComplexType.NormalComplexType.Part part = new ComplexType.NormalComplexType.Part();
                    part.typeName = parameter;
                    if (!IfAt_Skip(','))
                    {
                        string word = SkipToEndOfWord();
                        if (word != "extends" || word != "implements")
                            throw new Exception("Word is not \"extends\" or \"implements\".");
                        while (IfAt_Skip(','))
                            part.implements.Add(ReadType());
                    }
                    genericParts.Add(part);
                }
            result = new ComplexType.NormalComplexType
            {
                typeName = value,
                genericParameters = genericParts
            };
            while (IfAt_Skip('['))
                if (!IfAt_Skip(']'))
                    throw new Exception("Array declaration incorrect");
            return true;
        }

        public void Parse ()
        {
            List<NamespaceDefinition> result = new List<NamespaceDefinition>();
            List<NamespaceDefinition> namespaceTop = new List<NamespaceDefinition>();
            List<TypeDefinition> typeTop = new List<TypeDefinition>();

            while (!End)
            {
                SkipEmpty();
                while (IfAt_Skip('/'))
                {
                    if (Current == '/')
                        GotoNextOccurenceOf("\n");
                    else if (Current == '*')
                    {
                        GotoNextOccurenceOf("*/");
                        GoForward(2);
                    }
                }

                SkipEmpty();

                if (End)
                    return;

                BracketLoop:
                if (IfAt_Skip('}'))
                {
                    if (typeTop.Count != 0)
                    {
                        if (namespaceTop.Count == 0)
                            throw new Exception("You must put namespaces around types.");

                        namespaceTop.Last().typeDefinitions.Add(typeTop.Last());
                        typeTop.RemoveAt(typeTop.Count - 1);
                        goto EndBracket;
                    }

                    if (namespaceTop.Count != 0)
                    {
                        result.Add(namespaceTop.Last());
                        namespaceTop.RemoveAt(namespaceTop.Count - 1);
                    }
                    else
                        throw new Exception("No more namespaces to end.");
                    SkipEmpty();
                    goto EndBracket;
                }

                if (IfAt_Skip('{'))
                {
                    SkipEmpty();
                    goto Continue;
                }
                else
                    goto After;

                EndBracket:
                    SkipEmpty();
                    if (End) return;
                    goto Continue;
                After:
                string word;
                bool @static = false;
                bool @abstract = false;
                bool optionalField = false;
                SkipEmpty();
                do
                {
                    word = SkipToEndOfWord();
                    switch (word)
                    {
                        case "static":
                        case "function":
                        case "var":
                        case "let":
                        case "const":
                            @static = true;
                            break;
                        case "abstract":
                            @abstract = true;
                            break;
                    }
                    SkipEmpty();
                }
                while (word == "export" || word == "declare" || word == "static" || word == "function" || word == "var" || word == "const" || word == "abstract" || word == "let");
                var whereTypesExt = GenericRead(tsFile, ref index, ref word);
                switch (word)
                {
                    case "type":
                        {
                            string name = SkipToEndOfWord();
                            var wheres = GenericRead(tsFile, ref index, ref name);
                            SkipEmpty();
                            if (!IfAt_Skip('-'))
                            {
                                index--;
                                goto default;
                            }
                            SkipEmpty();
                            string type = default(string);
                            if (!ReadFunctionType(tsFile, ref index, ref type, name, typeTop, namespaceTop, global))
                            {
                                List<string> anys = new List<string>();
                                List<string> strings = new List<string>();
                                do
                                {
                                    if (tsFile[index] == '\'' || tsFile[index] == '"')
                                    {
                                        SkipEmpty();
                                        strings.Add(tsFile.Substring(index + 1, tsFile.IndexOf(tsFile[index], index + 1) - index - 1));
                                        anys.Add("string");
                                        index = tsFile.IndexOf(tsFile[index], index + 1) + 1;
                                        SkipEmpty();
                                    }
                                    else
                                        anys.Add(SkipToEndOfWord());
                                }
                                while (tsFile[index++] == '|');
                                index--;
                                if (strings.Count == anys.Count)
                                {
                                    var typeDefinitions = namespaceTop.Count == 0 ? global.typeDefinitions : namespaceTop.Last().typeDefinitions;
                                    typeDefinitions.Add(new EnumDefinition
                                    {
                                        name = name,
                                        emit = EnumDefinition.Emit.StringNamePreserveCase,
                                        members = strings
                                    });
                                    type = name;
                                    break;
                                }
                                type = string.Join(", ", anys);
                                if (anys.Count > 1)
                                    type = "Any<" + type + ">";
                            }
                            break;
                        }
                    case "class":
                    case "interface":
                        {
                            var type = ReadType();
                            typeTop.Add(new ClassDefinition
                            {
                                name = type.typeName,
                                typeWheres = type.genericParameters.ToDictionary()
                            });
                            break;
                        }

                    case "enum":
                        typeTop.Add(new EnumDefinition
                        {
                            name = SkipToEndOfWord()
                        });
                        break;

                    case "module":
                    case "namespace":
                        if (tsFile[index] == '\'')
                            index++;
                        namespaceTop.Add(new NamespaceDefinition
                        {
                            name = SkipToEndOfWord()
                        });
                        if (string.IsNullOrEmpty(namespaceTop.Last().name))
                        {
                            namespaceTop.RemoveAt(namespaceTop.Count - 1);
                            goto default;
                        }
                        ClassDefinition globalClass = new ClassDefinition
                        {
                            name = "GlobalClass",
                            type = TypeType.@class,
                            @static = true
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
                        }
                        SkipEmpty();
                        switch (Current)
                        {
                            case ',':
                            case '}':
                                var enumCurrent = typeTop.Last() as EnumDefinition;
                                if (enumCurrent != null)
                                    enumCurrent.members.Add(word);

                                switch (Current)
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
                                    string type = null;
                                    string arr = "";
                                    bool bracket = tsFile[index] == '{';
                                    int endBracketArrIndex = tsFile.IndexOf('}', index) + 1;
                                    if (bracket)
                                        while (tsFile[endBracketArrIndex] == '[')
                                        {
                                            arr += "[]";
                                            tsFile = tsFile.Remove(endBracketArrIndex, 2);
                                        }
                                    if (bracket)
                                        type = char.ToUpper(word[0]) + word.Substring(1) + "Interface";
                                    else if (!ReadFunctionType(tsFile, ref index, ref type, word + "Delegate", typeTop, namespaceTop, global))
                                    {
                                        List<string> anys = new List<string>();
                                        do
                                            anys.Add(SkipToEndOfWord());
                                        while (IfAt_Skip('|'));
                                        index--;
                                        type = string.Join(", ", anys);
                                        if (anys.Count > 1)
                                            type = $"Union<{type}>";
                                    }
                                    SkipEmpty();

                                    int i = 0;
                                    var typeDefinitions = namespaceTop.Count == 0 ? global.typeDefinitions : namespaceTop.Last().typeDefinitions;
                                    foreach (var it in typeDefinitions)
                                    {
                                        if (it is ClassDefinition && (it as ClassDefinition).name == type)
                                        {
                                            type = (i == 0 || i == 1 ? type : type.Substring(0, type.Length - i.ToString().Length)) + (i == 0 ? "" : i.ToString());
                                            i++;
                                        }
                                    }

                                    (typeTop.Last(v => v is ClassDefinition) as ClassDefinition).fields.Add(new Field
                                    {
                                        @static = @static,
                                        from = typeTop.Last(v => v is ClassDefinition) as ClassDefinition,
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
                            case '[':
                                {
                                    Method method = new Method
                                    {
                                        typeWheres = whereTypesExt,
                                        @static = @static,
                                        indexer = Current == '[',
                                        from = typeTop.Last(v => v is ClassDefinition) as ClassDefinition
                                    };
                                    var startBracket = Current;
                                    var endBracket = Current == '[' ? ']' : ')';
                                    method.typeAndName.name = word;

                                    SkipEmpty();
                                    if (IfAt_Skip(endBracket))
                                        goto Break;

                                    for (; index < tsFile.Length; index++)
                                    {
                                        SkipEmpty();
                                        optional = false;
                                        bool @params = false;
                                        if (IfAt_Skip('.'))
                                        {
                                            GoForward(2);
                                            @params = true;
                                        }

                                        string word2 = SkipToEndOfWord();

                                        if (IfAt_Skip('?'))
                                            optional = true;

                                        SkipEmpty();

                                        switch (Current)
                                        {
                                            case ':':
                                                GoForward(1);
                                                SkipEmpty();
                                                bool bracketIn = Current == '{';
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
                                                else if (!ReadFunctionType(tsFile, ref index, ref type2, method.typeAndName.name + "Param" + (method.parameters.Count + 1) + "Delegate", typeTop, namespaceTop, global))
                                                {
                                                    List<string> anys = new List<string>();
                                                    do
                                                    {
                                                        if (tsFile[index] == '\'' || tsFile[index] == '"')
                                                        {
                                                            SkipEmpty();
                                                            anys.Add("string");
                                                            index = tsFile.IndexOf(Current, index + 1) + 1;
                                                            SkipEmpty();
                                                        }
                                                        else
                                                            anys.Add(SkipToEndOfWord());
                                                    }
                                                    while (tsFile[index++] == '|');
                                                    index--;
                                                    type2 = string.Join(", ", anys);
                                                    if (anys.Count > 1)
                                                        type2 = "Any<" + type2 + ">";
                                                }

                                                method.parameters.Add(new TypeNameOptionalAndParams
                                                {
                                                    optional = optional,
                                                    @params = @params,
                                                    name = word2,
                                                    type = type2
                                                });
                                                SkipEmpty();



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

                                            case ']':
                                            case ')':
                                                GoForward(1);
                                                SkipEmpty();
                                                goto Break;
                                        }
                                    }
                                    Break:
                                    bool bracket = false;
                                    string type = "object";
                                    if (Current == ':')
                                    {
                                        index++;
                                        SkipEmpty();
                                        bracket = Current == '{';
                                        if (bracket)
                                            type = char.ToUpper(word[0]) + word.Substring(1) + "Interface";
                                        else if (!ReadFunctionType(tsFile, ref index, ref type, method.typeAndName.name + "Delegate", typeTop, namespaceTop, global))
                                        {
                                            List<string> anys = new List<string>();
                                            List<string> strings = new List<string>();
                                            do
                                            {
                                                if (tsFile[index] == '\'' || tsFile[index] == '"')
                                                {
                                                    SkipEmpty();
                                                    strings.Add(tsFile.Substring(index + 1, tsFile.IndexOf(Current, index + 1) - index - 1));
                                                    anys.Add("string");
                                                    index = tsFile.IndexOf(tsFile[index], index + 1) + 1;
                                                    SkipEmpty();
                                                }
                                                else
                                                    anys.Add(SkipToEndOfWord());
                                            }
                                            while (tsFile[index++] == '|');
                                            index--;
                                            if (strings.Count == anys.Count)
                                            {
                                                var typeDefinitions = namespaceTop.Count == 0 ? global.typeDefinitions : namespaceTop.Last().typeDefinitions;
                                                typeDefinitions.Add(new EnumDefinition
                                                {
                                                    name = word,
                                                    emit = EnumDefinition.Emit.StringNamePreserveCase,
                                                    members = strings
                                                });
                                                type = word;
                                                break;
                                            }
                                            type = string.Join(", ", anys);
                                            if (anys.Count > 1)
                                                type = "Any<" + type + ">";
                                        }
                                        method.typeAndName.type = type;
                                        SkipEmpty();
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
                                    else */
                                    if (string.IsNullOrEmpty(method.typeAndName.name) && !method.indexer)
                                    {
                                        var oldTypeName = (typeTop.Last(v => v is ClassDefinition) as ClassDefinition).name;
                                        typeTop.RemoveAt(typeTop.Count - 1);
                                        method.typeAndName.name = oldTypeName;
                                        typeTop.Add(method);
                                    }
                                    else
                                    {
                                        (typeTop.Last(v => v is ClassDefinition) as ClassDefinition).methods.Add(method);
                                    }
                                    if (bracket)
                                        typeTop.Add(new ClassDefinition
                                        {
                                            type = TypeType.@interface,
                                            name = type
                                        });
                                    if (tsFile[index] == '}')
                                        index--;
                                    goto Continue;
                                }
                        }
                        break;
                }
                Continue:;
            }
        }
    }
}
