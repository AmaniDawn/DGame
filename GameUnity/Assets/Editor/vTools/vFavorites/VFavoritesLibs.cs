#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.UIElements;
using Type = System.Type;
using static VFavorites.Libs.VUtils;


namespace VFavorites.Libs
{
    public static class VUtils
    {
        #region Text


        public static string Decamelcase(this string str) => Regex.Replace(Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");

        public static string Remove(this string s, string toRemove)
        {
            if (toRemove == "") return s;
            return s.Replace(toRemove, "");
        }

        public static bool IsEmpty(this string s) => s == "";
        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);





        #endregion

        #region IEnumerables


        public static T AddAt<T>(this List<T> l, T r, int i)
        {
            if (i < 0) i = 0;
            if (i >= l.Count)
                l.Add(r);
            else
                l.Insert(i, r);
            return r;
        }
        public static T RemoveLast<T>(this List<T> l)
        {
            if (!l.Any()) return default;

            var r = l.Last();

            l.RemoveAt(l.Count - 1);

            return r;
        }

        public static void Add<T>(this List<T> list, params T[] items)
        {
            foreach (var r in items)
                list.Add(r);
        }

        public static int LastIndex<T>(this List<T> l) => l.Count - 1; // toremove

        // public static T GetAtWrapped<T>(this List<T> list, int i) // toremove
        // {
        //     while (i < 0) i += list.Count;
        //     while (i >= list.Count) i -= list.Count;

        //     return list[i];
        // }





        #endregion

        #region Linq


        public static T NextTo<T>(this IEnumerable<T> e, T to) => e.SkipWhile(r => !r.Equals(to)).Skip(1).FirstOrDefault();
        public static T PreviousTo<T>(this IEnumerable<T> e, T to) => e.Reverse().SkipWhile(r => !r.Equals(to)).Skip(1).FirstOrDefault();
        public static T NextToOtFirst<T>(this IEnumerable<T> e, T to) => e.NextTo(to) ?? e.First();
        public static T PreviousToOrLast<T>(this IEnumerable<T> e, T to) => e.PreviousTo(to) ?? e.Last();

        public static Dictionary<TKey, TValue> MergeDictionaries<TKey, TValue>(IEnumerable<Dictionary<TKey, TValue>> dicts)
        {
            if (dicts.Count() == 0) return null;
            if (dicts.Count() == 1) return dicts.First();

            var mergedDict = new Dictionary<TKey, TValue>(dicts.First());

            foreach (var dict in dicts.Skip(1))
                foreach (var r in dict)
                    if (!mergedDict.ContainsKey(r.Key))
                        mergedDict.Add(r.Key, r.Value);

            return mergedDict;
        }

        public static IEnumerable<T> InsertFirst<T>(this IEnumerable<T> ie, T t) => new[] { t }.Concat(ie);

        public static bool None<T>(this IEnumerable<T> ie, System.Func<T, bool> f) => !ie.Any(f);
        public static bool None<T>(this IEnumerable<T> ie) => !ie.Any();

        public static int IndexOfFirst<T>(this List<T> list, System.Func<T, bool> f) => list.FirstOrDefault(f) is T t ? list.IndexOf(t) : -1;
        public static int IndexOfLast<T>(this List<T> list, System.Func<T, bool> f) => list.LastOrDefault(f) is T t ? list.IndexOf(t) : -1;

        public static void SortBy<T, T2>(this List<T> list, System.Func<T, T2> keySelector) where T2 : System.IComparable => list.Sort((q, w) => keySelector(q).CompareTo(keySelector(w)));

        public static void RemoveValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value)
        {
            if (dictionary.FirstOrDefault(r => r.Value.Equals(value)) is var kvp)
                dictionary.Remove(kvp);
        }


        public static void ForEach<T>(this IEnumerable<T> sequence, System.Action<T> action) { foreach (T item in sequence) action(item); }





        #endregion

        #region Reflection

        public static object GetFieldValue(this object o, string fieldName, bool exceptionIfNotFound = true)
        {
            var type = (o as Type) ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(fieldName) is FieldInfo fieldInfo)
                return fieldInfo.GetValue(target);


            if (exceptionIfNotFound)
                throw new System.Exception($"Field '{fieldName}' not found in type '{type.Name}' and its parent types");

