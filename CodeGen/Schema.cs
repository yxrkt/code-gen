using Newtonsoft.Json;
using System.Linq;

namespace CodeGen
{
    class SchemaPropertyUnionCase
    {
        public SchemaProperty[] Properties { get; set; }
    }

    class SchemaProperty
    {
        [JsonIgnore]
        public bool IsUnion => Cases != null;

        // Leaf
        public string Name { get; set; }
        public string Type { get; set; }

        // Union
        public SchemaPropertyUnionCase[] Cases { get; set; }
    }

    class SchemaUnionCase
    {
        public string Name { get; set; }
        public SchemaProperty[] Properties { get; set; }
    }

    class SchemaType
    {
        [JsonIgnore]
        public bool IsUnion => Cases != null;

        public string Name { get; set; }

        // Record
        public SchemaProperty[] Properties { get; set; }

        // Union
        public SchemaUnionCase[] Cases { get; set; }
    }

    class SchemaTypeDefinitions
    {
        public SchemaType[] Types { get; set; }
    }
}
