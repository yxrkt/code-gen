using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGen
{
    partial class CodeTemplate
    {
        internal CodeTemplate(IEnumerable<ICppType> types)
        {
            Types = types.ToArray();
        }

        private IEnumerable<ICppType> Types { get; }
    }
}
