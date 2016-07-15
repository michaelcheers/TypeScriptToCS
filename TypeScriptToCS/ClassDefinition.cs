using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    public class ClassDefinition : TypeDefinition
    {
        public List<Method> methods = new List<Method>();
        //public List<Property> properties = new List<Property>();
        public List<Field> fields = new List<Field>();
        public List<string> extends = new List<string>();
        public string name;
        public TypeType type;
    }
}
