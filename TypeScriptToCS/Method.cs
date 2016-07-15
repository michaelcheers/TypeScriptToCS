using System.Collections.Generic;

namespace TypeScriptToCS
{
    public class Method : ClassElement
    {
        public TypeAndName typeAndName = new TypeAndName();

        public List<TypeNameOptionalAndParams> parameters = new List<TypeNameOptionalAndParams>();
    }
}