            return null;

        }
        public static object GetPropertyValue(this object o, string propertyName, bool exceptionIfNotFound = true)
        {
            var type = (o as Type) ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetPropertyInfo(propertyName) is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(target);


            if (exceptionIfNotFound)
                throw new System.Exception($"Property '{propertyName}' not found in type '{type.Name}' and its parent types");

            return null;

        }
        public static object GetMemberValue(this object o, string memberName, bool exceptionIfNotFound = true)
        {
            var type = (o as Type) ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(memberName) is FieldInfo fieldInfo)
                return fieldInfo.GetValue(target);

            if (type.GetPropertyInfo(memberName) is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(target);


            if (exceptionIfNotFound)
                throw new System.Exception($"Member '{memberName}' not found in type '{type.Name}' and its parent types");

            return null;

        }

        public static void SetFieldValue(this object o, string fieldName, object value, bool exceptionIfNotFound = true)
        {
            var type = (o as Type) ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(fieldName) is FieldInfo fieldInfo)
                fieldInfo.SetValue(target, value);


            else if (exceptionIfNotFound)
                throw new System.Exception($"Field '{fieldName}' not found in type '{type.Name}' and its parent types");

        }
        public static void SetPropertyValue(this object o, string propertyName, object value, bool exceptionIfNotFound = true)
        {
            var type = (o as Type) ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetPropertyInfo(propertyName) is PropertyInfo propertyInfo)
                propertyInfo.SetValue(target, value);


            else if (exceptionIfNotFound)
                throw new System.Exception($"Property '{propertyName}' not found in type '{type.Name}' and its parent types");

        }
        public static void SetMemberValue(this object o, string memberName, object value, bool exceptionIfNotFound = true)
        {
            var type = (o as Type) ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(memberName) is FieldInfo fieldInfo)
                fieldInfo.SetValue(target, value);

            else if (type.GetPropertyInfo(memberName) is PropertyInfo propertyInfo)
                propertyInfo.SetValue(target, value);


            else if (exceptionIfNotFound)
                throw new System.Exception($"Member '{memberName}' not found in type '{type.Name}' and its parent types");

        }

        public static object InvokeMethod(this object o, string methodName, params object[] parameters) // todo handle null params (can't get their type)
        {
            var type = (o as Type) ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetMethodInfo(methodName, parameters.Select(r => r.GetType()).ToArray()) is MethodInfo methodInfo)
                return methodInfo.Invoke(target, parameters);


            throw new System.Exception($"Method '{methodName}' not found in type '{type.Name}', its parent types and interfaces");

        }



        static FieldInfo GetFieldInfo(this Type type, string fieldName)
        {
            if (fieldInfoCache.TryGetValue(type, out var fieldInfosByNames))
                if (fieldInfosByNames.TryGetValue(fieldName, out var fieldInfo))
                    return fieldInfo;


            if (!fieldInfoCache.ContainsKey(type))
                fieldInfoCache[type] = new Dictionary<string, FieldInfo>();

            for (var curType = type; curType != null; curType = curType.BaseType)
                if (curType.GetField(fieldName, maxBindingFlags) is FieldInfo fieldInfo)
                    return fieldInfoCache[type][fieldName] = fieldInfo;


            return fieldInfoCache[type][fieldName] = null;

        }
        static Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfoCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        static PropertyInfo GetPropertyInfo(this Type type, string propertyName)
        {
            if (propertyInfoCache.TryGetValue(type, out var propertyInfosByNames))
                if (propertyInfosByNames.TryGetValue(propertyName, out var propertyInfo))
                    return propertyInfo;


            if (!propertyInfoCache.ContainsKey(type))
                propertyInfoCache[type] = new Dictionary<string, PropertyInfo>();

            for (var curType = type; curType != null; curType = curType.BaseType)
                if (curType.GetProperty(propertyName, maxBindingFlags) is PropertyInfo propertyInfo)
                    return propertyInfoCache[type][propertyName] = propertyInfo;


            return propertyInfoCache[type][propertyName] = null;

        }
        static Dictionary<Type, Dictionary<string, PropertyInfo>> propertyInfoCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        static MethodInfo GetMethodInfo(this Type type, string methodName, params Type[] argumentTypes)
        {
            var methodHash = methodName.GetHashCode() ^ argumentTypes.Aggregate(0, (hash, r) => hash ^= r.GetHashCode());


            if (methodInfoCache.TryGetValue(type, out var methodInfosByHashes))
                if (methodInfosByHashes.TryGetValue(methodHash, out var methodInfo))
                    return methodInfo;



            if (!methodInfoCache.ContainsKey(type))
                methodInfoCache[type] = new Dictionary<int, MethodInfo>();

            for (var curType = type; curType != null; curType = curType.BaseType)
                if (curType.GetMethod(methodName, maxBindingFlags, null, argumentTypes, null) is MethodInfo methodInfo)
                    return methodInfoCache[type][methodHash] = methodInfo;

            foreach (var interfaceType in type.GetInterfaces())
                if (interfaceType.GetMethod(methodName, maxBindingFlags, null, argumentTypes, null) is MethodInfo methodInfo)
                    return methodInfoCache[type][methodHash] = methodInfo;



            return methodInfoCache[type][methodHash] = null;

        }
        static Dictionary<Type, Dictionary<int, MethodInfo>> methodInfoCache = new Dictionary<Type, Dictionary<int, MethodInfo>>();



        public static T GetFieldValue<T>(this object o, string fieldName, bool exceptionIfNotFound = true) => (T)o.GetFieldValue(fieldName, exceptionIfNotFound);
        public static T GetPropertyValue<T>(this object o, string propertyName, bool exceptionIfNotFound = true) => (T)o.GetPropertyValue(propertyName, exceptionIfNotFound);
        public static T GetMemberValue<T>(this object o, string memberName, bool exceptionIfNotFound = true) => (T)o.GetMemberValue(memberName, exceptionIfNotFound);
        public static T InvokeMethod<T>(this object o, string methodName, params object[] parameters) => (T)o.InvokeMethod(methodName, parameters);






        public static List<Type> GetSubclasses(this Type t) => t.Assembly.GetTypes().Where(type => type.IsSubclassOf(t)).ToList();

        public static object GetDefaultValue(this FieldInfo f, params object[] constructorVars) => f.GetValue(System.Activator.CreateInstance(((MemberInfo)f).ReflectedType, constructorVars));

        public static object GetDefaultValue(this FieldInfo f) => f.GetValue(System.Activator.CreateInstance(((MemberInfo)f).ReflectedType));


        public static IEnumerable<FieldInfo> GetFieldsWithoutBase(this Type t) => t.GetFields().Where(r => !t.BaseType.GetFields().Any(rr => rr.Name == r.Name));

        public static IEnumerable<PropertyInfo> GetPropertiesWithoutBase(this Type t) => t.GetProperties().Where(r => !t.BaseType.GetProperties().Any(rr => rr.Name == r.Name));


        public const BindingFlags maxBindingFlags = (BindingFlags)62;







        #endregion

        #region Math


        public static bool Approx(this float f1, float f2) => Mathf.Approximately(f1, f2);
        public static float DistanceTo(this float f1, float f2) => Mathf.Abs(f1 - f2);
        public static float DistanceTo(this Vector2 f1, Vector2 f2) => (f1 - f2).magnitude;
        public static float DistanceTo(this Vector3 f1, Vector3 f2) => (f1 - f2).magnitude;
        public static float Dist(float f1, float f2) => Mathf.Abs(f1 - f2);
        public static float Avg(float f1, float f2) => (f1 + f2) / 2;
        public static float Abs(this float f) => Mathf.Abs(f);
        public static int Abs(this int f) => Mathf.Abs(f);
        public static float Sign(this float f) => Mathf.Sign(f);
        public static float Clamp(this float f, float f0, float f1) => Mathf.Clamp(f, f0, f1);
        public static int Clamp(this int f, int f0, int f1) => Mathf.Clamp(f, f0, f1);
        public static float Clamp01(this float f) => Mathf.Clamp(f, 0, 1);
        public static Vector2 Clamp01(this Vector2 f) => new Vector2(f.x.Clamp01(), f.y.Clamp01());
        public static Vector3 Clamp01(this Vector3 f) => new Vector3(f.x.Clamp01(), f.y.Clamp01(), f.z.Clamp01());


        public static float Pow(this float f, float pow) => Mathf.Pow(f, pow);
        public static int Pow(this int f, int pow) => (int)Mathf.Pow(f, pow);

        public static float Round(this float f) => Mathf.Round(f);
        public static float Ceil(this float f) => Mathf.Ceil(f);
        public static float Floor(this float f) => Mathf.Floor(f);
        public static int RoundToInt(this float f) => Mathf.RoundToInt(f);
        public static int CeilToInt(this float f) => Mathf.CeilToInt(f);
        public static int FloorToInt(this float f) => Mathf.FloorToInt(f);
        public static int ToInt(this float f) => (int)f;
        public static float ToFloat(this int f) => (float)f;
        public static float ToFloat(this double f) => (float)f;


        public static float Sqrt(this float f) => Mathf.Sqrt(f);

        public static float Max(this float f, float ff) => Mathf.Max(f, ff);
        public static float Min(this float f, float ff) => Mathf.Min(f, ff);
        public static int Max(this int f, int ff) => Mathf.Max(f, ff);
        public static int Min(this int f, int ff) => Mathf.Min(f, ff);

        public static float Loop(this float f, float boundMin, float boundMax)
        {
            while (f < boundMin) f += boundMax - boundMin;
            while (f > boundMax) f -= boundMax - boundMin;
            return f;
        }
        public static float Loop(this float f, float boundMax) => f.Loop(0, boundMax);

        public static float PingPong(this float f, float boundMin, float boundMax) => boundMin + Mathf.PingPong(f - boundMin, boundMax - boundMin);
        public static float PingPong(this float f, float boundMax) => f.PingPong(0, boundMax);


        public static float TriangleArea(Vector2 A, Vector2 B, Vector2 C) => Vector3.Cross(A - B, A - C).z.Abs() / 2;
        public static Vector2 LineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
        {
            var a1 = B.y - A.y;
            var b1 = A.x - B.x;
            var c1 = a1 * A.x + b1 * A.y;

            var a2 = D.y - C.y;
            var b2 = C.x - D.x;
            var c2 = a2 * C.x + b2 * C.y;

            var d = a1 * b2 - a2 * b1;

            var x = (b2 * c1 - b1 * c2) / d;
            var y = (a1 * c2 - a2 * c1) / d;

            return new Vector2(x, y);

        }

        public static float ProjectOn(this Vector2 v, Vector2 on) => Vector3.Project(v, on).magnitude;
        public static float AngleTo(this Vector2 v, Vector2 to) => Vector2.Angle(v, to);

        public static Vector2 Rotate(this Vector2 v, float deg) => Quaternion.AngleAxis(deg, Vector3.forward) * v;

        public static float Smoothstep(this float f) { f = f.Clamp01(); return f * f * (3 - 2 * f); }

        public static float InverseLerp(this Vector2 v, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var av = v - a;
            return Vector2.Dot(av, ab) / Vector2.Dot(ab, ab);
        }

        public static bool IsOdd(this int i) => i % 2 == 1;
        public static bool IsEven(this int i) => i % 2 == 0;

        public static bool IsInRange(this int i, int a, int b) => i >= a && i <= b;
        public static bool IsInRange(this float i, float a, float b) => i >= a && i <= b;

        public static bool IsInRangeOf(this int i, IList list) => i.IsInRange(0, list.Count - 1);
        public static bool IsInRangeOf<T>(this int i, T[] array) => i.IsInRange(0, array.Length - 1);







        #endregion

        #region Lerping


        public static float LerpT(float lerpSpeed, float deltaTime) => 1 - Mathf.Exp(-lerpSpeed * 2f * deltaTime);
        public static float LerpT(float lerpSpeed) => LerpT(lerpSpeed, Time.deltaTime);

        public static float Lerp(float f1, float f2, float t) => Mathf.LerpUnclamped(f1, f2, t);
        public static float Lerp(ref float f1, float f2, float t) => f1 = Lerp(f1, f2, t);

        public static Vector2 Lerp(Vector2 f1, Vector2 f2, float t) => Vector2.LerpUnclamped(f1, f2, t);
        public static Vector2 Lerp(ref Vector2 f1, Vector2 f2, float t) => f1 = Lerp(f1, f2, t);

        public static Vector3 Lerp(Vector3 f1, Vector3 f2, float t) => Vector3.LerpUnclamped(f1, f2, t);
        public static Vector3 Lerp(ref Vector3 f1, Vector3 f2, float t) => f1 = Lerp(f1, f2, t);

        public static Color Lerp(Color f1, Color f2, float t) => Color.LerpUnclamped(f1, f2, t);
        public static Color Lerp(ref Color f1, Color f2, float t) => f1 = Lerp(f1, f2, t);


        public static float Lerp(float current, float target, float speed, float deltaTime) => Mathf.Lerp(current, target, LerpT(speed, deltaTime));
        public static float Lerp(ref float current, float target, float speed, float deltaTime) => current = Lerp(current, target, speed, deltaTime);

        public static Vector2 Lerp(Vector2 current, Vector2 target, float speed, float deltaTime) => Vector2.Lerp(current, target, LerpT(speed, deltaTime));
        public static Vector2 Lerp(ref Vector2 current, Vector2 target, float speed, float deltaTime) => current = Lerp(current, target, speed, deltaTime);

        public static Vector3 Lerp(Vector3 current, Vector3 target, float speed, float deltaTime) => Vector3.Lerp(current, target, LerpT(speed, deltaTime));
        public static Vector3 Lerp(ref Vector3 current, Vector3 target, float speed, float deltaTime) => current = Lerp(current, target, speed, deltaTime);

        public static float SmoothDamp(float current, float target, float speed, ref float derivative, float deltaTime) => Mathf.SmoothDamp(current, target, ref derivative, .5f / speed, Mathf.Infinity, deltaTime);
        public static float SmoothDamp(ref float current, float target, float speed, ref float derivative, float deltaTime) => current = SmoothDamp(current, target, speed, ref derivative, deltaTime);
        public static float SmoothDamp(float current, float target, float speed, ref float derivative) => SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
        public static float SmoothDamp(ref float current, float target, float speed, ref float derivative) => current = SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);

        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, float speed, ref Vector2 derivative, float deltaTime) => Vector2.SmoothDamp(current, target, ref derivative, .5f / speed, Mathf.Infinity, deltaTime);
        public static Vector2 SmoothDamp(ref Vector2 current, Vector2 target, float speed, ref Vector2 derivative, float deltaTime) => current = SmoothDamp(current, target, speed, ref derivative, deltaTime);
        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, float speed, ref Vector2 derivative) => SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
        public static Vector2 SmoothDamp(ref Vector2 current, Vector2 target, float speed, ref Vector2 derivative) => current = SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, float speed, ref Vector3 derivative, float deltaTime) => Vector3.SmoothDamp(current, target, ref derivative, .5f / speed, Mathf.Infinity, deltaTime);
        public static Vector3 SmoothDamp(ref Vector3 current, Vector3 target, float speed, ref Vector3 derivative, float deltaTime) => current = SmoothDamp(current, target, speed, ref derivative, deltaTime);
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, float speed, ref Vector3 derivative) => SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
        public static Vector3 SmoothDamp(ref Vector3 current, Vector3 target, float speed, ref Vector3 derivative) => current = SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);






        #endregion

        #region Colors


        public static Color HSLToRGB(float h, float s, float l)
        {
            float hue2Rgb(float v1, float v2, float vH)
            {
                if (vH < 0f)
                    vH += 1f;

                if (vH > 1f)
                    vH -= 1f;

                if (6f * vH < 1f)
                    return v1 + (v2 - v1) * 6f * vH;

                if (2f * vH < 1f)
                    return v2;

                if (3f * vH < 2f)
                    return v1 + (v2 - v1) * (2f / 3f - vH) * 6f;

                return v1;
            }

            if (s.Approx(0)) return new Color(l, l, l);

            float k1;

            if (l < .5f)
                k1 = l * (1f + s);
            else
                k1 = l + s - s * l;


            var k2 = 2f * l - k1;

            float r, g, b;
            r = hue2Rgb(k2, k1, h + 1f / 3);
            g = hue2Rgb(k2, k1, h);
            b = hue2Rgb(k2, k1, h - 1f / 3);

            return new Color(r, g, b);
        }
        public static Color LCHtoRGB(float l, float c, float h)
        {
            l *= 100;
            c *= 100;
            h *= 360;

            double xw = 0.948110;
            double yw = 1.00000;
            double zw = 1.07304;

            float a = c * Mathf.Cos(Mathf.Deg2Rad * h);
            float b = c * Mathf.Sin(Mathf.Deg2Rad * h);

            float fy = (l + 16) / 116;
            float fx = fy + (a / 500);
            float fz = fy - (b / 200);

            float x = (float)System.Math.Round(xw * ((System.Math.Pow(fx, 3) > 0.008856) ? System.Math.Pow(fx, 3) : ((fx - 16 / 116) / 7.787)), 5);
            float y = (float)System.Math.Round(yw * ((System.Math.Pow(fy, 3) > 0.008856) ? System.Math.Pow(fy, 3) : ((fy - 16 / 116) / 7.787)), 5);
            float z = (float)System.Math.Round(zw * ((System.Math.Pow(fz, 3) > 0.008856) ? System.Math.Pow(fz, 3) : ((fz - 16 / 116) / 7.787)), 5);

            float r = x * 3.2406f - y * 1.5372f - z * 0.4986f;
            float g = -x * 0.9689f + y * 1.8758f + z * 0.0415f;
            float bValue = x * 0.0557f - y * 0.2040f + z * 1.0570f;

            r = r > 0.0031308f ? 1.055f * (float)System.Math.Pow(r, 1 / 2.4) - 0.055f : r * 12.92f;
            g = g > 0.0031308f ? 1.055f * (float)System.Math.Pow(g, 1 / 2.4) - 0.055f : g * 12.92f;
            bValue = bValue > 0.0031308f ? 1.055f * (float)System.Math.Pow(bValue, 1 / 2.4) - 0.055f : bValue * 12.92f;

            // r = (float)System.Math.Round(System.Math.Max(0, System.Math.Min(1, r)));
            // g = (float)System.Math.Round(System.Math.Max(0, System.Math.Min(1, g)));
            // bValue = (float)System.Math.Round(System.Math.Max(0, System.Math.Min(1, bValue)));

            return new Color(r, g, bValue);

        }



        public static Color Greyscale(float brightness, float alpha = 1) => new Color(brightness, brightness, brightness, alpha);

        public static Color SetAlpha(this Color color, float alpha) { color.a = alpha; return color; }

        public static Color MultiplyAlpha(this Color color, float k) { color.a *= k; return color; }





        #endregion

        #region Rects


        public static Rect Resize(this Rect rect, float px) { rect.x += px; rect.y += px; rect.width -= px * 2; rect.height -= px * 2; return rect; }

        public static Rect SetPos(this Rect rect, Vector2 v) => rect.SetPos(v.x, v.y);
        public static Rect SetPos(this Rect rect, float x, float y) { rect.x = x; rect.y = y; return rect; }

        public static Rect SetX(this Rect rect, float x) => rect.SetPos(x, rect.y);
        public static Rect SetY(this Rect rect, float y) => rect.SetPos(rect.x, y);
        public static Rect SetXMax(this Rect rect, float xMax) { rect.xMax = xMax; return rect; }
        public static Rect SetYMax(this Rect rect, float yMax) { rect.yMax = yMax; return rect; }

        public static Rect SetMidPos(this Rect r, Vector2 v) => r.SetPos(v).MoveX(-r.width / 2).MoveY(-r.height / 2);

        public static Rect Move(this Rect rect, Vector2 v) { rect.position += v; return rect; }
        public static Rect Move(this Rect rect, float x, float y) { rect.x += x; rect.y += y; return rect; }
        public static Rect MoveX(this Rect rect, float px) { rect.x += px; return rect; }
        public static Rect MoveY(this Rect rect, float px) { rect.y += px; return rect; }

        public static Rect SetWidth(this Rect rect, float f) { rect.width = f; return rect; }
        public static Rect SetWidthFromMid(this Rect rect, float px) { rect.x += rect.width / 2; rect.width = px; rect.x -= rect.width / 2; return rect; }
        public static Rect SetWidthFromRight(this Rect rect, float px) { rect.x += rect.width; rect.width = px; rect.x -= rect.width; return rect; }

        public static Rect SetHeight(this Rect rect, float f) { rect.height = f; return rect; }
        public static Rect SetHeightFromMid(this Rect rect, float px) { rect.y += rect.height / 2; rect.height = px; rect.y -= rect.height / 2; return rect; }
        public static Rect SetHeightFromBottom(this Rect rect, float px) { rect.y += rect.height; rect.height = px; rect.y -= rect.height; return rect; }

        public static Rect AddWidth(this Rect rect, float f) => rect.SetWidth(rect.width + f);
        public static Rect AddWidthFromMid(this Rect rect, float f) => rect.SetWidthFromMid(rect.width + f);
        public static Rect AddWidthFromRight(this Rect rect, float f) => rect.SetWidthFromRight(rect.width + f);

        public static Rect AddHeight(this Rect rect, float f) => rect.SetHeight(rect.height + f);
        public static Rect AddHeightFromMid(this Rect rect, float f) => rect.SetHeightFromMid(rect.height + f);
        public static Rect AddHeightFromBottom(this Rect rect, float f) => rect.SetHeightFromBottom(rect.height + f);

        public static Rect SetSize(this Rect rect, Vector2 v) => rect.SetWidth(v.x).SetHeight(v.y);
        public static Rect SetSize(this Rect rect, float w, float h) => rect.SetWidth(w).SetHeight(h);
        public static Rect SetSize(this Rect rect, float f) { rect.height = rect.width = f; return rect; }

        public static Rect SetSizeFromMid(this Rect r, Vector2 v) => r.Move(r.size / 2).SetSize(v).Move(-v / 2);
        public static Rect SetSizeFromMid(this Rect r, float x, float y) => r.SetSizeFromMid(new Vector2(x, y));
        public static Rect SetSizeFromMid(this Rect r, float f) => r.SetSizeFromMid(new Vector2(f, f));

        public static Rect AlignToPixelGrid(this Rect r) => GUIUtility.AlignRectToDevice(r);





        #endregion

        #region Vectors


        public static Vector2 AddX(this Vector2 v, float f) => new Vector2(v.x + f, v.y + 0);
        public static Vector2 AddY(this Vector2 v, float f) => new Vector2(v.x + 0, v.y + f);

        public static Vector3 AddX(this Vector3 v, float f) => new Vector3(v.x + f, v.y + 0, v.z + 0);
        public static Vector3 AddY(this Vector3 v, float f) => new Vector3(v.x + 0, v.y + f, v.z + 0);
        public static Vector3 AddZ(this Vector3 v, float f) => new Vector3(v.x + 0, v.y + 0, v.z + f);

        public static Vector2 xx(this Vector3 v) { return new Vector2(v.x, v.x); }
        public static Vector2 xy(this Vector3 v) { return new Vector2(v.x, v.y); }
        public static Vector2 xz(this Vector3 v) { return new Vector2(v.x, v.z); }
        public static Vector2 yx(this Vector3 v) { return new Vector2(v.y, v.x); }
        public static Vector2 yy(this Vector3 v) { return new Vector2(v.y, v.y); }
        public static Vector2 yz(this Vector3 v) { return new Vector2(v.y, v.z); }
        public static Vector2 zx(this Vector3 v) { return new Vector2(v.z, v.x); }
        public static Vector2 zy(this Vector3 v) { return new Vector2(v.z, v.y); }
        public static Vector2 zz(this Vector3 v) { return new Vector2(v.z, v.z); }





        #endregion

        #region Compute


        [System.Serializable]
        public class GaussianKernel
        {
            public GaussianKernel(bool isEvenSize = false, int radius = 7, float sharpness = .5f)
            {
                this.isEvenSize = isEvenSize;
                this.radius = radius;
                this.sharpness = sharpness;
            }

            public bool isEvenSize = false;

            public int radius = 7;
            public float sharpness = .5f;

            public int size => radius * 2 + (isEvenSize ? 0 : 1);
            public float sigma => 1 - Mathf.Pow(sharpness, .1f) * .99999f;

            public float[,] Array2d()
            {
                float[,] kr = new float[size, size];

                if (size == 1) { kr[0, 0] = 1; return kr; }

                var a = -2f * radius * radius / Mathf.Log(sigma);
                var sum = 0f;

                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        var rX = size % 2 == 1 ? (x - radius) : (x - radius) + .5f;
                        var rY = size % 2 == 1 ? (y - radius) : (y - radius) + .5f;
                        var dist = Mathf.Sqrt(rX * rX + rY * rY);
                        kr[x, y] = Mathf.Exp(-dist * dist / a);
                        sum += kr[x, y];
                    }

                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        kr[x, y] /= sum;

                return kr;
            }
            public float[] ArrayFlat()
            {
                var gk = Array2d();
                float[] flat = new float[size * size];

                for (int i = 0; i < size; i++)
                    for (int j = 0; j < size; j++)
                        flat[(i * size + j)] = gk[i, j];

                return flat;
            }
        }





        #endregion

        #region Objects


        public static Object[] FindObjects(Type type)
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType(type, FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType(type);
#endif
        }
        public static T[] FindObjects<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }

        public static void Destroy(this Object r)
        {
            if (Application.isPlaying)
                Object.Destroy(r);
            else
                Object.DestroyImmediate(r);

        }

        public static void DestroyImmediate(this Object o) => Object.DestroyImmediate(o);





        #endregion

        #region GlobalID

