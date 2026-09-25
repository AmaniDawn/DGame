using System;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Editor.Commands.GameObjects
{
    /// <summary>
    /// Reads and writes a single <see cref="SerializedProperty"/> from/to a JSON value.
    ///
    /// WHY everything goes through SerializedProperty rather than reflection on the C# field: it is
    /// the only path that correctly participates in dirtying, prefab override tracking, and Undo
    /// (when the change is applied via <see cref="SerializedObject.ApplyModifiedProperties"/>).
    /// Reflection would bypass all three and leave the Editor's view stale.
    ///
    /// Supported surface: primitives (bool/int/long/float/double/string), enums (by name or index),
    /// <see cref="Vector2"/>/<see cref="Vector3"/>/<see cref="Vector4"/>, <see cref="Color"/>, and
    /// object references (assigned from an <see cref="ObjectRef"/> handle resolved by
    /// <see cref="ObjectResolver"/>).
    /// </summary>
    internal static class SerializedPropertyConverter
    {
        /// <summary>
        /// Read a property into a JSON-friendly value (primitive, string, or small array/object for
        /// vector/color types). Unsupported property kinds return a short type descriptor string so a
        /// get_component_properties dump never throws on an exotic field.
        /// </summary>
        public static JToken Read(SerializedProperty property)
        {
            // Array/list fields surface as Generic with isArray=true; return their elements as a JSON
            // array so a get/set round-trips. (String also reports isArray, hence the exclusion.)
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                var elements = new JArray();
                for (int i = 0; i < property.arraySize; i++)
                    elements.Add(Read(property.GetArrayElementAtIndex(i)));
                return elements;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return new JValue(property.boolValue);
                case SerializedPropertyType.Integer:
                    return new JValue(property.longValue);
                case SerializedPropertyType.Float:
                    return new JValue(property.doubleValue);
                case SerializedPropertyType.String:
                    return new JValue(property.stringValue);
                case SerializedPropertyType.Enum:
                    // enumValueIndex indexes enumNames; guard against -1 (mixed/unknown).
                    return property.enumValueIndex >= 0 && property.enumValueIndex < property.enumNames.Length
                        ? new JValue(property.enumNames[property.enumValueIndex])
                        : new JValue(property.intValue);
                case SerializedPropertyType.Vector2:
                    return ToArray(property.vector2Value.x, property.vector2Value.y);
                case SerializedPropertyType.Vector3:
                    return ToArray(property.vector3Value.x, property.vector3Value.y, property.vector3Value.z);
                case SerializedPropertyType.Vector4:
                    return ToArray(property.vector4Value.x, property.vector4Value.y, property.vector4Value.z, property.vector4Value.w);
                case SerializedPropertyType.Color:
                    var c = property.colorValue;
                    return ToArray(c.r, c.g, c.b, c.a);
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null
                        ? JToken.FromObject(ObjectResolver.Describe(property.objectReferenceValue))
                        : JValue.CreateNull();
                default:
                    // Bounds, Quaternion, AnimationCurve, etc. are out of MVP scope: surface a hint.
                    return new JValue($"<unsupported:{property.propertyType}>");
            }
        }

        /// <summary>
        /// Write a JSON value into a property. Throws <see cref="ArgumentException"/> with a clear
        /// message on a type mismatch so set_component_properties reports exactly which field/value
        /// failed. Object references are resolved from an <see cref="ObjectRef"/>-shaped JSON object
        /// (or a bare string treated as an asset path / globalId).
        /// </summary>
        public static void Write(SerializedProperty property, JToken value)
        {
            // Array/list fields (Generic + isArray) are written from a JSON array: resize, then recurse
            // per element so arrays of primitives AND object references (e.g. MeshRenderer.m_Materials)
            // both work. (String also reports isArray, hence the exclusion.)
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                if (!(value is JArray array))
                    throw new ArgumentException(
                        $"Property '{property.name}' is an array; provide a JSON array of elements.");

                property.arraySize = array.Count;
                for (int i = 0; i < array.Count; i++)
                    Write(property.GetArrayElementAtIndex(i), array[i]);
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    property.boolValue = value.ToObject<bool>();
                    break;
                case SerializedPropertyType.Integer:
                    property.longValue = value.ToObject<long>();
                    break;
                case SerializedPropertyType.Float:
                    property.doubleValue = value.ToObject<double>();
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = value.Type == JTokenType.Null ? string.Empty : value.ToObject<string>();
                    break;
                case SerializedPropertyType.Enum:
                    WriteEnum(property, value);
                    break;
                case SerializedPropertyType.Vector2:
                {
                    var a = ToFloats(value, 2, property.name);
                    property.vector2Value = new Vector2(a[0], a[1]);
                    break;
                }
                case SerializedPropertyType.Vector3:
                {
                    var a = ToFloats(value, 3, property.name);
                    property.vector3Value = new Vector3(a[0], a[1], a[2]);
                    break;
                }
                case SerializedPropertyType.Vector4:
                {
                    var a = ToFloats(value, 4, property.name);
                    property.vector4Value = new Vector4(a[0], a[1], a[2], a[3]);
                    break;
                }
                case SerializedPropertyType.Color:
                {
                    var a = ToFloats(value, 4, property.name);
                    property.colorValue = new Color(a[0], a[1], a[2], a[3]);
                    break;
                }
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = ResolveObjectReference(value, property.name);
                    break;
                default:
                    throw new ArgumentException(
                        $"Property '{property.name}' has unsupported type '{property.propertyType}' for set.");
            }
        }

        private static void WriteEnum(SerializedProperty property, JToken value)
        {
            // Accept either the enum constant name or its index/value.
            if (value.Type == JTokenType.String)
            {
                var name = value.ToObject<string>();
                var index = Array.IndexOf(property.enumNames, name);
                if (index < 0)
                {
                    // Fall back to display names (Editor humanized form), then error.
                    index = Array.IndexOf(property.enumDisplayNames, name);
                    if (index < 0)
                        throw new ArgumentException(
                            $"Enum value '{name}' is not valid for property '{property.name}'. Valid: [{string.Join(", ", property.enumNames)}].");
                }

                property.enumValueIndex = index;
            }
            else
            {
                // A numeric value is the enum's underlying integer, NOT an index into enumNames:
                // those only coincide when declaration order matches the constant values. Mirror Read,
                // which falls back to property.intValue, so round-tripping a numeric enum is stable.
                property.intValue = value.ToObject<int>();
            }
        }

        private static Object ResolveObjectReference(JToken value, string propertyName)
        {
            // An explicit null clears the reference; anything else MUST resolve — never silently no-op
            // (a handle that doesn't resolve used to be dropped, so the command reported success while
            // assigning nothing).
            if (value == null || value.Type == JTokenType.Null)
                return null;

            // Route every shape (string handle, handle object, or bare-integer instanceId) through
            // ObjectRefConverter so path / guid / globalId / instanceId forms all parse identically.
            var handle = value.ToObject<ObjectRef>();
            if (handle == null || handle.IsEmpty)
                throw new ArgumentException(
                    $"Property '{propertyName}': '{value}' is not a recognized object reference handle. " +
                    "Provide a path, guid, globalId, or instanceId (or null to clear).");

            if (!ObjectResolver.TryResolve(handle, out var obj, out var error))
                throw new ArgumentException($"Could not resolve object reference for '{propertyName}': {error}");

            return obj;
        }

        private static float[] ToFloats(JToken value, int expected, string propertyName)
        {
            if (value is JArray array)
            {
                if (array.Count != expected)
                    throw new ArgumentException(
                        $"Property '{propertyName}' expects {expected} components, got {array.Count}.");
                var result = new float[expected];
                for (int i = 0; i < expected; i++)
                    result[i] = array[i].ToObject<float>();
                return result;
            }

            throw new ArgumentException(
                $"Property '{propertyName}' expects an array of {expected} numbers.");
        }

        private static JArray ToArray(params float[] values)
        {
            var array = new JArray();
            foreach (var v in values)
                array.Add(v);
            return array;
        }
    }
}
