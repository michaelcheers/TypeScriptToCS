using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    public class Property : ClassElement
    {
        public TypeAndName typeAndName = new TypeAndName();
        public bool get;
        public bool set;
    }
}