#if UNITY_EDITOR

        [System.Serializable]
        public struct GlobalID : System.IEquatable<GlobalID>
        {
            public Object GetObject() => GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            public int GetObjectInstanceId() => GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(globalObjectId);


            public string guid => globalObjectId.assetGUID.ToString();
            public ulong fileId => globalObjectId.targetObjectId;

            public bool isNull => globalObjectId.identifierType == 0;
            public bool isAsset => globalObjectId.identifierType == 1;
            public bool isSceneObject => globalObjectId.identifierType == 2;

            public GlobalObjectId globalObjectId => _globalObjectId.Equals(default) && globalObjectIdString != null && GlobalObjectId.TryParse(globalObjectIdString, out var r) ? _globalObjectId = r : _globalObjectId;
            public GlobalObjectId _globalObjectId;

            public GlobalID(Object o) => globalObjectIdString = (_globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(o)).ToString();
            public GlobalID(string s) => globalObjectIdString = GlobalObjectId.TryParse(s, out _globalObjectId) ? s : s;

            public string globalObjectIdString;



            public bool Equals(GlobalID other) => this.globalObjectIdString.Equals(other.globalObjectIdString);

            public static bool operator ==(GlobalID a, GlobalID b) => a.Equals(b);
            public static bool operator !=(GlobalID a, GlobalID b) => !a.Equals(b);

            public override bool Equals(object other) => other is GlobalID otherglobalID && this.Equals(otherglobalID);
            public override int GetHashCode() => globalObjectIdString == null ? 0 : globalObjectIdString.GetHashCode();


            public override string ToString() => globalObjectIdString;

        }

        public static GlobalID GetGlobalID(this Object o) => new GlobalID(o);

        public static int[] GetObjectInstanceIds(this IEnumerable<GlobalID> globalIDs)
        {
            var goids = globalIDs.Select(r => r.globalObjectId).ToArray();

            var iids = new int[goids.Length];

            GlobalObjectId.GlobalObjectIdentifiersToInstanceIDsSlow(goids, iids);

            return iids;
        }


