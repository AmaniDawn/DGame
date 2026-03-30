using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DGame
{
    public partial class Utility
    {
        public static class Reflection
        {
            public static object InvokeMethod(object obj, string methodName, object[] parameters = null)
            {
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(methodName))
                {
                    throw new ArgumentNullException($"方法名称无效");
                }

                Type type = obj.GetType();

                if (type.BaseType == null)
                {
                    throw new ArgumentNullException($"BaseType无效");
                }

                var method = type.BaseType.GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (method == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到方法 {methodName}");
                }
                return method.Invoke(obj, parameters);
            }

            public static object InvokeMethod(Type type, object obj, string methodName, object[] parameters = null)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }

                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(methodName))
                {
                    throw new ArgumentNullException($"方法名称无效");
                }

                var method = type.GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (method == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到方法 {methodName}");
                }
                return method.Invoke(obj, parameters);
            }

            public static void SetPropertyValue(object obj, string propertyName, object newValue)
            {
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException($"对象属性名称无效");
                }
                Type type = obj.GetType();
                var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (prop == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到属性 {propertyName}");
                }
                prop.SetValue(obj, newValue, null);
            }

            public static void SetPropertyValue(Type type, object obj, string propertyName, object newValue)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException($"对象属性名称无效");
                }
                var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (prop == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到属性 {propertyName}");
                }
                prop.SetValue(obj, newValue, null);
            }

            public static void SetFieldValue(Type type, object obj, string fieldName, object newValue)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(fieldName))
                {
                    throw new ArgumentNullException($"对象字段名称无效");
                }
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (field == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到字段 {fieldName}");
                }
                field.SetValue(obj, newValue);
            }

            public static void SetFieldValue(object obj, string fieldName, object newValue)
            {
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(fieldName))
                {
                    throw new ArgumentNullException($"对象字段名称无效");
                }
                Type type = obj.GetType();
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (field == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到字段 {fieldName}");
                }
                field.SetValue(obj, newValue);
            }

            public static object GetPropertyValue(object obj, string propertyName)
            {
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException($"对象属性名称无效");
                }
                Type type = obj.GetType();
                var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (prop == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到属性 {propertyName}");
                }
                return prop.GetValue(obj);
            }

            public static object GetPropertyValue(Type type, object obj, string propertyName)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException($"对象属性名称无效");
                }
                var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (prop == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到属性 {propertyName}");
                }
                return prop.GetValue(obj);
            }

            public static object GetFieldValue(object obj, string fieldName)
            {
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(fieldName))
                {
                    throw new ArgumentNullException($"对象字段名称无效");
                }
                Type type = obj.GetType();
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (field == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到字段 {fieldName}");
                }
                return field.GetValue(obj);
            }

            public static object GetFieldValue(Type type, object obj, string fieldName)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                if (obj == null)
                {
                    throw new NullReferenceException($"目标对象无效");
                }

                if (string.IsNullOrEmpty(fieldName))
                {
                    throw new ArgumentNullException($"对象字段名称无效");
                }
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (field == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到字段 {fieldName}");
                }
                return field.GetValue(obj);
            }

            public static object GetNonInstancePropertyValue(Type type, string propertyName)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException($"对象属性名称无效");
                }
                var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

                if (prop == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到属性 {propertyName}");
                }
                return prop.GetValue(propertyName);
            }

            public static object GetPropertyInfo(Type type, string propertyName)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException($"对象属性名称无效");
                }
                var prop = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);

                if (prop == null)
                {
                    throw new NullReferenceException($"类型: {type} 找不到属性 {propertyName}");
                }
                return prop;
            }

            public static string[] GetTypeAllFields<T>()
            {
                return GetTypeAllFields(typeof(T));
            }

            public static string[] GetTypeAllFields(Type type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                return fields.Select(f => f.Name).ToArray();
            }

            public static string[] GetTypeAllProperties<T>()
            {
                return GetTypeAllProperties(typeof(T));
            }

            public static string[] GetTypeAllProperties(Type type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                return props.Select(f => f.Name).ToArray();
            }

            public static IDictionary<string, Type> GetTypeFieldsNameAndTypeMapping(Type type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                return fields.ToDictionary(f => f.Name, f => f.FieldType);
            }

            public static IDictionary<string, Type> GetTypePropertyNameAndTypeMapping(Type type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException($"Type无效");
                }
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                return props.ToDictionary(f => f.Name, f => f.PropertyType);
            }
        }
    }
}