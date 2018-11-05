using Newtonsoft.Json;

namespace CodeGen
{
    interface ISchemaUnionCase
    {
        SchemaProperty[] Properties { get; }
    }

    class SchemaPropertyUnionCase : ISchemaUnionCase
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

    class SchemaUnionCase : ISchemaUnionCase
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