#endif




        #endregion

        #region Paths


        public static string GetParentPath(this string path) => path.Substring(0, path.LastIndexOf('/'));
        public static bool HasParentPath(this string path) => path.Contains('/') && path.GetParentPath() != "";

        public static string ToGlobalPath(this string localPath) => Application.dataPath + "/" + localPath.Substring(0, localPath.Length - 1);
        public static string ToLocalPath(this string globalPath) => "Assets" + globalPath.Remove(Application.dataPath);



        public static string CombinePath(this string p, string p2) => Path.Combine(p, p2);

        public static bool IsSubpathOf(this string path, string of) => path.StartsWith(of + "/") || of == "";

        public static string GetDirectory(this string pathOrDirectory)
        {
            var directory = pathOrDirectory.Contains('.') ? pathOrDirectory.Substring(0, pathOrDirectory.LastIndexOf('/')) : pathOrDirectory;

            if (directory.Contains('.'))
                directory = directory.Substring(0, directory.LastIndexOf('/'));

            return directory;

        }

        public static bool DirectoryExists(this string pathOrDirectory) => Directory.Exists(pathOrDirectory.GetDirectory());

        public static string EnsureDirExists(this string pathOrDirectory) // todo to EnsureDirectoryExists
        {
            var directory = pathOrDirectory.GetDirectory();

            if (directory.HasParentPath() && !Directory.Exists(directory.GetParentPath()))
                EnsureDirExists(directory.GetParentPath());

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return pathOrDirectory;

        }



        public static string ClearDir(this string dir)
        {
            if (!Directory.Exists(dir)) return dir;

            var diri = new DirectoryInfo(dir);
            foreach (var r in diri.EnumerateFiles()) r.Delete();
            foreach (var r in diri.EnumerateDirectories()) r.Delete(true);

            return dir;
        }






