using System.Collections.Generic;

namespace TypeScriptToCS
{
    public class Method : ClassElement
    {
        public TypeAndName typeAndName = new TypeAndName();

        public List<TypeNameAndOptional> parameters = new List<TypeNameAndOptional>();
    }
}