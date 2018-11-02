using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGen
{
    internal class TypeNameAndSize
    {
        public TypeNameAndSize(string name, int size)
        {
            Name = name;
            Size = size;
        }

        public string Name { get; }
        public int Size { get; }
    }

    internal abstract class TypeInfoPart
    {
    }

    internal class FieldTypeInfoPart : TypeInfoPart
    {
        public FieldTypeInfoPart(string name, TypeNameAndSize type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public TypeNameAndSize Type { get; }
    }

    internal class UnionHeaderTypeInfoPart : TypeInfoPart
    {
        public UnionHeaderTypeInfoPart(int unionID, int size)
        {
            UnionID = unionID;
            Size = size;
        }

        public int UnionID { get; }
        public int Size { get; }
    }

    internal class NamedUnionHeaderTypeInfoPart : UnionHeaderTypeInfoPart
    {
        public NamedUnionHeaderTypeInfoPart(string name, int unionID, int size)
            : base(unionID, size)
        {
            Name = name;
        }

        public string Name { get; }
    }

    internal class UnionFieldTypeInfoPart : TypeInfoPart
    {
        public UnionFieldTypeInfoPart(int unionID, string name, TypeNameAndSize type)
        {
            UnionID = unionID;
            Name = name;
            Type = type;
        }

        public int UnionID { get; }
        public string Name { get; }
        public TypeNameAndSize Type { get; }
    }

    internal class CodeGenerator2
    {
    }
}