#if UNITY_EDITOR

        public static string EnsurePathIsUnique(this string path)
        {
            if (!path.DirectoryExists()) return path;

            var s = AssetDatabase.GenerateUniqueAssetPath(path); // returns empty if parent dir doesnt exist 

            return s == "" ? path : s;

        }

        public static void EnsureDirExistsAndRevealInFinder(string dir)
        {
            EnsureDirExists(dir);
            UnityEditor.EditorUtility.OpenWithDefaultApp(dir);
        }

#endif



        #endregion

        #region AssetDatabase

#if UNITY_EDITOR

        public static AssetImporter GetImporter(this Object t) => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t));

        public static string ToPath(this string guid) => AssetDatabase.GUIDToAssetPath(guid); // returns empty string if not found
        public static List<string> ToPaths(this IEnumerable<string> guids) => guids.Select(r => r.ToPath()).ToList();

        public static string GetFilename(this string path, bool withExtension = false) => withExtension ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path); // prev GetName
        public static string GetExtension(this string path) => Path.GetExtension(path);


        public static string ToGuid(this string pathInProject) => AssetDatabase.AssetPathToGUID(pathInProject);
        public static List<string> ToGuids(this IEnumerable<string> pathsInProject) => pathsInProject.Select(r => r.ToGuid()).ToList();

        public static string GetPath(this Object o) => AssetDatabase.GetAssetPath(o);
        public static string GetGuid(this Object o) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o));

        public static string GetScriptPath(string scriptName) => AssetDatabase.FindAssets("t: script " + scriptName, null).FirstOrDefault()?.ToPath() ?? "scirpt not found";


        public static bool IsValidGuid(this string guid) => AssetDatabase.AssetPathToGUID(AssetDatabase.GUIDToAssetPath(guid), AssetPathToGUIDOptions.OnlyExistingAssets) != "";



        // toremove
        public static Object LoadGuid(this string guid) => AssetDatabase.LoadAssetAtPath(guid.ToPath(), typeof(Object));
        public static T LoadGuid<T>(this string guid) where T : Object => AssetDatabase.LoadAssetAtPath<T>(guid.ToPath());




        public static List<string> FindAllAssetsOfType_guids(Type type) => AssetDatabase.FindAssets("t:" + type.Name).ToList();
        public static List<string> FindAllAssetsOfType_guids(Type type, string path) => AssetDatabase.FindAssets("t:" + type.Name, new[] { path }).ToList();
        public static List<T> FindAllAssetsOfType<T>() where T : Object => FindAllAssetsOfType_guids(typeof(T)).Select(r => (T)r.LoadGuid()).ToList();
        public static List<T> FindAllAssetsOfType<T>(string path) where T : Object => FindAllAssetsOfType_guids(typeof(T), path).Select(r => (T)r.LoadGuid()).ToList();

        public static T Reimport<T>(this T t) where T : Object { AssetDatabase.ImportAsset(t.GetPath(), ImportAssetOptions.ForceUpdate); return t; }

#endif





        #endregion

        #region Serialization

        [System.Serializable]
        public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        {
            [SerializeField] List<TKey> keys = new List<TKey>();
            [SerializeField] List<TValue> values = new List<TValue>();

            public void OnBeforeSerialize()
            {
                keys.Clear();
                values.Clear();

                foreach (KeyValuePair<TKey, TValue> kvp in this)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }

            }
            public void OnAfterDeserialize()
            {
                this.Clear();

                for (int i = 0; i < keys.Count; i++)
                    this[keys[i]] = values[i];

            }

        }


        #endregion

        #region Editor

