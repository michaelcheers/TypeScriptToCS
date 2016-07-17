using System;

namespace TypeScriptToCS
{
    public class TypeAndName
    {
        public string type { get { return Program.GetType(_type); } set { _type = value; } }
        protected string _type;
        public string name;

        public TypeAndName Clone() => (TypeAndName)MemberwiseClone();

        public override bool Equals(object obj)
        {
            var tObj = obj as TypeAndName;
            if (tObj == null)
                return false;
            return tObj._type == _type && tObj.name == name;
        }
    }
}