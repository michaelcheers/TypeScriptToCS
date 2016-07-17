namespace TypeScriptToCS
{
    public class TypeNameAndOptional : TypeAndName
    {
        public bool optional;
        public string OptionalString => optional ? "?" : "";

        public override bool Equals(object obj)
        {
            var tObj = obj as TypeNameAndOptional;
            if (tObj == null)
                return false;
            return base.Equals(tObj) && optional == tObj.optional;
        }
    }
}