#if UNITY_EDITOR

        public static void ToggleDefineDisabledInScript(Type scriptType)
        {
            var path = GetScriptPath(scriptType.Name);

            var lines = File.ReadAllLines(path);
            if (lines.First().StartsWith("#define DISABLED"))
                File.WriteAllLines(path, lines.Skip(1));
            else
                File.WriteAllLines(path, lines.Prepend("#define DISABLED    // this line was added by VUtils.ToggleDefineDisabledInScript"));

            AssetDatabase.ImportAsset(path);
        }
        public static bool ScriptHasDefineDisabled(Type scriptType) => File.ReadLines(GetScriptPath(scriptType.Name)).First().StartsWith("#define DISABLED");
        public static void SetDefineDisabledInScript(Type scriptType, bool defineDisabled)
        {
            if (ScriptHasDefineDisabled(scriptType) != defineDisabled)
                ToggleDefineDisabledInScript(scriptType);

        }

        public static int GetProjectId() => Application.dataPath.GetHashCode();

        public static void PingObject(Object o, bool select = false, bool focusProjectWindow = true)
        {
            if (select)
            {
                Selection.activeObject = null;
                Selection.activeObject = o;
            }
            if (focusProjectWindow) EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(o);

        }
        public static void PingObject(string guid, bool select = false, bool focusProjectWindow = true) => PingObject(AssetDatabase.LoadAssetAtPath<Object>(guid.ToPath()));


        public static void OpenFolder(string path)
        {
            var folder = AssetDatabase.LoadAssetAtPath(path, typeof(Object));

            var t = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
            var w = (EditorWindow)t.GetField("s_LastInteractedProjectBrowser").GetValue(null);

            var m_ListAreaState = t.GetField("m_ListAreaState", maxBindingFlags).GetValue(w);

            m_ListAreaState.GetType().GetField("m_SelectedInstanceIDs").SetValue(m_ListAreaState, new List<int> { folder.GetInstanceID() });

            t.GetMethod("OpenSelectedFolders", maxBindingFlags).Invoke(null, null);

        }

        public static void Dirty(this Object o) => UnityEditor.EditorUtility.SetDirty(o);
        public static void Save(this Object o) => AssetDatabase.SaveAssetIfDirty(o);
        public static void RecordUndo(this Object o) => Undo.RecordObject(o, "");


        public static EditorWindow OpenObjectPicker<T>(Object obj = null, bool allowSceneObjects = false, string searchFilter = "", int controlID = 0) where T : Object
        {
            EditorGUIUtility.ShowObjectPicker<T>(obj, allowSceneObjects, searchFilter, controlID);

            return Resources.FindObjectsOfTypeAll(typeof(Editor).Assembly.GetType("UnityEditor.ObjectSelector")).FirstOrDefault() as EditorWindow;

        }
        public static EditorWindow OpenColorPicker(System.Action<Color> colorChangedCallback, Color color, bool showAlpha = true, bool hdr = false)
        {
            typeof(Editor).Assembly.GetType("UnityEditor.ColorPicker").InvokeMethod("Show", colorChangedCallback, color, showAlpha, hdr);

            return typeof(Editor).Assembly.GetType("UnityEditor.ColorPicker").GetPropertyValue<EditorWindow>("instance");

        }

        public static void MoveTo(this EditorWindow window, Vector2 position, bool ensureFitsOnScreen = true)
        {
            if (!ensureFitsOnScreen) { window.position = window.position.SetPos(position); return; }

            var windowRect = window.position;
            var unityWindowRect = EditorGUIUtility.GetMainWindowPosition();

            position.x = position.x.Max(unityWindowRect.position.x);
            position.y = position.y.Max(unityWindowRect.position.y);

            position.x = position.x.Min(unityWindowRect.xMax - windowRect.width);
            position.y = position.y.Min(unityWindowRect.yMax - windowRect.height);

            window.position = windowRect.SetPos(position);

        }



        public static void RemoveEditorErrors() => removeEditorErrorsMethod.Invoke(null, new object[] { 1 });
        static MethodInfo removeEditorErrorsMethod = System.AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(r => r.GetName().ToString().Contains("UnityEditor.CoreModule")).GetTypes().First(r => r.Name.Contains("LogEntry")).GetMethod("RemoveLogEntriesByMode", BindingFlags.Static | BindingFlags.NonPublic);



        public static class EditorPrefsCached
        {
            public static bool GetBool(string key, bool defaultValue = false)
            {
                if (bools_byKey.ContainsKey(key))
                    return bools_byKey[key];
                else
                    return bools_byKey[key] = EditorPrefs.GetBool(key, defaultValue);

            }
            public static float GetFloat(string key, float defaultValue = 0)
            {
                if (floats_byKey.ContainsKey(key))
                    return floats_byKey[key];
                else
                    return floats_byKey[key] = EditorPrefs.GetFloat(key, defaultValue);

            }
            public static int GetInt(string key, int defaultValue = 0)
            {
                if (ints_byKey.ContainsKey(key))
                    return ints_byKey[key];
                else
                    return ints_byKey[key] = EditorPrefs.GetInt(key, defaultValue);

            }
            public static string GetString(string key, string defaultValue = "")
            {
                if (strings_byKey.ContainsKey(key))
                    return strings_byKey[key];
                else
                    return strings_byKey[key] = EditorPrefs.GetString(key, defaultValue);

            }


            public static void SetBool(string key, bool value)
            {
                bools_byKey[key] = value;

                EditorPrefs.SetBool(key, value);

            }
            public static void SetFloat(string key, float value)
            {
                floats_byKey[key] = value;

                EditorPrefs.SetFloat(key, value);

            }
            public static void SetInt(string key, int value)
            {
                ints_byKey[key] = value;

                EditorPrefs.SetInt(key, value);

            }
            public static void SetString(string key, string value)
            {
                strings_byKey[key] = value;

                EditorPrefs.SetString(key, value);

            }


            static Dictionary<string, bool> bools_byKey = new();
            static Dictionary<string, float> floats_byKey = new();
            static Dictionary<string, int> ints_byKey = new();
            static Dictionary<string, string> strings_byKey = new();

        }


