using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace CodeGen
{
    [DebuggerDisplay("Name = {Name}, Alignment = {Alignment}, Bits = {Bits}")]
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

        // Byte alignment for the type. When 0, indicates that the field can be packed in a bit field.
        public int Alignment { get; }

        // Size of the type, in bits.
        public int Bits { get; }
    }

    interface ICppPart
    {
    }

    class CppPropertyPart : ICppPart
    {
        public CppPropertyPart(CppTypeInfo type, string name)
        {
            Type = type;
            Name = name;
        }

        public CppTypeInfo Type { get; }
        public string Name { get; }
    }

    class CppUnionHeaderPart : ICppPart
    {
        public CppUnionHeaderPart(int iD, int bits)
        {
            ID = iD;
            Bits = bits;
        }

        public int ID { get; }
        public int Bits { get; }
    }

    class CppUnionCase
    {
        public CppUnionCase(ICppPart[] parts)
        {
            Parts = parts;
        }

        public ICppPart[] Parts { get; }
    }

    class CppUnionBodyPart : ICppPart
    {
        public CppUnionBodyPart(int iD, CppUnionCase[] cases)
        {
            ID = iD;
            Cases = cases;
        }

        public int ID { get; }
        public CppUnionCase[] Cases { get; }
    }

    class CppBitFieldPart : ICppPart
    {
        public CppBitFieldPart(CppTypeInfo type, ICppPart[] parts)
        {
            Type = type;
            Parts = parts;
        }

        public CppTypeInfo Type { get; }
        public ICppPart[] Parts { get; }
    }

    class CppPropertyCondition
    {
        public CppPropertyCondition(int unionID, int unionCase)
        {
            UnionID = unionID;
            UnionCase = unionCase;
        }

        public int UnionID { get; }
        public int UnionCase { get; }
    }

    class CppProperty
    {
        public CppProperty(CppTypeInfo type, string name, params CppPropertyCondition[] conditions)
        {
            Type = type;
            Name = name;
            Conditions = conditions;
        }

        public CppTypeInfo Type { get; }
        public string Name { get; }
        public CppPropertyCondition[] Conditions { get; }
    }

    interface ICppType
    {
        CppTypeInfo TypeInfo { get; }
    }

    class CppClass : ICppType
    {
        public CppClass(CppTypeInfo typeInfo, ICppPart[] parts, CppProperty[] properties)
        {
            TypeInfo = typeInfo;
            Parts = parts;
            Properties = properties;
        }

        public CppTypeInfo TypeInfo { get; }
        public ICppPart[] Parts { get; }
        public CppProperty[] Properties { get; }
    }

    class CppEnum : ICppType
    {
        public CppEnum(CppTypeInfo typeInfo, string[] cases)
        {
            TypeInfo = typeInfo;
            Cases = cases;
        }

        public CppTypeInfo TypeInfo { get; }
        public CppTypeInfo UnderlyingType { get; }
        public string[] Cases { get; }
    }

    static class CodeGenerator
    {
        private const int AlignmentBits = 64;
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
                var topLevelParts = GenerateParts(schemaType);

                var properties = new List<CppProperty>();

                GenerateProperties(topLevelParts);

                var (finalTopLevelParts, topLevelType) = OptimizePartLayout(topLevelParts);

                var namedType = new CppTypeInfo(
                    name: schemaType.Name,
                    alignment: Math.Max(topLevelType.Alignment, schemaType.Alignment),
                    bits: topLevelType.Bits);
                discoveredTypes.Add(namedType.Name, namedType);
                return new CppClass(namedType, finalTopLevelParts, properties.ToArray());

                void GenerateProperties(ICppPart[] parts, params CppPropertyCondition[] conditions)
                {
                    foreach (var part in parts)
                    {
                        switch (part)
                        {
                            case CppPropertyPart property:
                                properties.Add(new CppProperty(property.Type, property.Name, conditions));
                                break;
                            case CppUnionHeaderPart unionHeader:
                                var headerFieldType = new CppTypeInfo("", 0, unionHeader.Bits);
                                break;
                            case CppUnionBodyPart unionBody:
                                foreach (var (unionCase, i) in unionBody.Cases.Select((c, i) => (c, i)))
                                {
                                    var caseCondition = new CppPropertyCondition(unionBody.ID, i);
                                    GenerateProperties(unionCase.Parts, conditions.Concat(new[] { caseCondition }).ToArray());
                                }

                                break;
                        }
                    }
                }

                (ICppPart[] parts, CppTypeInfo type) OptimizePartLayout(ICppPart[] unoptimizedParts)
                {
                    var unoptimizedPartsAndTypes = GetPartsAndTypes(unoptimizedParts);
                    return ArrangeParts(unoptimizedPartsAndTypes);

                    (ICppPart part, CppTypeInfo type)[] GetPartsAndTypes(ICppPart[] parts)
                    {
                        return parts.Select<ICppPart, ValueTuple<ICppPart, CppTypeInfo>>(part =>
                        {
                            switch (part)
                            {
                                case CppPropertyPart property:
                                    return (property, property.Type);
                                case CppUnionHeaderPart unionHeader:
                                    return (unionHeader, new CppTypeInfo("", 0, unionHeader.Bits));
                                case CppUnionBodyPart unionBody:
                                    var optimizedCases = new List<CppUnionCase>();
                                    var unionBodySize = 8;
                                    var unionBodyAlignment = 1;
                                    foreach (var unionCase in unionBody.Cases)
                                    {
                                        (ICppPart[] caseParts, CppTypeInfo caseType) = OptimizePartLayout(unionCase.Parts);
                                        unionBodySize = Math.Max(caseType.Bits, unionBodySize);
                                        unionBodyAlignment = Math.Max(caseType.Alignment, unionBodyAlignment);
                                        optimizedCases.Add(new CppUnionCase(caseParts));
                                    }

                                    var unionBodyType = new CppTypeInfo("", unionBodyAlignment, unionBodySize);
                                    return (new CppUnionBodyPart(unionBody.ID, optimizedCases.ToArray()), unionBodyType);
                            }

                            throw new ArgumentNullException(nameof(part));
                        }).ToArray();
                    }

                    (ICppPart[] parts, CppTypeInfo type) ArrangeParts((ICppPart part, CppTypeInfo type)[] typesAndParts)
                    {
                        var bitParts = new List<(ICppPart part, CppTypeInfo type)>();
                        var smallParts = new List<(ICppPart part, CppTypeInfo type)>();
                        var largeParts = new List<(ICppPart part, CppTypeInfo type)>();

                        foreach (var typeAndPart in typesAndParts)
                        {
                            if (typeAndPart.type.Alignment == 0)
                            {
                                bitParts.Add(typeAndPart);
                            }
                            else if (typeAndPart.type.Bits < AlignmentBits)
                            {
                                smallParts.Add(typeAndPart);
                            }
                            else
                            {
                                largeParts.Add(typeAndPart);
                            }
                        }

                        var bitFieldBins = bitParts.BinPack(MaxBitFieldBits, typeAndPart => typeAndPart.type.Bits);
                        var bitFieldParts = (
                            from bin in bitFieldBins
                            let bitFieldSize = new[] { 8, 16, 32, 64 }.First(size => bin.size <= size)
                            let bitFieldType = GetTypeInfo($"u{bitFieldSize}")
                            let parts = bin.items.Select(pair => pair.part).ToArray()
                            select (part: (ICppPart)new CppBitFieldPart(bitFieldType, parts), type: bitFieldType)
                        ).ToArray();

                        var packedSmallParts =
                            bitFieldParts.Concat(smallParts).BinPack(
                                binSize: AlignmentBits,
                                sizeSelector: pair => pair.type.Bits,
                                sumFunc: (binSize, pair) => Utilities.AddWithAlignment(binSize, pair.type.Bits, pair.type.Alignment));

                        var packedLargeParts = largeParts.OrderBy(pair => pair.type.Alignment).ToArray();

                        var (finalParts, finalTypeSize, finalTypeAlignment) =
                            packedSmallParts.SelectMany(bin => bin.items).Concat(packedLargeParts).Aggregate(
                                seed: (parts: new List<ICppPart>(), typeSize: 0, typeAlignment: 0),
                                func: (aggregate, pair) =>
                                {
                                    aggregate.parts.Add(pair.part);
                                    aggregate.typeSize = Utilities.AddWithAlignment(
                                        blockBits: aggregate.typeSize,
                                        itemBits: pair.type.Bits,
                                        itemByteAlignment: pair.type.Alignment);
                                    aggregate.typeAlignment = Math.Max(pair.type.Alignment, aggregate.typeAlignment);
                                    return aggregate;
                                });

                        return (parts: finalParts.ToArray(), new CppTypeInfo("", finalTypeAlignment, finalTypeSize));
                    }
                }
            }

            ICppPart[] GenerateParts(SchemaType schemaType)
            {
                var nextUnionID = 0;

                var cppParts = new List<ICppPart>();

                if (schemaType.IsUnion)
                {
                    throw new NotImplementedException(); // TODO: Union type
                }
                else
                {
                    foreach (var property in schemaType.Properties)
                    {
                        if (property.IsUnion)
                        {
                            cppParts.Add(GenerateUnionParts(property.Cases));
                        }
                        else
                        {
                            cppParts.Add(new CppPropertyPart(GetTypeInfo(property.Type), property.Name));
                        }
                    } 
                }

                return cppParts.ToArray();

                CppUnionBodyPart GenerateUnionParts(SchemaPropertyUnionCase[] schemaCases)
                {
                    var unionID = nextUnionID++;

                    cppParts.Add(new CppUnionHeaderPart(unionID, Utilities.GetMinimumBitsForInt(schemaCases.Length)));

                    var unionCases = new List<CppUnionCase>();
                    foreach (var schemaCase in schemaCases)
                    {
                        var unionCaseParts = new List<ICppPart>();
                        foreach (var property in schemaCase.Properties)
                        {
                            if (property.IsUnion)
                            {
                                unionCaseParts.Add(GenerateUnionParts(property.Cases));
                            }
                            else
                            {
                                unionCaseParts.Add(new CppPropertyPart(GetTypeInfo(property.Type), property.Name));
                            }
                        }

                        unionCases.Add(new CppUnionCase(unionCaseParts.ToArray()));
                    }

                    return new CppUnionBodyPart(unionID, unionCases.ToArray());
                }
            }

            CppTypeInfo GetTypeInfo(string typeName)
            {
                if (discoveredTypes.TryGetValue(typeName, out CppTypeInfo typeInfo))
                {
                    return typeInfo;
                }
                else
                {
                    throw new Exception($"Type '{typeName}' has not yet been defined.");
                }
            }
        }
    }
}
