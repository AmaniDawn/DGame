using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Editor.Commands.Scripts
{
    /// <summary>
    /// Bridges agent-supplied JSON values and Unity's <see cref="SerializedProperty"/> model for the
    /// set_serialized_field / get_serialized_fields commands (CLI-195).
    ///
    /// Supported property kinds: integers, floats, bools, strings, enums, Vector2/3/4, Color,
    /// Quaternion, Rect, Bounds, object references, arrays and lists, and [Serializable] composites.
    /// Object references accept an <see cref="ObjectRef"/> handle (resolved via <see cref="ObjectResolver"/>)
    /// — assets by GUID/fileId or path, and scene objects by instanceId/hierarchyPath. Arrays are also
    /// addressed element-by-element via a path like "myArray.Array.data[2]" (Unity's native
    /// SerializedProperty path syntax) — see the commands.
    ///
    /// Vector-like values are written from a named object (<c>{"x":1,"y":2}</c>), a positional array
    /// (<c>[1,2]</c>) or a comma-separated string (<c>"1,2"</c>), and read back as a named object.
    /// A composite (a <see cref="SerializedPropertyType.Generic"/> property that is not an array) reads
    /// as a JSON object of its child fields and is written from one; only the keys given change.
    /// Enums are written by name (a [Flags] combination as <c>"A, B"</c> or <c>"A|B"</c>) or by their
    /// underlying value, never by declaration index.
    /// </summary>
    static class SerializedPropertyConverter
    {
        static readonly string[] s_XY = { "x", "y" };
        static readonly string[] s_XYZ = { "x", "y", "z" };
        static readonly string[] s_XYZW = { "x", "y", "z", "w" };
        static readonly string[] s_RGBA = { "r", "g", "b", "a" };
        static readonly string[] s_Rect = { "x", "y", "width", "height" };
        static readonly float[] s_ColorDefaults = { 0f, 0f, 0f, 1f };

        /// <summary>
        /// Assign a JSON value to a serialized property, converting by the property's type. Throws
        /// <see cref="ArgumentException"/> with a clear message when the value can't be coerced or the
        /// property type isn't supported. The caller is responsible for ApplyModifiedProperties.
        /// </summary>
        public static void SetValue(SerializedProperty prop, JToken value)
        {
            // Whole-array assignment: an array/list field (Generic + isArray) set from a JSON array —
            // resize, then recurse per element (handles primitive, object-reference and composite arrays).
            // Element-by-element addressing via "field.Array.data[i]" / "field.Array.size" still works.
            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                if (!(value is JArray array))
                    throw new ArgumentException(
                        $"Field '{prop.propertyPath}' is an array; provide a JSON array of elements.");

                prop.arraySize = array.Count;
                for (int i = 0; i < array.Count; i++)
                    SetValue(prop.GetArrayElementAtIndex(i), array[i]);
                return;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.longValue = value.ToObject<long>();
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = value.ToObject<bool>();
                    break;
                case SerializedPropertyType.Float:
                    prop.doubleValue = value.ToObject<double>();
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = value.ToObject<string>();
                    break;
                case SerializedPropertyType.Enum:
                    SetEnum(prop, value);
                    break;
                case SerializedPropertyType.Vector2:
                {
                    var c = ToFloats(prop, value, s_XY);
                    prop.vector2Value = new Vector2(c[0], c[1]);
                    break;
                }
                case SerializedPropertyType.Vector3:
                    prop.vector3Value = ToVector3(prop, value);
                    break;
                case SerializedPropertyType.Vector4:
                {
                    var c = ToFloats(prop, value, s_XYZW);
                    prop.vector4Value = new Vector4(c[0], c[1], c[2], c[3]);
                    break;
                }
                case SerializedPropertyType.Quaternion:
                {
                    var c = ToFloats(prop, value, s_XYZW);
                    prop.quaternionValue = new Quaternion(c[0], c[1], c[2], c[3]);
                    break;
                }
                case SerializedPropertyType.Color:
                {
                    var c = ToFloats(prop, value, s_RGBA, s_ColorDefaults, required: 3);
                    prop.colorValue = new Color(c[0], c[1], c[2], c[3]);
                    break;
                }
                case SerializedPropertyType.Rect:
                {
                    var c = ToFloats(prop, value, s_Rect);
                    prop.rectValue = new Rect(c[0], c[1], c[2], c[3]);
                    break;
                }
                case SerializedPropertyType.Bounds:
                    prop.boundsValue = ToBounds(prop, value);
                    break;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = ResolveObjectReference(value);
                    break;
                case SerializedPropertyType.ArraySize:
                    // Resizing an array via "<field>.Array.size" — the property is an int under the hood.
                    prop.intValue = value.ToObject<int>();
                    break;
                case SerializedPropertyType.Generic:
                    SetComposite(prop, value);
                    break;
                default:
                    throw new ArgumentException(
                        $"Field '{prop.propertyPath}' has unsupported type '{prop.propertyType}'. " +
                        "Supported: int, float, bool, string, enum, Vector2/3/4, Quaternion, Color, Rect, Bounds, " +
                        "object reference, array, [Serializable] composite, array size.");
            }
        }

        /// <summary>
        /// Read a serialized property into a plain object suitable for JSON serialization. Object
        /// references are described via <see cref="ObjectResolver.Describe"/> so the agent gets a
        /// re-usable handle; arrays are returned as a JSON array and composites as a JSON object of
        /// their fields, so both round-trip with <see cref="SetValue"/>.
        /// </summary>
        public static object GetValue(SerializedProperty prop)
        {
            // String also reports isArray, hence the exclusion.
            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                var elements = new JArray();
                for (int i = 0; i < prop.arraySize; i++)
                    elements.Add(ToToken(GetValue(prop.GetArrayElementAtIndex(i))));

                return elements;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.longValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.doubleValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.Enum:
                    return GetEnum(prop);
                case SerializedPropertyType.Vector2:
                    return new { x = prop.vector2Value.x, y = prop.vector2Value.y };
                case SerializedPropertyType.Vector3:
                    return new { x = prop.vector3Value.x, y = prop.vector3Value.y, z = prop.vector3Value.z };
                case SerializedPropertyType.Vector4:
                    return new { x = prop.vector4Value.x, y = prop.vector4Value.y, z = prop.vector4Value.z, w = prop.vector4Value.w };
                case SerializedPropertyType.Quaternion:
                    return new { x = prop.quaternionValue.x, y = prop.quaternionValue.y, z = prop.quaternionValue.z, w = prop.quaternionValue.w };
                case SerializedPropertyType.Color:
                    return new { r = prop.colorValue.r, g = prop.colorValue.g, b = prop.colorValue.b, a = prop.colorValue.a };
                case SerializedPropertyType.Rect:
                    return new { x = prop.rectValue.x, y = prop.rectValue.y, width = prop.rectValue.width, height = prop.rectValue.height };
                case SerializedPropertyType.Bounds:
                    var b = prop.boundsValue;
                    return new { center = new { x = b.center.x, y = b.center.y, z = b.center.z }, size = new { x = b.size.x, y = b.size.y, z = b.size.z } };
                case SerializedPropertyType.ObjectReference:
                    return ObjectResolver.Describe(prop.objectReferenceValue);
                case SerializedPropertyType.ArraySize:
                    return prop.intValue;
                case SerializedPropertyType.Generic:
                    return GetComposite(prop);
                default:
                    // Unknown/unsupported types are reported by kind rather than failing a whole read.
                    return new { unsupported = prop.propertyType.ToString() };
            }
        }

        private static JToken ToToken(object value) => value == null ? JValue.CreateNull() : JToken.FromObject(value);

        private static void SetComposite(SerializedProperty prop, JToken value)
        {
            if (!(value is JObject obj))
                throw new ArgumentException(
                    $"Field '{prop.propertyPath}' is a {prop.type}; provide a JSON object of its fields " +
                    $"({ChildNames(prop)}), but received a '{value?.Type.ToString() ?? "null"}'.");

            // Find every key before writing any, so a misspelled key leaves the composite untouched.
            var targets = new List<KeyValuePair<SerializedProperty, JToken>>();
            foreach (var p in obj.Properties())
            {
                var child = prop.FindPropertyRelative(p.Name);
                if (child == null)
                    throw new ArgumentException(
                        $"'{p.Name}' is not a field of '{prop.propertyPath}' ({prop.type}). Fields: {ChildNames(prop)}.");
                targets.Add(new KeyValuePair<SerializedProperty, JToken>(child, p.Value));
            }

            foreach (var target in targets)
                SetValue(target.Key, target.Value);
        }

        private static JObject GetComposite(SerializedProperty prop)
        {
            var result = new JObject();
            foreach (var child in Children(prop))
                result[child.name] = ToToken(GetValue(child));

            return result;
        }

        private static string ChildNames(SerializedProperty prop) => string.Join(", ", Children(prop).Select(c => c.name));

        /// <summary>The direct children of a composite, hidden ones included, so a read round-trips through a write.</summary>
        private static IEnumerable<SerializedProperty> Children(SerializedProperty prop)
        {
            var it = prop.Copy();
            var end = prop.GetEndProperty();
            if (!it.Next(true))
                yield break;

            while (!SerializedProperty.EqualContents(it, end))
            {
                yield return it.Copy();
                if (!it.Next(false))
                    yield break;
            }
        }

        private static object GetEnum(SerializedProperty prop)
        {
            if (TryGetEnumType(prop, out var enumType))
            {
                // Enum.ToString gives "A, B" for a [Flags] combination, and the bare number for a value
                // no constant names; report that one as a number.
                var text = Enum.ToObject(enumType, prop.longValue).ToString();
                return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                    ? (object)number
                    : text;
            }

            return prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumNames.Length
                ? prop.enumNames[prop.enumValueIndex]
                : (object)prop.longValue;
        }

        private static void SetEnum(SerializedProperty prop, JToken value)
        {
            // A number is the enum's underlying value, never an index into enumNames: the two only
            // coincide when the constants run 0..n-1 in declaration order, and a [Flags] combination has
            // no index at all.
            if (TryGetEnumType(prop, out var enumType))
            {
                prop.longValue = ParseEnum(prop, enumType, value);
                return;
            }

            // A native enum (on a built-in component) has no managed type to parse against: a name maps
            // through enumNames, a number is written as the underlying value.
            if (value.Type == JTokenType.String)
            {
                var name = value.ToObject<string>();
                var idx = IndexOfEnumName(prop, name);
                if (idx < 0)
                    throw new ArgumentException(
                        $"Enum '{name}' is not a valid value for '{prop.propertyPath}'. Valid: {string.Join(", ", prop.enumNames)}.");
                prop.enumValueIndex = idx;
            }
            else
            {
                // With no managed type there is no list of values to check against, so write the number
                // and see whether Unity maps it to a constant; restore and reject when it does not.
                // Native enums are 32-bit; a wider number would be truncated into a valid-looking one.
                long number;
                try
                {
                    number = (long)ToIntegerInRange(prop, value, typeof(int));
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"{e.Message} Pass the name instead: {string.Join(", ", prop.enumNames)}.");
                }

                var previous = prop.longValue;
                prop.longValue = number;
                if (prop.enumValueIndex < 0)
                {
                    prop.longValue = previous;
                    throw new ArgumentException(
                        $"{number} is not a value of '{prop.propertyPath}'. Pass the name instead: {string.Join(", ", prop.enumNames)}.");
                }
            }
        }

        private static long ParseEnum(SerializedProperty prop, Type enumType, JToken value)
        {
            var isFlags = enumType.IsDefined(typeof(FlagsAttribute), false);
            object parsed;
            if (value.Type == JTokenType.String)
            {
                var text = value.ToObject<string>().Replace('|', ',');
                if (!TryParseEnum(enumType, text, out parsed))
                {
                    // Fall back to the Inspector's display name ("Very Aggressive" for VeryAggressive).
                    var idx = Array.IndexOf(prop.enumDisplayNames, text.Trim());
                    if (idx < 0)
                        throw new ArgumentException(
                            $"Enum '{value}' is not a valid value for '{prop.propertyPath}'. " +
                            $"Valid: {string.Join(", ", Enum.GetNames(enumType))}" +
                            (isFlags ? " (combine with ',' or '|')." : "."));
                    parsed = Enum.Parse(enumType, prop.enumNames[idx]);
                }
            }
            else
            {
                // Range-check before Enum.ToObject, which silently truncates to the underlying type
                // (4294967306 on an int enum would become 10).
                var underlying = Enum.GetUnderlyingType(enumType);
                parsed = Enum.ToObject(enumType,
                    Convert.ChangeType(ToIntegerInRange(prop, value, underlying), underlying, CultureInfo.InvariantCulture));
            }

            var number = Convert.ToInt64(parsed, CultureInfo.InvariantCulture);
            if (isFlags)
            {
                long defined = 0;
                foreach (var v in Enum.GetValues(enumType))
                    defined |= Convert.ToInt64(v, CultureInfo.InvariantCulture);
                if ((number & ~defined) != 0)
                    throw new ArgumentException(
                        $"{value} sets bits no constant of {enumType.Name} defines, for '{prop.propertyPath}'. " +
                        $"Valid: {DescribeConstants(enumType)}.");
            }
            else if (!Enum.IsDefined(enumType, parsed))
            {
                throw new ArgumentException(
                    $"{value} is not a value of {enumType.Name}, for '{prop.propertyPath}'. Valid: {DescribeConstants(enumType)}.");
            }

            return number;
        }

        /// <summary>
        /// Enum.TryParse on a numeric string may throw instead of failing when the number does not fit
        /// the underlying type, depending on the runtime; treat that as "not a value".
        /// </summary>
        private static bool TryParseEnum(Type enumType, string text, out object parsed)
        {
            try
            {
                return Enum.TryParse(enumType, text, false, out parsed);
            }
            catch (OverflowException)
            {
                parsed = null;
                return false;
            }
        }

        /// <summary>
        /// Read a whole number that fits <paramref name="integerType"/>, or throw an ArgumentException.
        /// A ulong is capped at long.MaxValue, the most <see cref="SerializedProperty.longValue"/> holds.
        /// </summary>
        private static decimal ToIntegerInRange(SerializedProperty prop, JToken value, Type integerType)
        {
            decimal number;
            if (value.Type != JTokenType.Integer && value.Type != JTokenType.Float)
                throw new ArgumentException(
                    $"Field '{prop.propertyPath}' expects an enum name or number, but received a '{value.Type}'.");
            try
            {
                number = value.ToObject<decimal>();
            }
            catch (OverflowException)
            {
                number = decimal.MaxValue;
            }

            decimal min, max;
            switch (Type.GetTypeCode(integerType))
            {
                case TypeCode.SByte: min = sbyte.MinValue; max = sbyte.MaxValue; break;
                case TypeCode.Byte: min = byte.MinValue; max = byte.MaxValue; break;
                case TypeCode.Int16: min = short.MinValue; max = short.MaxValue; break;
                case TypeCode.UInt16: min = ushort.MinValue; max = ushort.MaxValue; break;
                case TypeCode.Int32: min = int.MinValue; max = int.MaxValue; break;
                case TypeCode.UInt32: min = uint.MinValue; max = uint.MaxValue; break;
                case TypeCode.UInt64: min = 0; max = long.MaxValue; break;
                default: min = long.MinValue; max = long.MaxValue; break;
            }

            if (number != decimal.Truncate(number) || number < min || number > max)
                throw new ArgumentException(
                    $"{value} is not a whole number between {min} and {max} ({integerType.Name}), for '{prop.propertyPath}'.");

            return number;
        }

        private static string DescribeConstants(Type enumType) =>
            string.Join(", ", Enum.GetValues(enumType).Cast<object>()
                .Select(v => $"{v} = {Convert.ToInt64(v, CultureInfo.InvariantCulture)}"));

        private static int IndexOfEnumName(SerializedProperty prop, string name)
        {
            var idx = Array.IndexOf(prop.enumNames, name);
            return idx >= 0 ? idx : Array.IndexOf(prop.enumDisplayNames, name);
        }

        /// <summary>
        /// The managed enum type behind a property, found by walking its path through the target's
        /// fields. False for a native field, which has no managed declaration to walk.
        /// </summary>
        private static bool TryGetEnumType(SerializedProperty prop, out Type enumType)
        {
            var target = prop.serializedObject.targetObject;
            var type = target != null ? target.GetType() : null;
            var parts = prop.propertyPath.Split('.');
            for (int i = 0; i < parts.Length && type != null; i++)
            {
                if (parts[i] == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data[", StringComparison.Ordinal))
                {
                    type = ElementType(type);
                    i++;
                    continue;
                }

                type = FindField(type, parts[i])?.FieldType;
            }

            enumType = type != null && type.IsEnum ? type : null;
            return enumType != null;
        }

        private static Type ElementType(Type collection)
        {
            if (collection.IsArray)
                return collection.GetElementType();
            if (collection.IsGenericType && collection.GetGenericTypeDefinition() == typeof(List<>))
                return collection.GetGenericArguments()[0];
            return null;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, flags);
                if (field != null)
                    return field;
            }

            return null;
        }

        private static Object ResolveObjectReference(JToken value)
        {
            // An explicit null clears the reference; anything else MUST resolve — never silently no-op
            // (an unresolved handle used to be dropped, so the command reported success while assigning
            // nothing).
            if (value == null || value.Type == JTokenType.Null)
                return null;

            var handle = value.ToObject<ObjectRef>();
            if (handle == null || handle.IsEmpty)
                throw new ArgumentException(
                    $"'{value}' is not a recognized object reference handle. " +
                    "Provide a path, guid, globalId, or instanceId (or null to clear).");

            if (!ObjectResolver.TryResolve(handle, out var obj, out var error))
                throw new ArgumentException($"Could not resolve object reference: {error}");

            return obj;
        }

        /// <summary>
        /// Read the components of a vector-like value from a named object (a missing key takes its
        /// default), a positional array, or a comma-separated string. A positional value may leave out
        /// trailing components from index <paramref name="required"/> on (a Color's alpha), and no others.
        /// </summary>
        private static float[] ToFloats(SerializedProperty prop, JToken value, string[] names, float[] defaults = null, int required = -1)
        {
            if (required < 0)
                required = names.Length;

            var result = new float[names.Length];
            if (defaults != null)
                Array.Copy(defaults, result, names.Length);

            if (value is JObject obj)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    var token = obj[names[i]];
                    if (token != null)
                        result[i] = ToFloat(prop, token, names[i]);
                }

                return result;
            }

            float[] items = null;
            if (value is JArray array)
                items = array.Select((a, i) => ToFloat(prop, a, i < names.Length ? names[i] : $"[{i}]")).ToArray();
            else if (value != null && value.Type == JTokenType.String)
                items = ParseList(prop, value.ToObject<string>(), names);

            if (items == null || items.Length < required || items.Length > names.Length)
            {
                var optional = required < names.Length ? $" ({string.Join(", ", names.Skip(required))} optional)" : "";
                var received = items != null ? $"{items.Length} components" : $"a '{value?.Type.ToString() ?? "null"}'";
                throw new ArgumentException(
                    $"Field '{prop.propertyPath}' expects {string.Join(", ", names)}{optional} as an object " +
                    $"({{ \"{names[0]}\": 0, ... }}), an array [{string.Join(", ", names)}] or a string " +
                    $"\"{string.Join(",", names)}\", but received {received}.");
            }

            Array.Copy(items, result, items.Length);
            return result;
        }

        /// <summary>
        /// A component must be a JSON number. Anything else would make <c>ToObject&lt;float&gt;</c> throw a
        /// FormatException, which the server reports as an internal error rather than a bad argument.
        /// </summary>
        private static float ToFloat(SerializedProperty prop, JToken token, string component)
        {
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return token.ToObject<float>();

            // A number sent as a string ({"x":"1"}) was accepted before; keep accepting it.
            if (token.Type == JTokenType.String
                && float.TryParse(token.ToObject<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            throw new ArgumentException(
                $"Field '{prop.propertyPath}': component '{component}' must be a number, but received {token.ToString(Newtonsoft.Json.Formatting.None)}.");
        }

        private static float[] ParseList(SerializedProperty prop, string text, string[] names)
        {
            var parts = text.Trim().Trim('(', ')', '[', ']').Split(',');
            var result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                    throw new ArgumentException(
                        $"Field '{prop.propertyPath}': '{text}' is not a list of numbers; expected \"{string.Join(",", names)}\".");
            }

            return result;
        }

        private static Vector3 ToVector3(SerializedProperty prop, JToken value)
        {
            var c = ToFloats(prop, value, s_XYZ);
            return new Vector3(c[0], c[1], c[2]);
        }

        private static Bounds ToBounds(SerializedProperty prop, JToken value)
        {
            if (!(value is JObject obj))
                throw new ArgumentException(
                    $"Field '{prop.propertyPath}' expects a JSON object with 'center' and 'size' components but received a " +
                    $"'{value?.Type.ToString() ?? "null"}'.");

            var center = obj["center"] != null ? ToVector3(prop, obj["center"]) : Vector3.zero;
            var size = obj["size"] != null ? ToVector3(prop, obj["size"]) : Vector3.zero;
            return new Bounds(center, size);
        }
    }
}