#endif





        #endregion

    }

    public static partial class VGUI
    {
        #region Colors

        public static class GUIColors
        {
            public static Color windowBackground => isDarkTheme ? Greyscale(.22f) : Greyscale(.78f); // prev backgroundCol
            public static Color pressedButtonBackground => isDarkTheme ? new Color(.48f, .76f, 1f, 1f) * 1.4f : new Color(.48f, .7f, 1f, 1f) * 1.2f; // prev pressedButtonCol
            public static Color greyedOutTint => Greyscale(.7f);
            public static Color selectedBackground => isDarkTheme ? new Color(.17f, .365f, .535f) : new Color(.2f, .375f, .555f) * 1.2f;
        }


        #endregion

        #region Shortcuts

        public static Rect lastRect => GUILayoutUtility.GetLastRect();

        public static bool isDarkTheme => EditorGUIUtility.isProSkin;

        public static float GetLabelWidth(this string s) => GUI.skin.label.CalcSize(new GUIContent(s)).x;
        public static float GetLabelWidth(this string s, int fotSize)
        {
            SetLabelFontSize(fotSize);

            var r = s.GetLabelWidth();

            ResetLabelStyle();

            return r;

        }
        public static float GetLabelWidth(this string s, bool isBold)
        {
            if (isBold)
                SetLabelBold();

            var r = s.GetLabelWidth();

            if (isBold)
                ResetLabelStyle();

            return r;

        }

        public static void SetGUIEnabled(bool enabled) { _prevGuiEnabled = GUI.enabled; GUI.enabled = enabled; }
        public static void ResetGUIEnabled() => GUI.enabled = _prevGuiEnabled;
        static bool _prevGuiEnabled = true;

        public static void SetLabelFontSize(int size) => GUI.skin.label.fontSize = size;
        public static void SetLabelBold() => GUI.skin.label.fontStyle = FontStyle.Bold;
        public static void SetLabelAlignmentCenter() => GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        public static void ResetLabelStyle()
        {
            GUI.skin.label.fontSize = 0;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
        }


        public static void SetGUIColor(Color c)
        {
            if (!_guiColorModified)
                _defaultGuiColor = GUI.color;

            _guiColorModified = true;

            GUI.color = _defaultGuiColor * c;

        }
        public static void ResetGUIColor()
        {
            GUI.color = _guiColorModified ? _defaultGuiColor : Color.white;

            _guiColorModified = false;

        }
        static bool _guiColorModified;
        static Color _defaultGuiColor;



        #endregion

        #region Events


        public class WrappedEvent
        {
            public Event e;

            public bool isRepaint => e.type == EventType.Repaint;
            public bool isLayout => e.type == EventType.Layout;
            public bool isUsed => e.type == EventType.Used;
            public bool isMouseLeaveWindow => e.type == EventType.MouseLeaveWindow;
            public bool isMouseEnterWindow => e.type == EventType.MouseEnterWindow;
            public bool isContextClick => e.type == EventType.ContextClick;

            public bool isKeyDown => e.type == EventType.KeyDown;
            public bool isKeyUp => e.type == EventType.KeyUp;
            public KeyCode keyCode => e.keyCode;
            public char characted => e.character;

            public bool isExecuteCommand => e.type == EventType.ExecuteCommand;
            public string commandName => e.commandName;

            public bool isMouse => e.isMouse;
            public bool isMouseDown => e.type == EventType.MouseDown;
            public bool isMouseUp => e.type == EventType.MouseUp;
            public bool isMouseDrag => e.type == EventType.MouseDrag;
            public bool isMouseMove => e.type == EventType.MouseMove;
            public bool isScroll => e.type == EventType.ScrollWheel;
            public int mouseButton => e.button;
            public int clickCount => e.clickCount;
            public Vector2 mousePosition => e.mousePosition;
            public Vector2 mousePosition_screenSpace => GUIUtility.GUIToScreenPoint(e.mousePosition);
            public Vector2 mouseDelta => e.delta;

            public bool isDragUpdate => e.type == EventType.DragUpdated;
            public bool isDragPerform => e.type == EventType.DragPerform;
            public bool isDragExit => e.type == EventType.DragExited;

            public EventModifiers modifiers => e.modifiers;
            public bool holdingAnyModifierKey => modifiers != EventModifiers.None;

            public bool holdingAlt => e.alt;
            public bool holdingShift => e.shift;
            public bool holdingCtrl => e.control;
            public bool holdingCmd => e.command;
            public bool holdingCmdOrCtrl => e.command || e.control;

            public bool holdingAltOnly => e.modifiers == EventModifiers.Alt;        // in some sessions FunctionKey is always pressed?
            public bool holdingShiftOnly => e.modifiers == EventModifiers.Shift;        // in some sessions FunctionKey is always pressed?
            public bool holdingCtrlOnly => e.modifiers == EventModifiers.Control;
            public bool holdingCmdOnly => e.modifiers == EventModifiers.Command;
            public bool holdingCmdOrCtrlOnly => (e.modifiers == EventModifiers.Command || e.modifiers == EventModifiers.Control);

            public EventType type => e.type;

            public void Use() => e?.Use();


            public WrappedEvent(Event e) => this.e = e;

            public override string ToString() => e.ToString();

        }

        public static WrappedEvent Wrap(this Event e) => new(e);

        public static WrappedEvent curEvent => _curEvent ??= typeof(Event).GetFieldValue<Event>("s_Current").Wrap();
        static WrappedEvent _curEvent;





        #endregion

        #region Layout



        public static void BeginPanel(string title, float height, System.Action onClose = null, System.Action onApply = null)
        {

            void bg()
            {
                GUI.enabled = false;
                SetGUIColor(Greyscale(.75f));
                var r = ExpandWidthLabelRect(0).SetHeight(height);
                GUI.Button(r, "");
                GUI.Button(r, "");
                GUI.Button(r, "");
                GUI.Button(r, "");
                GUI.Button(r, "");
                GUI.Button(r, "");
                GUI.Button(r, "");
                GUI.Button(r, "");
                ResetGUIColor();
                GUI.enabled = true;
            }

            void layout()
            {

                GUILayout.BeginHorizontal();
                Space(7);
                EditorGUIUtility.labelWidth -= 7;
                GUILayout.BeginVertical();

            }

            void title_()
            {
                Space(5);
                EditorGUI.PrefixLabel(ExpandWidthLabelRect(), new GUIContent(title));
                // GUI.skin.label.fontStyle = FontStyle.Bold;
                // GUILayout.Label(title);

            }

            void buttons()
            {
                var rClose = lastRect.SetWidthFromRight(16).MoveX(1).MoveY(-1);

                // if()
                GUI.color = Greyscale(rClose.IsHovered() ? .9f : .6f);
                GUI.Label(rClose, EditorGUIUtility.IconContent("CrossIcon"));

                GUI.color = Color.clear;
                if (GUI.Button(rClose, ""))
                    onClose?.Invoke();


                var rApply = rClose.MoveX(-19).Resize(-1);

                GUI.color = Greyscale(rApply.IsHovered() ? .9f : .6f);
                GUI.Label(rApply, EditorGUIUtility.IconContent("check"));

                GUI.color = Color.clear;
                if (GUI.Button(rApply, ""))
                    onApply?.Invoke();


                GUI.color = Color.white;

            }



            bg();
            layout();
            title_();
            buttons();

            Space(5);

        }

        public static void EndPanel()
        {
            GUILayout.EndVertical();
            Space(7);
            EditorGUIUtility.labelWidth = 0;
            GUILayout.EndHorizontal();
        }



        public static void BeginIndent(float f)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(f);
            GUILayout.BeginVertical();

            _indentLabelWidthStack.Push(EditorGUIUtility.labelWidth);

            EditorGUIUtility.labelWidth -= f;
        }

        public static void EndIndent(float f = 0)
        {
            GUILayout.EndVertical();
            GUILayout.Space(f);
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = _indentLabelWidthStack.Pop();
        }
        static Stack<float> _indentLabelWidthStack = new Stack<float>();




        #endregion

        #region Drawing

        public static Rect Draw(this Rect r) { EditorGUI.DrawRect(r, Color.black); return r; }
        public static Rect Draw(this Rect r, Color c) { EditorGUI.DrawRect(r, c); return r; }



        public static Rect DrawWithRoundedCorners(this Rect rect, Color color, int cornerRadius)
        {
            if (!curEvent.isRepaint) return rect;

            cornerRadius = cornerRadius.Min((rect.height / 2).FloorToInt()).Min((rect.width / 2).FloorToInt());

            GUIStyle style;

            void getStyle()
            {
                if (_roundedStylesByCornerRadius.TryGetValue(cornerRadius, out style)) return;

                var pixelsPerPoint = 2;

                var res = cornerRadius * 2 * pixelsPerPoint;
                var pixels = new Color[res * res];

                var white = Greyscale(1, 1);
                var clear = Greyscale(1, 0);
                var halfRes = res / 2;

                for (int x = 0; x < res; x++)
                    for (int y = 0; y < res; y++)
                    {
                        var sqrMagnitude = (new Vector2(x - halfRes + .5f, y - halfRes + .5f)).sqrMagnitude;
                        pixels[x + y * res] = sqrMagnitude <= halfRes * halfRes ? white : clear;
                    }

                var texture = new Texture2D(res, res);
                texture.SetPropertyValue("pixelsPerPoint", pixelsPerPoint);
                texture.hideFlags = HideFlags.DontSave;
                texture.SetPixels(pixels);
                texture.Apply();



                style = new GUIStyle();
                style.normal.background = texture;
                style.alignment = TextAnchor.MiddleCenter;
                style.border = new RectOffset(cornerRadius, cornerRadius, cornerRadius, cornerRadius);


                _roundedStylesByCornerRadius[cornerRadius] = style;

            }
            void draw()
            {
                SetGUIColor(color);

                style.Draw(rect, false, false, false, false);

                ResetGUIColor();

            }

            getStyle();
            draw();

            return rect;

        }
        public static Rect DrawWithRoundedCorners(this Rect rect, Color color, float cornerRadius) => rect.DrawWithRoundedCorners(color, cornerRadius.RoundToInt());
        static Dictionary<int, GUIStyle> _roundedStylesByCornerRadius = new Dictionary<int, GUIStyle>();


        public static Rect DrawBlurred(this Rect rect, Color color, int blurRadius)
        {
            if (!curEvent.isRepaint) return rect;

            var pixelsPerPoint = .5f;
            // var pixelsPerPoint = 1f;

            var blurRadiusScaled = (blurRadius * pixelsPerPoint).RoundToInt().Max(1).Min(123);

            var croppedRectWidth = (rect.width * pixelsPerPoint).RoundToInt().Min(blurRadiusScaled * 2);
            var croppedRectHeight = (rect.height * pixelsPerPoint).RoundToInt().Min(blurRadiusScaled * 2);

            var textureWidth = croppedRectWidth + blurRadiusScaled * 2;
            var textureHeight = croppedRectHeight + blurRadiusScaled * 2;

            GUIStyle style;

            void getStyle()
            {
                if (_blurredStylesByTextureSize.TryGetValue((textureWidth, textureHeight), out style)) return;

                // VDebug.LogStart(blurRadius + "");

                var pixels = new Color[textureWidth * textureHeight];
                var kernel = new GaussianKernel(false, blurRadiusScaled).Array2d();

                for (int x = 0; x < textureWidth; x++)
                    for (int y = 0; y < textureHeight; y++)
                    {
                        var sum = 0f;

                        for (int xSample = (x - blurRadiusScaled).Max(blurRadiusScaled); xSample <= (x + blurRadiusScaled).Min(textureWidth - 1 - blurRadiusScaled); xSample++)
                            for (int ySample = (y - blurRadiusScaled).Max(blurRadiusScaled); ySample <= (y + blurRadiusScaled).Min(textureHeight - 1 - blurRadiusScaled); ySample++)
                                sum += kernel[blurRadiusScaled + xSample - x, blurRadiusScaled + ySample - y];

                        pixels[x + y * textureWidth] = Greyscale(1, sum);

                    }

                var texture = new Texture2D(textureWidth, textureHeight);
                texture.SetPropertyValue("pixelsPerPoint", pixelsPerPoint);
                texture.hideFlags = HideFlags.DontSave;
                texture.SetPixels(pixels);
                texture.Apply();


                style = new GUIStyle();
                style.normal.background = texture;
                style.alignment = TextAnchor.MiddleCenter;

                var borderX = ((textureWidth / 2f - 1) / pixelsPerPoint).FloorToInt();
                var borderY = ((textureHeight / 2f - 1) / pixelsPerPoint).FloorToInt();
                style.border = new RectOffset(borderX, borderX, borderY, borderY);

                _blurredStylesByTextureSize[(textureWidth, textureHeight)] = style;

                // VDebug.LogFinish();

            }
            void draw()
            {
                SetGUIColor(color);

                style.Draw(rect.SetSizeFromMid(rect.width + blurRadius * 2, rect.height + blurRadius * 2), false, false, false, false);

                ResetGUIColor();

            }

            getStyle();
            draw();

            return rect;

        }
        public static Rect DrawBlurred(this Rect rect, Color color, float blurRadius) => rect.DrawBlurred(color, blurRadius.RoundToInt());
        static Dictionary<(int, int), GUIStyle> _blurredStylesByTextureSize = new Dictionary<(int, int), GUIStyle>();


        static void DrawCurtain(this Rect rect, Color color, int dir)
        {
            void genTextures()
            {
                if (_gradientTextures != null) return;

                _gradientTextures = new Texture2D[4];

                // var pixels = Enumerable.Range(0, 256).Select(r => Greyscale(1, r / 255f));
                var pixels = Enumerable.Range(0, 256).Select(r => Greyscale(1, (r / 255f).Smoothstep()));

                var up = new Texture2D(1, 256);
                up.SetPixels(pixels.Reverse().ToArray());
                up.Apply();
                up.hideFlags = HideFlags.DontSave;
                up.wrapMode = TextureWrapMode.Clamp;
                _gradientTextures[0] = up;

                var down = new Texture2D(1, 256);
                down.SetPixels(pixels.ToArray());
                down.Apply();
                down.hideFlags = HideFlags.DontSave;
                down.wrapMode = TextureWrapMode.Clamp;
                _gradientTextures[1] = down;

                var left = new Texture2D(256, 1);
                left.SetPixels(pixels.ToArray());
                left.Apply();
                left.hideFlags = HideFlags.DontSave;
                left.wrapMode = TextureWrapMode.Clamp;
                _gradientTextures[2] = left;

                var right = new Texture2D(256, 1);
                right.SetPixels(pixels.Reverse().ToArray());
                right.Apply();
                right.hideFlags = HideFlags.DontSave;
                right.wrapMode = TextureWrapMode.Clamp;
                _gradientTextures[3] = right;

            }
            void draw()
            {
                SetGUIColor(color);

                GUI.DrawTexture(rect, _gradientTextures[dir]);

                ResetGUIColor();

            }

            genTextures();
            draw();

        }
        public static void DrawCurtainUp(this Rect rect, Color color) => rect.DrawCurtain(color, 0);
        public static void DrawCurtainDown(this Rect rect, Color color) => rect.DrawCurtain(color, 1);
        public static void DrawCurtainLeft(this Rect rect, Color color) => rect.DrawCurtain(color, 2);
        public static void DrawCurtainRight(this Rect rect, Color color) => rect.DrawCurtain(color, 3);
        static Texture2D[] _gradientTextures;



        public static bool IsHovered(this Rect r) => r.Contains(curEvent.mousePosition);

        #endregion

        #region Spacing

        public static void Space(float px = 6) => GUILayout.Space(px);

        public static void Divider(float space = 15, float yOffset = 0)
        {
            GUILayout.Label("", GUILayout.Height(space), GUILayout.ExpandWidth(true));
            lastRect.SetHeightFromMid(1).SetWidthFromMid(lastRect.width - 16).MoveY(yOffset).Draw(isDarkTheme ? Color.white * .42f : Color.white * .72f);
        }

        public static Rect ExpandSpace() { GUILayout.Label("", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)); return lastRect; }

        public static Rect ExpandWidthLabelRect() { GUILayout.Label(""/* , GUILayout.Height(0) */, GUILayout.ExpandWidth(true)); return lastRect; }
        public static Rect ExpandWidthLabelRect(float height) { GUILayout.Label("", GUILayout.Height(height), GUILayout.ExpandWidth(true)); return lastRect; }


        #endregion

        #region Icons

        public static class EditorIcons
        {
            public static Texture2D GetIcon(string iconNameOrPath)
            {
                if (icons_byName.TryGetValue(iconNameOrPath, out var cachedResult)) return cachedResult;

                var icon = typeof(EditorGUIUtility).InvokeMethod<Texture2D>("LoadIcon", iconNameOrPath) as Texture2D;

                return icons_byName[iconNameOrPath] = icon;

            }

            static Dictionary<string, Texture2D> icons_byName = new Dictionary<string, Texture2D>();
        }




        // toremove: 

        public static void DrawIcon(Rect rect, string icon, Color? col = null)
        {
            if (icon == "") return;
            Texture2D tex = (Texture2D)EditorGUIUtility.FindTexture(icon);
            if (!tex) tex = (Texture2D)EditorGUIUtility.Load(icon);
            if (!tex) tex = (Texture2D)Resources.Load(icon);
            DrawIcon(rect, tex, col);
        }
        public static void DrawIcon(Rect rect, string icon, bool greyedOut) => DrawIcon(rect, icon, greyedOut ? GUIColors.greyedOutTint : (Color?)null);

        public static void DrawIcon(Rect rect, Texture2D tex, Color? col = null)
        {
            var color = Color.white;
            if (col != null) color = col.GetValueOrDefault();

            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
        }
        public static void DrawIcon(Rect rect, Texture2D tex, bool greyedOut) => DrawIcon(rect, tex, greyedOut ? GUIColors.greyedOutTint : (Color?)null);

        public static void DrawIconForGuid(Rect rect, string guid, bool greyedOut = false)
        {
            if (!guid.IsValidGuid()) return;

            var type = AssetDatabase.GetMainAssetTypeAtPath(guid.ToPath());

            if (AssetDatabase.IsValidFolder(guid.ToPath()))
                DrawIcon(rect, "Folder Icon", greyedOut);

            else if (guid.ToPath().GetExtension() == ".cs")
                DrawIcon(rect, "cs Script Icon", greyedOut);

            else DrawIcon(rect, AssetPreview.GetMiniTypeThumbnail(type), greyedOut);
        }

        // public static void DrawIcon(string icon, bool greyedOut = false, bool pressed = false) => DrawIcon(lastRect, icon, greyedOut, pressed);
        // public static void DrawIcon(Rect rect, string icon, bool greyedOut = false, bool pressed = false)
        // {
        //     if (icon == "") return;
        //     Texture2D tex = (Texture2D)EditorGUIUtility.FindTexture(icon);
        //     if (!tex) tex = (Texture2D)EditorGUIUtility.Load(icon);
        //     if (!tex) tex = (Texture2D)Resources.Load(icon);
        //     DrawIcon(rect, tex, greyedOut, pressed);
        // }
        // public static void DrawIcon(Rect rect, GUIContent tex, bool greyedOut = false) //some icons get fucked up if drawn differently
        // {
        //     var r = GUI.enabled;
        //     if (greyedOut) GUI.enabled = false;
        //     var s = new GUIStyle();
        //     GUI.Label(rect.Resize(0), tex, s);
        //     GUI.enabled = r;
        // }
        // public static void DrawIcon(Rect rect, Texture2D tex, bool greyedOut = false, bool pressed = false)
        // {
        //     var col = Color.white;
        //     if (greyedOut) col *= greyedOutColor;
        //     if (pressed) col *= pressedCol;
        //     GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, col, 0, 0);
        // }
        // public static void DrawIcon(Rect rect, string icon, Color col) => DrawIcon(lastRect, EditorGUIUtility.FindTexture(icon), col);
        // public static void DrawIcon(Rect rect, Texture2D tex, Color col) => GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, col, 0, 0);



        #endregion

        #region Other 

        public static void MarkInteractive(this Rect rect)
        {
            if (!curEvent.isRepaint) return;

            var unclippedRect = (Rect)_mi_GUIClip_UnclipToWindow.Invoke(null, new object[] { rect });

            var curGuiView = _pi_GUIView_current.GetValue(null);

            _mi_GUIView_MarkHotRegion.Invoke(curGuiView, new object[] { unclippedRect });

        }
        static PropertyInfo _pi_GUIView_current = typeof(Editor).Assembly.GetType("UnityEditor.GUIView").GetProperty("current", maxBindingFlags);
        static MethodInfo _mi_GUIView_MarkHotRegion = typeof(Editor).Assembly.GetType("UnityEditor.GUIView").GetMethod("MarkHotRegion", maxBindingFlags);
        static MethodInfo _mi_GUIClip_UnclipToWindow = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip").GetMethod("UnclipToWindow", maxBindingFlags, null, new[] { typeof(Rect) }, null);



        #endregion


    }
}
#endif