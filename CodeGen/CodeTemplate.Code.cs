using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGen
{
    partial class CodeTemplate
    {
        internal CodeTemplate(IEnumerable<TypeInfo> types)
        {
            Types = types.ToArray();
        }

        private IEnumerable<TypeInfo> Types { get; }
    }
}
