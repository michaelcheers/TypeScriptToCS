using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    public class NamespaceDefinition
    {
        public List<TypeDefinition> typeDefinitions = new List<TypeDefinition>();

        public string name;
    }
}
