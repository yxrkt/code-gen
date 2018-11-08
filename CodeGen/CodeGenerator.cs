using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGen
{
    class CppTypeInfo
    {
        public CppTypeInfo(string name, int alignment, int bits)
        {
            Name = name;
            Alignment = alignment;
            Bits = bits;
        }

        // Name used for method and field generation. When empty, indicates that we're an anonymous union or union header type.
        public string Name { get; }

        // Byte alignment for the type. When 0, indicates that the field will be packed in a bit field.
        public int Alignment { get; }

        // Size of the type, in bits.
        public int Bits { get; }
    }

    class CppPart
    {
        public CppPart(string name, CppTypeInfo type, int unionID, int unionCaseID, params CppPart[] children)
        {
            Name = name;
            Type = type;
            UnionID = unionID;
            UnionCaseID = unionCaseID;
            Children = children;
        }

        // Name used for method and field generation.
        public string Name { get; }

        public CppTypeInfo Type { get; }

        // For union and union header parts, identifies the union. Otherwise, -1.
        public int UnionID { get; }

        // For union fields, identifies the case where the field is valid.
        public int UnionCaseID { get; }

        // Cases for unions
        public CppPart[] Children { get; }
    }

    class CppField
    {
        public CppField(string type, string name)
        {
            Type = type;
            Name = name;
        }

        public string Type { get; }
        public string Name { get; }
    }

    class CppProperty
    {
        public CppProperty(CppField[] unionStateFields, int[] unionStates, CppField value)
        {
            UnionStateFields = unionStateFields;
            UnionStates = unionStates;
            Value = value;
        }

        public CppField[] UnionStateFields { get; }
        public int[] UnionStates { get; }
        public CppField Value { get; }
    }

    interface ICppType
    {
        CppTypeInfo TypeInfo { get; }
    }

    class CppClass : ICppType
    {
        public CppClass(CppTypeInfo typeInfo, CppField[] fields, CppProperty[] properties)
        {
            TypeInfo = typeInfo;
            Fields = fields;
            Properties = properties;
        }

        public CppTypeInfo TypeInfo { get; }
        public CppField[] Fields { get; }
        public CppProperty[] Properties { get; }
    }

    static class CodeGenerator
    {
        private const int MaxBitFieldBits = 64;

        private static readonly CppTypeInfo[] intrinsicTypes =
        {
            new CppTypeInfo(name: "bool", alignment: 0, bits: 1),
            new CppTypeInfo(name: "s8", alignment: 1, bits: 8),
            new CppTypeInfo(name: "u8", alignment: 1, bits: 8),
            new CppTypeInfo(name: "s16", alignment: 2, bits: 16),
            new CppTypeInfo(name: "u16", alignment: 2, bits: 16),
            new CppTypeInfo(name: "s32", alignment: 4, bits: 32),
            new CppTypeInfo(name: "u32", alignment: 4, bits: 32),
            new CppTypeInfo(name: "s64", alignment: 8, bits: 64),
            new CppTypeInfo(name: "u64", alignment: 8, bits: 64),
            new CppTypeInfo(name: "f32", alignment: 4, bits: 32),
            new CppTypeInfo(name: "f64", alignment: 8, bits: 64),
        };

        public static string GenerateCode(string json)
        {
            var discoveredTypes = intrinsicTypes.ToDictionary(type => type.Name, type => type);

            var schemaDefinitions = JsonConvert.DeserializeObject<SchemaTypeDefinitions>(json);

            var cppTypes = schemaDefinitions.Types.Select(t => GenerateType(t));
            var template = new CodeTemplate(cppTypes);
            return template.TransformText();

            ICppType GenerateType(SchemaType schemaType)
            {
                var nextUnionID = 0;

                // 1. Generate parts
                var parts = GenerateParts(schemaType).ToArray();

                // 2. Bin-pack 0-alignment parts into bigger bit-field parts
                var bitFieldParts = BinPackBitFields(parts.Where(part => part.Type.Alignment == 0));
                parts = BinPackBitFields(parts).ToArray();

                // 3. Minimize size of last bit-field part

                // 4. Bin-pack bit-field and small field parts

                // 5. Sort large field parts by alignment, ascending

                // 6. Generate fields and properties

                // 7. Sort properties based on schema

                return null;
            }

            IEnumerable<CppPart> GenerateParts(SchemaType type)
            {
                if (type.IsUnion)
                {

                }
                else
                {
                    foreach (var property in type.Properties)
                    {
                        if (property.IsUnion)
                        {
                            var headers = new List<CppPart>();
                            var bodies = new List<CppPart>();
                            foreach (var part in GenerateUnionPropertyParts(property))
                            {
                                yield return part;
                            }
                        }
                        else
                        {
                            yield return new CppPart(property.Name, LookupType(property.Type), -1, -1);
                        }
                    }
                }

                IEnumerable<CppPart> GenerateUnionPropertyParts(SchemaProperty unionProperty, int unionCaseID = -1)
                {
                    var unionID = nextUnionID++;

                    var headerType = new CppTypeInfo("", 0, Utilities.GetMinimumBitsForInt(unionProperty.Cases.Length));
                    yield return new CppPart($"Union{unionID}State", headerType, unionID, unionCaseID);

                    var cases = new List<CppPart>();

                    var caseID = 0;
                    foreach (var @case in unionProperty.Cases)
                    {
                        var caseProperties = new List<CppPart>();
                        foreach (var property in @case.Properties)
                        {
                            if (property.IsUnion)
                            {
                                var childParts = GenerateUnionPropertyParts(property, caseID).Reverse().ToArray();

                                var childUnionPart = childParts.First();
                                caseProperties.Add(childUnionPart);

                                foreach (var descendantHeader in childParts.Skip(1))
                                {
                                    yield return descendantHeader;
                                }
                            }
                            else
                            {
                                caseProperties.Add(new CppPart(property.Name, LookupType(property.Type), -1, caseID));
                            }
                        }

                        var caseType = new CppTypeInfo(
                            name: "",
                            alignment: caseProperties.Max(part => part.Type.Alignment),
                            bits: caseProperties.Max(part => part.Type.Bits));
                        cases.Add(new CppPart("", caseType, unionID, unionCaseID, caseChildren.ToArray()));

                        caseID++;
                    }

                    var unionType = new CppTypeInfo("", cases.Max(c => c.Type.Alignment), cases.Max(c => c.Type.Bits));
                    yield return new CppPart("", unionType, )
            }
            }

            CppPart[] BinPackBitFields(IEnumerable<CppPart> parts)
            {
                var orderedParts = parts.OrderByDescending(part => part.Type.Bits).ToArray();

                var bins = new List<List<CppPart>>();
                foreach (var part in orderedParts)
                {
                    if (part.Type.Bits >= MaxBitFieldBits)
                    {
                        throw new Exception($"{part.Name}: Exceeded {MaxBitFieldBits} bits");
                    }

                    if (bins.Find(b => b.Sum(p => p.Type.Bits) + part.Type.Bits <= MaxBitFieldBits) is List<CppPart> bin)
                    {
                        bin.Add(part);
                    }
                    else
                    {
                        bins.Add(new List<CppPart>() { part });
                    }
                }
            }

            CppTypeInfo LookupType(string name)
            {
                if (discoveredTypes.TryGetValue(name, out CppTypeInfo partType))
                {
                    return partType;
                }
                else
                {
                    throw new Exception($"Unknown type '{name}'");
                }
            }
        }
    }
}
