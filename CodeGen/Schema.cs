using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGen
{
    interface IProperty
    {
    }

    class Property : IProperty
    {
        public Property(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public string Type { get; }
    }

    class UnionProperty : IProperty
    {
        public IProperty[][] Cases { get; }
    }

    interface IType
    {
        string Name { get; }
    }

    class Record : IType
    {
        public Record(string name, IProperty[] properties)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; }
        public IProperty[] Properties { get; }
    }

    class UnionCase
    {
        public UnionCase(string name, IProperty[] properties)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; }
        public IProperty[] Properties { get; }
    }

    class Union : IType
    {
        public Union(string name, UnionCase[] cases)
        {
            Name = name;
            Cases = cases;
        }

        public string Name { get; }
        public UnionCase[] Cases { get; }
    }
}
