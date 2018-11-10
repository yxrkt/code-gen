using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeGen
{
    partial class CodeTemplate
    {
        const string indentation = "    ";

        internal CodeTemplate(IEnumerable<ICppType> types)
        {
            Types = types.ToArray();
        }

        private IEnumerable<ICppType> Types { get; }

        private string GetAvailabilityCondition(CppProperty property)
        {
            var conditions =
                from condition in property.Conditions
                select $"m_union{condition.UnionID}State == {condition.UnionCase}";

            return string.Join(" && ", conditions.ToArray());
        }

        private IEnumerable<string> GetAvailablilityConditionUpdates(CppProperty property)
        {
            foreach (var condition in property.Conditions)
            {
                yield return $"m_union{condition.UnionID}State = {condition.UnionCase};";
            }
        }

        private IEnumerable<string> GetPartLines(ICppPart part)
        {
            switch (part)
            {
                case CppPropertyPart property:
                    yield return $"{property.Type.Name} {GetFieldName(property.Name)};";
                    break;
                case CppUnionBodyPart unionBody:
                    yield return $"union";
                    yield return "{";
                    PushIndent(indentation);

                    foreach (var unionCase in unionBody.Cases)
                    {
                        if (unionCase.Parts.Length == 1)
                        {
                            foreach (var partLine in GetPartLines(unionCase.Parts[0]))
                            {
                                yield return partLine;
                            }
                        }
                        else
                        {
                            yield return "struct";
                            yield return "{";
                            PushIndent(indentation);

                            foreach (var casePart in unionCase.Parts)
                            {
                                foreach (var partLine in GetPartLines(casePart))
                                {
                                    yield return partLine;
                                }
                            }

                            PopIndent();
                            yield return "};";
                        }
                    }

                    PopIndent();
                    yield return "};";

                    break;
                case CppBitFieldPart bitField:
                    foreach (var bitFieldPart in bitField.Parts)
                    {
                        switch (bitFieldPart)
                        {
                            case CppPropertyPart bitFieldProperty:
                                yield return $"{bitField.Type.Name} {GetFieldName(bitFieldProperty.Name)} : {bitFieldProperty.Type.Bits};";
                                break;
                            case CppUnionHeaderPart unionHeader:
                                yield return $"{bitField.Type.Name} m_union{unionHeader.ID}State : {unionHeader.Bits};";
                                break;
                            default:
                                throw new InvalidOperationException($"A bit field cannot contain parts of type '{bitFieldPart.GetType()}'");
                        }
                    }

                    break;
            }
        }

        private string GetFieldName(string propertyName)
        {
            var regex = new Regex(@"^[A-Z]*");
            return regex.Replace(propertyName, match => $"m_{match.Value.ToLower()}");
        }

        private string GetReturnType(CppTypeInfo type)
        {
            return type.Bits <= 64
                ? type.Name
                : $"{type.Name}&";
        }

        private string GetConstReturnType(CppTypeInfo type)
        {
            return type.Bits <= 64
                ? type.Name
                : $"const {type.Name}&";
        }

        private string GetParameterType(CppTypeInfo type)
        {
            return type.Bits <= 64
                ? type.Name
                : $"const {type.Name}&";
        }
    }
}