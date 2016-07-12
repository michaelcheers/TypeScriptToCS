using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    public class EnumDefinition : TypeDefinition
    {
        public List<string> members = new List<string>();
        public string name;
    }
}
