namespace TypeScriptToCS
{
    public class Field : ClassElement
    {
        public TypeNameAndOptional typeAndName = new TypeNameAndOptional();

        public string CSName
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                    return string.Empty;
                return Program.ChangeName((char.ToUpper(name[0]) + name.Substring(1)).Replace("$", "DollarSign"));
            }
        }

        public string name => typeAndName.name;
    }
}