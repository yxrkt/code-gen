using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGen
{
    class TypeNameAndSize
    {
        public TypeNameAndSize(string name, int size)
        {
            Name = name;
            Size = size;
        }

        public string Name { get; }
        public int Size { get; }
    }

    interface ITypeInfoPart
    {
    }

    class FieldTypeInfoPart : ITypeInfoPart
    {
        public FieldTypeInfoPart(string name, TypeNameAndSize type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public TypeNameAndSize Type { get; }
    }

    class UnionHeaderTypeInfoPart : ITypeInfoPart
    {
        public UnionHeaderTypeInfoPart(int unionID, int size)
        {
            UnionID = unionID;
            Size = size;
        }

        public int UnionID { get; }
        public int Size { get; }
    }

    class NamedUnionHeaderTypeInfoPart : UnionHeaderTypeInfoPart
    {
        public NamedUnionHeaderTypeInfoPart(string name, int unionID, int size)
            : base(unionID, size)
        {
            Name = name;
        }

        public string Name { get; }
    }

    class UnionFieldTypeInfoPart : ITypeInfoPart
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

    static class CodeGenerator
    {
        private static readonly Dictionary<string, int> intrinsicTypes = new Dictionary<string, int>
        {
            { "bool", 1 },

            { "s8", 8 },
            { "u8", 8 },
            { "s16", 16 },
            { "u16", 16 },
            { "s32", 32 },
            { "u32", 32 },
            { "s64", 64 },
            { "u64", 64 },

            { "f32", 32 },
            { "f64", 64 },
        };

        public static string Generate(string json)
        {
            var knownTypes = intrinsicTypes.ToDictionary(kvp => kvp.Key, kvp => new TypeNameAndSize(kvp.Key, kvp.Value));

            var definitions = JsonConvert.DeserializeObject<SchemaTypeDefinitions>(json);

            // TODO: schema -> parts

            // TODO: parts -> tetris

            return definitions.ToString();
        }
    }
}
