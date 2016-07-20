using System.Collections.Generic;

namespace TypeScriptToCS
{
    public class Method : ClassElement, TypeDefinition
    {
        public TypeAndName typeAndName = new TypeAndName();

        public List<TypeNameOptionalAndParams> parameters = new List<TypeNameOptionalAndParams>();

        public Method Clone() => (Method)MemberwiseClone();

        public Dictionary<string, string> typeWheres = new Dictionary<string, string>();

        public string name { get { return typeAndName.name.Replace("$", "DollarSign"); } set { typeAndName.name = value; } }
        public string CSName { get {
                if (string.IsNullOrEmpty(typeAndName.name))
                    return string.Empty;
                return (char.ToUpper(typeAndName.name[0]) + typeAndName.name.Substring(1)).Replace("$", "DollarSign"); } }
        public bool Dollar => typeAndName.name.Contains("$");
        public char StartBracket => indexer ? '[' : '(';
        public char EndBracket => indexer ? ']' : ')';
        public bool indexer;
        public ClassDefinition from;
    }
}