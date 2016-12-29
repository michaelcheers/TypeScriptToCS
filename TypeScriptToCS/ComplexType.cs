using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    public class ComplexType
    {
        public List<ComplexPart> complexPart = new List<ComplexPart>();

        public class ComplexPart
        {
            public int numberOfArrAtEnd;
        }

        public class NormalComplexType : ComplexPart
        {
            public List<Part> genericParameters;
            public string typeName;

            public class Part
            {
                public string typeName;
                public List<ComplexType> implements;
            }
        }

        public class FunctionType : ComplexPart
        {
            public ComplexType returnType;
            public Parameter[] parameters;

            public class Parameter
            {
                public ComplexType type;
                public string parameterName;
            }
        }
    }
}
