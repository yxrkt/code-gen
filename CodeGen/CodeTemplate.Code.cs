using System.Linq;

namespace CodeGen
{
    partial class CodeTemplate
    {
        internal CodeTemplate(CppType[] types)
        {
            Types = types;
        }

        private CppType[] Types { get; }
    }

    internal static class CppTypeExtensions
    {
        public static bool IsEnum(this CppType type)
        {
            return type.Cases?.Any() ?? false;
        }

        // TODO: GetFields(), GetMethods()
    }
}
