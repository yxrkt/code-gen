using System.Linq;
using Newtonsoft.Json.Linq;

namespace CodeGen
{
    internal abstract class CppPropertyBase
    {
        public static CppPropertyBase Create(JToken property)
        {
            if (property.SelectToken("name") is JToken name)
            {
                return new CppProperty(name.ToString(), property["type"].ToString());
            }
            else
            {
                return new CppPropertyUnion(property["cases"]);
            }
        }
    }

    internal class CppProperty : CppPropertyBase
    {
        public CppProperty(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public string Type { get; }
    }

    internal class CppPropertyUnionCaseType
    {
        public CppPropertyUnionCaseType(JToken properties)
        {
            Properties = properties.Select(p => CppPropertyBase.Create(p)).ToArray();
        }

        public CppPropertyBase[] Properties { get; }
    }

    internal class CppPropertyUnion : CppPropertyBase
    {
        public CppPropertyUnionCaseType[] Cases { get; }

        public CppPropertyUnion(JToken cases)
        {
            Cases = cases.Select(c => new CppPropertyUnionCaseType(cases)).ToArray();
        }
    }

    internal class CppType
    {
        public string Name { get; }
        public CppPropertyBase[] Properties { get; }
        public string[] Cases { get; }

        public CppType(JToken type)
        {
            Name = type["name"].ToString();

            if (type.SelectToken("properties") is JToken properties)
            {
                Properties = properties.Select(p => CppPropertyBase.Create(p)).ToArray();
            }
            else
            {
                Cases = type["cases"].Select(c => c["name"].ToString()).ToArray();
            }
        }
    }

    internal static class CodeGenerator
    {
        public static string Generate(JObject input)
        {
            var types = input["types"].Select(t => new CppType(t)).ToArray();

            var template = new CodeTemplate(types);
            return template.TransformText();
        }
    }
}
