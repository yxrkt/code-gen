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

    class TypeInfo
    {
        public TypeInfo(string name, int size, ITypeInfoPart[] parts)
        {
            Name = name;
            Size = size;
            Parts = parts;
        }

        public TypeInfo(string name, int size, string[] caseNames, ITypeInfoPart[] parts)
            : this(name, size, parts)
        {
            CaseNames = caseNames;
        }

        public string Name { get; }

        public int Size { get; }

        public ITypeInfoPart[] Parts { get; }

        public string[] CaseNames { get; }

        public bool IsUnion => CaseNames?.Any() ?? false;
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
            var knownTypes = intrinsicTypes.ToDictionary(
                kvp => kvp.Key,
                kvp => new TypeNameAndSize(kvp.Key, kvp.Value));

            var definitions = JsonConvert.DeserializeObject<SchemaTypeDefinitions>(json);

            foreach (var type in definitions.Types)
            {
                var typeInfoParts = GetTypeInfoParts(type).ToArray();

                var arrangedParts = ArrangeParts(typeInfoParts);

                if (type.IsUnion)
                {
                    var caseNames = type.Cases.Select(c => c.Name).ToArray();
                    
                }
                else
                {

                }

                // parts -> tetris
                // knownTypes.Add
                // type -> c++
            }

            return definitions.ToString();

            IEnumerable<ITypeInfoPart> GetTypeInfoParts(SchemaType type)
            {
                var nextUnionID = 0;
                if (type.IsUnion)
                {
                    foreach (var part in GetUnionTypeInfoParts(type.Cases))
                    {
                        yield return part;
                    }
                }
                else
                {
                    foreach (var property in type.Properties)
                    {
                        if (property.IsUnion)
                        {
                            foreach (var part in GetUnionTypeInfoParts(property.Cases))
                            {
                                yield return part;
                            }
                        }
                        else
                        {
                            yield return new FieldTypeInfoPart(
                                name: property.Name,
                                type: knownTypes[property.Type]);
                        }
                    }
                }

                IEnumerable<ITypeInfoPart> GetUnionTypeInfoParts(ISchemaUnionCase[] cases)
                {
                    var unionID = nextUnionID++;

                    var headerSize = Utilities.GetMinimumBitsForInt(cases.Length);
                    yield return new UnionHeaderTypeInfoPart(unionID, headerSize);

                    foreach (var @case in cases)
                    {
                        foreach (var property in @case.Properties)
                        {
                            if (property.IsUnion)
                            {
                                foreach (var part in GetUnionTypeInfoParts(property.Cases))
                                {
                                    yield return part;
                                }
                            }
                            else
                            {
                                yield return new UnionFieldTypeInfoPart(
                                    unionID: unionID,
                                    name: property.Name,
                                    type: knownTypes[property.Type]);
                            }
                        }
                    }
                }
            }

            ITypeInfoPart[] ArrangeParts(ITypeInfoPart[] parts)
            {

            }
        }
    }
}
