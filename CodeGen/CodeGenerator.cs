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
    class PartType
    {
        public PartType(string name, int alignment, int bits)
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

    class Part
    {
        public Part(string name, PartType type, int unionID, int unionCaseID)
        {
            Name = name;
            Type = type;
            UnionID = unionID;
            UnionCaseID = unionCaseID;
        }

        // Name used for method and field generation.
        public string Name { get; }

        public PartType Type { get; }

        // For union and union header parts, identifies the union. Otherwise, -1.
        public int UnionID { get; }

        // For union fields, identifies the case where the field is valid.
        public int UnionCaseID { get; }

        // Cases for unions
        public Part[] Children { get; }
    }

    class Field
    {
        public Field(string type, string name)
        {
            Type = type;
            Name = name;
        }

        public string Type { get; }
        public string Name { get; }
    }

    class Property
    {
        public Property(Field[] unionStateFields, int[] unionStates, Field value)
        {
            UnionStateFields = unionStateFields;
            UnionStates = unionStates;
            Value = value;
        }

        public Field[] UnionStateFields { get; }
        public int[] UnionStates { get; }
        public Field Value { get; }
    }

    static class CodeGenerator
    {
        private static readonly PartType[] intrinsicTypes =
        {
            new PartType(name: "bool", alignment: 0, bits: 1),
            new PartType(name: "s8", alignment: 1, bits: 8),
            new PartType(name: "u8", alignment: 1, bits: 8),
            new PartType(name: "s16", alignment: 2, bits: 16),
            new PartType(name: "u16", alignment: 2, bits: 16),
            new PartType(name: "s32", alignment: 4, bits: 32),
            new PartType(name: "u32", alignment: 4, bits: 32),
            new PartType(name: "s64", alignment: 8, bits: 64),
            new PartType(name: "u64", alignment: 8, bits: 64),
            new PartType(name: "f32", alignment: 4, bits: 32),
            new PartType(name: "f64", alignment: 8, bits: 64),
        };

        public static string GenerateCode(string json)
        {
            var schemaDefinitions = JsonConvert.DeserializeObject<SchemaTypeDefinitions>(json);

            // 1. Generate parts
            // 2. Bin-pack 0-alignment parts into bigger bit-field parts
            // 3. Minimize size of last bit-field part
            // 4. Bin-pack bit-field and small field parts
            // 5. Sort large field parts by alignment, ascending
            // 6. Generate fields and properties
            // 7. Sort properties based on schema

            return "";
        }
    }
}
