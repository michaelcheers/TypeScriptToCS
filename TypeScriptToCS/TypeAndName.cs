namespace TypeScriptToCS
{
    public class TypeAndName
    {
        public string type { get { return Program.GetType(_type); } set { _type = value; } }
        string _type;
        public string name;
    }
}