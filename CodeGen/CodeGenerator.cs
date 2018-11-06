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
        Int,
        Float,
        Other
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

            var bitFieldSize = parts.TakeWhile(part => part.Classify() == TypeInfoPartClassification.Int).Sum(part => part.Size);
            BitFieldType =
                bitFieldSize <= 8
                ? "u8"
                : bitFieldSize <= 16
                ? "u16"
                : bitFieldSize <= 32
                ? "u32"
                : "u64";
        }

        public TypeInfo(string name, int size, string[] caseNames, ITypeInfoPart[] parts)
            : this(name, size, parts)
        {
            CaseNames = caseNames;
        }

        public string Name { get; }

        public int Size { get; }

        public string BitFieldType { get; }

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

            var typesToGenerate = new List<TypeInfo>();
            foreach (var type in definitions.Types)
            {
                var typeInfoParts = GetTypeInfoParts(type).ToArray();

                var arrangedParts = ArrangeParts(typeInfoParts).ToArray();
                var totalSize = arrangedParts.Aggregate(0, (size, part) =>
                {
                    var wordOffset = size % 64;
                    if (wordOffset > 0 &&
                        wordOffset + part.Size > 64)
                    {
                        size += 64 - wordOffset;
                    }

                    return size + part.Size;
                });

                totalSize = (64 - (totalSize % 64)) % 64;

                var typeInfo =
                    type.IsUnion
                    ? new TypeInfo(type.Name, totalSize, type.Cases.Select(c => c.Name).ToArray(), arrangedParts)
                    : new TypeInfo(type.Name, totalSize, arrangedParts);

                typesToGenerate.Add(typeInfo);

                knownTypes.Add(typeInfo.Name, new TypeNameAndSize(typeInfo.Name, typeInfo.Size));
            }

            var codeTemplate = new CodeTemplate(typesToGenerate);
            return codeTemplate.TransformText();

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

            IEnumerable<ITypeInfoPart> ArrangeParts(ITypeInfoPart[] parts)
            {
                var classifiedParts = (
                    from part in parts
                    let classification = part.Classify()
                    group part by classification
                ).ToDictionary(g => g.Key, g => g.ToArray());

                if (classifiedParts.TryGetValue(TypeInfoPartClassification.Int, out ITypeInfoPart[] intParts))
                {
                    var orderedSmallInts = intParts.OrderByDescending(i => i.Size).ToArray();
                    var packedSmallInts = Group(orderedSmallInts).OrderBy(g => g.group).Select(g => g.part);
                    foreach (var part in packedSmallInts)
                    {
                        yield return part;
                    }
                }

                if (classifiedParts.TryGetValue(TypeInfoPartClassification.Float, out ITypeInfoPart[] floatParts))
                {
                    foreach (var part in floatParts)
                    {
                        yield return part;
                    }
                }

                if (classifiedParts.TryGetValue(TypeInfoPartClassification.Other, out ITypeInfoPart[] otherParts))
                {
                    foreach (var part in otherParts)
                    {
                        yield return part;
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

        public static TypeInfoPartClassification Classify(this ITypeInfoPart part)
        {
            if (part is UnionHeaderTypeInfoPart)
            {
                return TypeInfoPartClassification.Int;
            }
            else
            {
                var field = part as FieldTypeInfoPart;
                var unionField = part as UnionFieldTypeInfoPart;

                var (name, size) = field != null ? (field.Name, field.Size) : (unionField.Name, unionField.Size);

                if (size < 64 && intrinsicTypes.ContainsKey(name))
                {
                    return name.StartsWith("f") ? TypeInfoPartClassification.Float : TypeInfoPartClassification.Int;
                }
                else
                {
                    return TypeInfoPartClassification.Other;
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
