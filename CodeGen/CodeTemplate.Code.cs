using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGen
{
    partial class CodeTemplate
    {
        private readonly Dictionary<string, int> typeSizesInBits = new Dictionary<string, int>
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
            { "f32", 32 },
            { "f64", 64 },
            { "f64", 64 },
        };

        internal CodeTemplate(CppType[] types)
        {
            Types = types;

            foreach (var type in types)
            {
                int typeSizeInBits = 0;

                foreach (var property in type.Properties)
                {
                }
            }
        }

        private CppType[] Types { get; }

        internal bool IsEnum(CppType type) => type.Cases?.Any() ?? false;

        private IEnumerable<ICppClassField> GetFields(CppType type)
        {
            int unionIndex = 0;
            foreach (var property in type.Properties)
            {
                if (property is CppProperty simpleProperty)
                {
                    yield return new CppClassField(simpleProperty, typeSizesInBits);
                }
                else if (property is CppPropertyUnion unionProperty)
                {
                    yield return new CppClassFieldUnionHeader(unionProperty.Cases.Length);
                }
            }
        }
    }

    internal interface ICppClassField
    {
        int SizeInBits { get; }
    }

    internal class CppClassField : ICppClassField
    {
        public CppClassField(CppProperty property, Dictionary<string, int> typeSizesInBits)
        {
            if (!typeSizesInBits.ContainsKey(property.Type))
            {
                throw new Exception($"Undiscovered type '{property.Type}' encountered.");
            }

            SizeInBits = typeSizesInBits[property.Type];

            Type = property.Type;
            Name = property.Name;
        }

        public string Type { get; }
        public string Name { get; }
        public int SizeInBits { get; }
    }

    internal class CppClassFieldUnionHeader : ICppClassField
    {
        public CppClassFieldUnionHeader(int casesCount)
        {
            SizeInBits = BitUtility.GetMinimumBitsForInt(casesCount);
        }

        public int SizeInBits { get; }
    }

    internal class CppClassFieldUnionBody : ICppClassField
    {
        public CppClassFieldUnionBody(CppPropertyUnion property, Dictionary<string, int> typeSizesInBits)
        {
        }

        public int SizeInBits => throw new NotImplementedException();
        public int MaxFieldSizeInBits { get; }
    }

    internal static class CppTypeExtensions
    {
        public static bool IsEnum(this CppType type)
        {
            return type.Cases?.Any() ?? false;
        }

        public static IEnumerable<CppClassField> GetFields(this CppType type)
        {
            var fields = new List<CppClassField>();
            foreach (var property in type.Properties)
            {
                if (property is CppProperty simpleProperty)
                {
                    simpleProperty
                }
            }
        }
        // TODO: GetFields(), GetMethods()
    }

    internal static class BitUtility
    {
        public static uint NextPowerOfTwo(uint v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;

            return v;
        }

        public static uint CountBitsSet(uint v)
        {
            v = v - ((v >> 1) & 0x55555555);
            v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
            return (((v + (v >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public static int GetMinimumBitsForInt(int n)
        {
            if (n <= 2)
            {
                return 1;
            }

            var nextPowerOfTwo = NextPowerOfTwo((uint)(n - 2));
            return (int)CountBitsSet(nextPowerOfTwo - 1);
        }
    }
}
