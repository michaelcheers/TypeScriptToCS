using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    public class TypeNameOptionalAndParams : TypeNameAndOptional
    {
        public bool @params;

        public override bool Equals(object obj)
        {
            var tObj = obj as TypeNameAndOptional;
            if (tObj == null)
                return false;
            return base.Equals(tObj) && optional == tObj.optional;
        }
    }
}
