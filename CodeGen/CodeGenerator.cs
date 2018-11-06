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

    enum TypeInfoPartClassification
    {
        SmallInt,
        SmallFloat,
        Big
    }

    interface ITypeInfoPart
    {
        int Size { get; }
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
        public int Size => Type.Size;
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
        public int Size => Type.Size;
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

                TypeInfo typeInfo;
                if (type.IsUnion)
                {
                    var caseNames = type.Cases.Select(c => c.Name).ToArray();
                    
                }
                else
                {
                    //typeInfo = new TypeInfo(type.Name, )
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

            (ITypeInfoPart[] parts, int size) ArrangeParts(ITypeInfoPart[] parts)
            {
                var classifiedParts = (
                    from part in parts
                    let classification = Classify(part)
                    group part by classification
                ).ToDictionary(g => g.Key, g => g.ToArray());

                if (classifiedParts.TryGetValue(TypeInfoPartClassification.SmallInt, out ITypeInfoPart[] smallInts))
                {
                    var orderedSmallInts = smallInts.OrderByDescending(i => i.Size).ToArray();
                    Group(orderedSmallInts).OrderBy(g => g.group).Select(g => g.part);
                }

                var smallInts = classifiedParts[TypeInfoPartClassification.SmallInt];

                return (parts, 0);

                TypeInfoPartClassification Classify(ITypeInfoPart part)
                {
                    if (part is UnionHeaderTypeInfoPart)
                    {
                        return TypeInfoPartClassification.SmallInt;
                    }
                    else
                    {
                        var field = part as FieldTypeInfoPart;
                        var unionField = part as UnionFieldTypeInfoPart;

                        var (name, size) = field != null ? (field.Name, field.Size) : (unionField.Name, unionField.Size);

                        if (size < 64 && intrinsicTypes.ContainsKey(name))
                        {
                            return name.StartsWith("f") ? TypeInfoPartClassification.SmallFloat : TypeInfoPartClassification.SmallInt;
                        }
                        else
                        {
                            return TypeInfoPartClassification.Big;
                        }
                    }
                }

                IEnumerable<(ITypeInfoPart part, int group)> Group(ITypeInfoPart[] orderedParts)
                {
                    var groupSizes = new SortedList<int, int> { { 0, 0 } };

                    foreach (var part in orderedParts)
                    {
                        bool grouped = false;
                        foreach (var (id, size) in groupSizes)
                        {
                            var combinedSize = size + part.Size;
                            if (combinedSize <= 64)
                            {
                                yield return (part, id);
                                grouped = true;
                                break;
                            }
                        }

                        if (!grouped)
                        {
                            var groupIndex = groupSizes.Count;
                            groupSizes.Add(groupIndex, part.Size);
                            yield return (part, groupIndex);
                        }
                    }
                }
            }
        }
    }

    public static class Extensions
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}
