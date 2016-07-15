namespace TypeScriptToCS
{
    public class TypeNameAndOptional : TypeAndName
    {
        public bool optional;
        public string OptionalString => optional ? "?" : "";
    }
}