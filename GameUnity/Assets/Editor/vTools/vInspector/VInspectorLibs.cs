#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.UIElements;
using Type = System.Type;
using static VInspector.Libs.VUtils;


namespace VInspector.Libs
{
    public static class VUtils
    {

        #region Reflection


        public static object GetFieldValue(this object o, string fieldName)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(fieldName) is FieldInfo fieldInfo)
                return fieldInfo.GetValue(target);


            throw new System.Exception($"Field '{fieldName}' not found in type '{type.Name}' and its parent types");

        }
        public static object GetPropertyValue(this object o, string propertyName)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetPropertyInfo(propertyName) is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(target);


            throw new System.Exception($"Property '{propertyName}' not found in type '{type.Name}' and its parent types");

        }
        public static object GetMemberValue(this object o, string memberName)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(memberName) is FieldInfo fieldInfo)
                return fieldInfo.GetValue(target);

            if (type.GetPropertyInfo(memberName) is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(target);


            throw new System.Exception($"Member '{memberName}' not found in type '{type.Name}' and its parent types");

        }

        public static void SetFieldValue(this object o, string fieldName, object value)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(fieldName) is FieldInfo fieldInfo)
                fieldInfo.SetValue(target, value);


            else throw new System.Exception($"Field '{fieldName}' not found in type '{type.Name}' and its parent types");

        }
        public static void SetPropertyValue(this object o, string propertyName, object value)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetPropertyInfo(propertyName) is PropertyInfo propertyInfo)
                propertyInfo.SetValue(target, value);


            else throw new System.Exception($"Property '{propertyName}' not found in type '{type.Name}' and its parent types");

        }
        public static void SetMemberValue(this object o, string memberName, object value)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetFieldInfo(memberName) is FieldInfo fieldInfo)
                fieldInfo.SetValue(target, value);

            else if (type.GetPropertyInfo(memberName) is PropertyInfo propertyInfo)
                propertyInfo.SetValue(target, value);


            else throw new System.Exception($"Member '{memberName}' not found in type '{type.Name}' and its parent types");

        }

        public static object InvokeMethod(this object o, string methodName, params object[] parameters) // todo handle null params (can't get their type)
        {
            var type = o as Type ?? o.GetType();
            var target = o is Type ? null : o;


            if (type.GetMethodInfo(methodName, parameters.Select(r => r.GetType()).ToArray()) is MethodInfo methodInfo)
                return methodInfo.Invoke(target, parameters);


            throw new System.Exception($"Method '{methodName}' not found in type '{type.Name}', its parent types and interfaces");

        }


        public static T GetFieldValue<T>(this object o, string fieldName) => (T)o.GetFieldValue(fieldName);
        public static T GetPropertyValue<T>(this object o, string propertyName) => (T)o.GetPropertyValue(propertyName);
        public static T GetMemberValue<T>(this object o, string memberName) => (T)o.GetMemberValue(memberName);
        public static T InvokeMethod<T>(this object o, string methodName, params object[] parameters) => (T)o.InvokeMethod(methodName, parameters);




        public static FieldInfo GetFieldInfo(this Type type, string fieldName)
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
        public static PropertyInfo GetPropertyInfo(this Type type, string propertyName)
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
        public static MethodInfo GetMethodInfo(this Type type, string methodName, params Type[] argumentTypes)
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

        static Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfoCache = new();
        static Dictionary<Type, Dictionary<string, PropertyInfo>> propertyInfoCache = new();
        static Dictionary<Type, Dictionary<int, MethodInfo>> methodInfoCache = new();






        public static T GetCustomAttributeCached<T>(this MemberInfo memberInfo) where T : System.Attribute
        {
            if (!attributesCache.TryGetValue(memberInfo, out var attributes_byType))
                attributes_byType = attributesCache[memberInfo] = new();

            if (!attributes_byType.TryGetValue(typeof(T), out var attribute))
                attribute = attributes_byType[typeof(T)] = memberInfo.GetCustomAttribute<T>();

            return attribute as T;

        }

        static Dictionary<MemberInfo, Dictionary<Type, System.Attribute>> attributesCache = new();






        public static List<Type> GetSubclasses(this Type t) => t.Assembly.GetTypes().Where(type => type.IsSubclassOf(t)).ToList();

        public static object GetDefaultValue(this FieldInfo f, params object[] constructorVars) => f.GetValue(System.Activator.CreateInstance(((MemberInfo)f).ReflectedType, constructorVars));
        public static object GetDefaultValue(this FieldInfo f) => f.GetValue(System.Activator.CreateInstance(((MemberInfo)f).ReflectedType));

        public static IEnumerable<FieldInfo> GetFieldsWithoutBase(this Type t) => t.GetFields().Where(r => !t.BaseType.GetFields().Any(rr => rr.Name == r.Name));
        public static IEnumerable<PropertyInfo> GetPropertiesWithoutBase(this Type t) => t.GetProperties().Where(r => !t.BaseType.GetProperties().Any(rr => rr.Name == r.Name));


        public const BindingFlags maxBindingFlags = (BindingFlags)62;








        #endregion

        #region Collections


        public static T NextTo<T>(this IEnumerable<T> e, T to) => e.SkipWhile(r => !r.Equals(to)).Skip(1).FirstOrDefault();
        public static T PreviousTo<T>(this IEnumerable<T> e, T to) => e.Reverse().SkipWhile(r => !r.Equals(to)).Skip(1).FirstOrDefault();
        public static T NextToOrFirst<T>(this IEnumerable<T> e, T to) => e.NextTo(to) ?? e.First();
        public static T PreviousToOrLast<T>(this IEnumerable<T> e, T to) => e.PreviousTo(to) ?? e.Last();

        public static IEnumerable<T> InsertFirst<T>(this IEnumerable<T> ie, T t) => new[] { t }.Concat(ie);

        public static int IndexOfFirst<T>(this List<T> list, System.Func<T, bool> f) => list.FirstOrDefault(f) is T t ? list.IndexOf(t) : -1;
        public static int IndexOfLast<T>(this List<T> list, System.Func<T, bool> f) => list.LastOrDefault(f) is T t ? list.IndexOf(t) : -1;

        public static void SortBy<T, T2>(this List<T> list, System.Func<T, T2> keySelector) where T2 : System.IComparable => list.Sort((q, w) => keySelector(q).CompareTo(keySelector(w)));

        public static void RemoveValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value)
        {
            if (dictionary.FirstOrDefault(r => r.Value.Equals(value)) is var kvp)
                dictionary.Remove(kvp);
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, System.Action<T> action) { foreach (T item in sequence) action(item); }



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






        #endregion

        #region Math


        public static class MathUtil // MathUtils name is taken by UnityEditor.MathUtils 
        {

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




            public static float Lerp(float f1, float f2, float t) => Mathf.LerpUnclamped(f1, f2, t);
            public static float Lerp(ref float f1, float f2, float t)
            {
                return f1 = Lerp(f1, f2, t);
            }

            public static Vector2 Lerp(Vector2 f1, Vector2 f2, float t) => Vector2.LerpUnclamped(f1, f2, t);
            public static Vector2 Lerp(ref Vector2 f1, Vector2 f2, float t)
            {
                return f1 = Lerp(f1, f2, t);
            }

            public static Vector3 Lerp(Vector3 f1, Vector3 f2, float t) => Vector3.LerpUnclamped(f1, f2, t);
            public static Vector3 Lerp(ref Vector3 f1, Vector3 f2, float t)
            {
                return f1 = Lerp(f1, f2, t);
            }

            public static Color Lerp(Color f1, Color f2, float t) => Color.LerpUnclamped(f1, f2, t);
            public static Color Lerp(ref Color f1, Color f2, float t)
            {
                return f1 = Lerp(f1, f2, t);
            }


            public static float Lerp(float current, float target, float speed, float deltaTime) => Mathf.Lerp(current, target, GetLerpT(speed, deltaTime));
            public static float Lerp(ref float current, float target, float speed, float deltaTime)
            {
                return current = Lerp(current, target, speed, deltaTime);
            }

            public static Vector2 Lerp(Vector2 current, Vector2 target, float speed, float deltaTime) => Vector2.Lerp(current, target, GetLerpT(speed, deltaTime));
            public static Vector2 Lerp(ref Vector2 current, Vector2 target, float speed, float deltaTime)
            {
                return current = Lerp(current, target, speed, deltaTime);
            }

            public static Vector3 Lerp(Vector3 current, Vector3 target, float speed, float deltaTime) => Vector3.Lerp(current, target, GetLerpT(speed, deltaTime));
            public static Vector3 Lerp(ref Vector3 current, Vector3 target, float speed, float deltaTime)
            {
                return current = Lerp(current, target, speed, deltaTime);
            }

            public static float SmoothDamp(float current, float target, float speed, ref float derivative, float deltaTime, float maxSpeed) => Mathf.SmoothDamp(current, target, ref derivative, .5f / speed, maxSpeed, deltaTime);
            public static float SmoothDamp(float current, float target, float speed, ref float derivative, float deltaTime)
            {
                return Mathf.SmoothDamp(current, target, ref derivative, .5f / speed, Mathf.Infinity, deltaTime);
            }
            public static float SmoothDamp(float current, float target, float speed, ref float derivative)
            {
                return SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
            }
            public static float SmoothDamp(ref float current, float target, float speed, ref float derivative, float deltaTime, float maxSpeed)
            {
                return current = SmoothDamp(current, target, speed, ref derivative, deltaTime, maxSpeed);
            }
            public static float SmoothDamp(ref float current, float target, float speed, ref float derivative, float deltaTime)
            {
                return current = SmoothDamp(current, target, speed, ref derivative, deltaTime);
            }
            public static float SmoothDamp(ref float current, float target, float speed, ref float derivative)
            {
                return current = SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
            }

            public static Vector2 SmoothDamp(Vector2 current, Vector2 target, float speed, ref Vector2 derivative, float deltaTime) => Vector2.SmoothDamp(current, target, ref derivative, .5f / speed, Mathf.Infinity, deltaTime);
            public static Vector2 SmoothDamp(Vector2 current, Vector2 target, float speed, ref Vector2 derivative)
            {
                return SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
            }
            public static Vector2 SmoothDamp(ref Vector2 current, Vector2 target, float speed, ref Vector2 derivative, float deltaTime)
            {
                return current = SmoothDamp(current, target, speed, ref derivative, deltaTime);
            }
            public static Vector2 SmoothDamp(ref Vector2 current, Vector2 target, float speed, ref Vector2 derivative)
            {
                return current = SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
            }

            public static Vector3 SmoothDamp(Vector3 current, Vector3 target, float speed, ref Vector3 derivative, float deltaTime) => Vector3.SmoothDamp(current, target, ref derivative, .5f / speed, Mathf.Infinity, deltaTime);
            public static Vector3 SmoothDamp(Vector3 current, Vector3 target, float speed, ref Vector3 derivative)
            {
                return SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
            }
            public static Vector3 SmoothDamp(ref Vector3 current, Vector3 target, float speed, ref Vector3 derivative, float deltaTime)
            {
                return current = SmoothDamp(current, target, speed, ref derivative, deltaTime);
            }
            public static Vector3 SmoothDamp(ref Vector3 current, Vector3 target, float speed, ref Vector3 derivative)
            {
                return current = SmoothDamp(current, target, speed, ref derivative, Time.deltaTime);
            }


            public static float GetLerpT(float lerpSpeed, float deltaTime) => 1 - Mathf.Exp(-lerpSpeed * 2f * deltaTime);
            public static float GetLerpT(float lerpSpeed)
            {
                return GetLerpT(lerpSpeed, Time.deltaTime);
            }



        }


        public static float DistanceTo(this float f1, float f2) => Mathf.Abs(f1 - f2);
        public static float DistanceTo(this Vector2 f1, Vector2 f2) => (f1 - f2).magnitude;
        public static float DistanceTo(this Vector3 f1, Vector3 f2) => (f1 - f2).magnitude;

        public static float Sign(this float f) => f == 0 ? 0 : Mathf.Sign(f);

        public static int Abs(this int f) => Mathf.Abs(f);
        public static float Abs(this float f) => Mathf.Abs(f);

        public static int Clamp(this int f, int f0, int f1) => Mathf.Clamp(f, f0, f1);
        public static float Clamp(this float f, float f0, float f1) => Mathf.Clamp(f, f0, f1);

        public static float Clamp01(this float f) => Mathf.Clamp(f, 0, 1);
        public static Vector2 Clamp01(this Vector2 f) => new(f.x.Clamp01(), f.y.Clamp01());
        public static Vector3 Clamp01(this Vector3 f) => new(f.x.Clamp01(), f.y.Clamp01(), f.z.Clamp01());


        public static int Pow(this int f, int pow) => (int)Mathf.Pow(f, pow);
        public static float Pow(this float f, float pow) => Mathf.Pow(f, pow);

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

        public static int Max(this int f, int ff) => Mathf.Max(f, ff);
        public static int Min(this int f, int ff) => Mathf.Min(f, ff);
        public static float Max(this float f, float ff) => Mathf.Max(f, ff);
        public static float Min(this float f, float ff) => Mathf.Min(f, ff);

        public static float Loop(this float f, float boundMin, float boundMax)
        {
            while (f < boundMin) f += boundMax - boundMin;
            while (f > boundMax) f -= boundMax - boundMin;
            return f;
        }
        public static float Loop(this float f, float boundMax) => f.Loop(0, boundMax);

        public static float PingPong(this float f, float boundMin, float boundMax) => boundMin + Mathf.PingPong(f - boundMin, boundMax - boundMin);
        public static float PingPong(this float f, float boundMax) => f.PingPong(0, boundMax);


        public static float ProjectOn(this Vector2 v, Vector2 on) => Vector3.Project(v, on).magnitude;
        public static float ProjectOn(this Vector3 v, Vector3 on) => Vector3.Project(v, on).magnitude;

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

        public static bool Approx(this float f1, float f2) => Mathf.Approximately(f1, f2);



        [System.Serializable]
        public class GaussianKernel
        {
            public static float[,] GenerateArray(int size, float sharpness = .5f)
            {
                float[,] kr = new float[size, size];

                if (size == 1) { kr[0, 0] = 1; return kr; }


                var sigma = 1f - Mathf.Pow(sharpness, .1f) * .99999f;
                var radius = (size / 2f).FloorToInt();


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

            public float[,] Array2d() // todo test and use GenerateArray
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

        #region Rects


        public static Rect Resize(this Rect rect, float px) { rect.x += px; rect.y += px; rect.width -= px * 2; rect.height -= px * 2; return rect; }

        public static Rect SetPos(this Rect rect, Vector2 v) => rect.SetPos(v.x, v.y);
        public static Rect SetPos(this Rect rect, float x, float y) { rect.x = x; rect.y = y; return rect; }

        public static Rect SetX(this Rect rect, float x) => rect.SetPos(x, rect.y);
        public static Rect SetY(this Rect rect, float y) => rect.SetPos(rect.x, y);
        public static Rect SetXMax(this Rect rect, float xMax) { rect.xMax = xMax; return rect; }
        public static Rect SetYMax(this Rect rect, float yMax) { rect.yMax = yMax; return rect; }

        public static Rect SetMidPos(this Rect r, Vector2 v) => r.SetPos(v).MoveX(-r.width / 2).MoveY(-r.height / 2);
        public static Rect SetMidPos(this Rect r, float x, float y) => r.SetMidPos(new Vector2(x, y));

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

        #region Colors


        public class ColorUtils
        {
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

        }


        public static Color Greyscale(float brightness, float alpha = 1) => new(brightness, brightness, brightness, alpha);

        public static Color SetAlpha(this Color color, float alpha) { color.a = alpha; return color; }
        public static Color MultiplyAlpha(this Color color, float k) { color.a *= k; return color; }





        #endregion

        #region Objects


        public static void Destroy(this Object r)
        {
            if (Application.isPlaying)
                Object.Destroy(r);
            else
                Object.DestroyImmediate(r);

        }

        public static void DestroyImmediate(this Object o) => Object.DestroyImmediate(o);





        #endregion

        #region GameObjects


        public static bool IsPrefab(this GameObject go) => go.scene.name == null || go.scene.name == go.name;

        public static Bounds GetBounds(this GameObject go, bool local = false)
        {
            Bounds bounds = default;

            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var b = local ? r.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds : r.bounds;

                if (bounds == default)
                    bounds = b;
                else
                    bounds.Encapsulate(b);
            }

            foreach (var r in go.GetComponentsInChildren<Terrain>())
            {
                var b = local ? new Bounds(r.terrainData.size / 2, r.terrainData.size) : new Bounds(r.transform.position + r.terrainData.size / 2, r.terrainData.size);

                if (bounds == default)
                    bounds = b;
                else
                    bounds.Encapsulate(new Bounds(r.transform.position + r.terrainData.size / 2, r.terrainData.size));

            }

            if (bounds == default)
                bounds.center = go.transform.position;

            return bounds;
        }





        #endregion

        #region Text


        public static class TextUtils
        {
            public static string FormatDistance(float meters)
            {
                int m = (int)meters;

                if (m < 1000)
                    return m + " m";
                else
                    return (m / 1000) + "." + (m / 100) % 10 + " km";

            }
            public static string FormatLong(long l) => System.String.Format("{0:n0}", l);
            public static string FormatInt(int l) => FormatLong((long)l);
            public static string FormatTime(long ms, bool includeMs = false)
            {
                System.TimeSpan t = System.TimeSpan.FromMilliseconds(ms);
                var s = "";
                if (t.Hours != 0) s += " " + t.Hours + " hour" + GetCountSuffix(t.Hours);
                if (t.Minutes != 0) s += " " + t.Minutes + " minute" + GetCountSuffix(t.Minutes);
                if (t.Seconds != 0) s += " " + t.Seconds + " second" + GetCountSuffix(t.Seconds);
                if (t.Milliseconds != 0 && includeMs) s += " " + t.Milliseconds + " millisecond" + GetCountSuffix(t.Milliseconds);

                if (s == "")
                    if (includeMs) s = "0 milliseconds";
                    else s = "0 seconds";

                return s.Trim();
            }
            public static string FormatFileSize(long bytes, bool sizeUnknownIfNotMoreThanZero = false)
            {
                if (sizeUnknownIfNotMoreThanZero && bytes == 0) return "Size unknown";

                var ss = new[] { "B", "KB", "MB", "GB", "TB" };
                var bprev = bytes;
                int i = 0;
                while (bytes >= 1024 && i++ < ss.Length - 1) bytes = (bprev = bytes) / 1024;

                if (bytes < 0) return "? B";
                if (i < 3) return string.Format("{0:0.#} ", bytes) + ss[i];
                return string.Format("{0:0.##} ", bytes) + ss[i];
            }

            static string GetCountSuffix(long c) => c % 10 != 1 ? "s" : "";

            public static string GetLoremIpsum(int words = 2)
            {
                var s = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur Excepteur sint occaecat cupidatat non proident sunt in culpa qui officia deserunt mollit anim id est laborum";
                var ws = s.Split(' ').Select(r => r.ToLower().Trim(new[] { ',', '.' }));
                ws = ws.OrderBy(r => UnityEngine.Random.Range(0, 1232)).Take(words);
                var ss = string.Join(" ", ws);
                return char.ToUpper(ss[0]) + ss.Substring(1);
            }

        }


        public static bool IsEmpty(this string s) => s == "";
        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

        public static bool IsLower(this char c) => System.Char.IsLower(c);
        public static bool IsUpper(this char c) => System.Char.IsUpper(c);



        public static string Decamelcase(this string s)
        {
            return Regex.Replace(Regex.Replace(s, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
        }
        public static string FormatVariableName(this string s, bool lowercaseFollowingWords = true)
        {
            return string.Join(" ", s.Decamelcase()
                         .Split(' ')
                         .Select(r => new[] { "", "and", "or", "with", "without", "by", "from" }.Contains(r.ToLower()) || (lowercaseFollowingWords && !s.Trim().StartsWith(r)) ? r.ToLower()
                                                                                                                                                                                 : r.Substring(0, 1).ToUpper() + r.Substring(1))).Trim(' ');
        }

        public static string Remove(this string s, string toRemove)
        {
            if (toRemove == "") return s;
            return s.Replace(toRemove, "");
        }






        #endregion

        #region Paths


        public static bool HasParentPath(this string path) => path.LastIndexOf('/') > 0;
        public static string GetParentPath(this string path) => path.HasParentPath() ? path.Substring(0, path.LastIndexOf('/')) : "";

        public static string GetFilename(this string path, bool withExtension = false) => withExtension ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
        public static string GetExtension(this string path) => Path.GetExtension(path);


        public static string ToGlobalPath(this string localPath) => Application.dataPath + "/" + localPath.Substring(0, localPath.Length - 1);
        public static string ToLocalPath(this string globalPath) => "Assets" + globalPath.Replace(Application.dataPath, "");



        public static string CombinePath(this string p1, string p2, bool useBackslashOnWindows = false)
        {
            if (useBackslashOnWindows) // false by default because all paths in unity use forward slashes, even on Windows
                return Path.Combine(p1, p2);
            else
                return Path.Combine(p1, p2).Replace('\\', '/');

        }

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


        public static string ToGuid(this string pathInProject) => AssetDatabase.AssetPathToGUID(pathInProject);
        public static List<string> ToGuids(this IEnumerable<string> pathsInProject) => pathsInProject.Select(r => r.ToGuid()).ToList();

        public static string GetPath(this Object o) => AssetDatabase.GetAssetPath(o);
        public static string GetGuid(this Object o) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o));

        public static string GetScriptPath(string scriptName) => AssetDatabase.FindAssets("t: script " + scriptName, null).FirstOrDefault()?.ToPath() ?? "scirpt not found";


        public static bool IsValidGuid(this string guid) => AssetDatabase.AssetPathToGUID(AssetDatabase.GUIDToAssetPath(guid), AssetPathToGUIDOptions.OnlyExistingAssets) != "";


        public static T Reimport<T>(this T t) where T : Object { AssetDatabase.ImportAsset(t.GetPath(), ImportAssetOptions.ForceUpdate); return t; }



#endif





        #endregion

        #region Serialization


        [System.Serializable]
        public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        {
            [SerializeField] List<TKey> keys = new();
            [SerializeField] List<TValue> values = new();

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


#if UNITY_EDITOR

        public static object GetBoxedValue(this SerializedProperty p)
        {

#if UNITY_2022_1_OR_NEWER
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                    switch (p.numericType)
                    {
                        case SerializedPropertyNumericType.Int8: return (sbyte)p.intValue;
                        case SerializedPropertyNumericType.UInt8: return (byte)p.uintValue;
                        case SerializedPropertyNumericType.Int16: return (short)p.intValue;
                        case SerializedPropertyNumericType.UInt16: return (ushort)p.uintValue;
                        case SerializedPropertyNumericType.Int32: return p.intValue;
                        case SerializedPropertyNumericType.UInt32: return p.uintValue;
                        case SerializedPropertyNumericType.Int64: return p.longValue;
                        case SerializedPropertyNumericType.UInt64: return p.ulongValue;
                        default: return p.intValue;

                    }

                case SerializedPropertyType.Float:
                    if (p.numericType == SerializedPropertyNumericType.Double)
                        return p.doubleValue;
                    else
                        return p.floatValue;

                case SerializedPropertyType.Hash128: return p.hash128Value;
                case SerializedPropertyType.Character: return (ushort)p.uintValue;
                case SerializedPropertyType.Gradient: return p.gradientValue;
                case SerializedPropertyType.ManagedReference: return p.managedReferenceValue;


            }
#endif

            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: return p.intValue;
                case SerializedPropertyType.Float: return p.floatValue;
                case SerializedPropertyType.Vector2: return p.vector2Value;
                case SerializedPropertyType.Vector3: return p.vector3Value;
                case SerializedPropertyType.Vector4: return p.vector4Value;
                case SerializedPropertyType.Vector2Int: return p.vector2IntValue;
                case SerializedPropertyType.Vector3Int: return p.vector3IntValue;
                case SerializedPropertyType.Quaternion: return p.quaternionValue;
                case SerializedPropertyType.Rect: return p.rectValue;
                case SerializedPropertyType.RectInt: return p.rectIntValue;
                case SerializedPropertyType.Bounds: return p.boundsValue;
                case SerializedPropertyType.BoundsInt: return p.boundsIntValue;
                case SerializedPropertyType.Enum: return p.enumValueIndex;
                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Color: return p.colorValue;
                case SerializedPropertyType.ArraySize: return p.intValue;
                case SerializedPropertyType.Character: return (ushort)p.intValue;
                case SerializedPropertyType.AnimationCurve: return p.animationCurveValue;
                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
                case SerializedPropertyType.ExposedReference: return p.exposedReferenceValue;
                case SerializedPropertyType.FixedBufferSize: return p.intValue;
                case SerializedPropertyType.LayerMask: return (LayerMask)p.intValue;

            }


            return _noValue;

        }
        public static void SetBoxedValue(this SerializedProperty p, object value)
        {
            if (value == _noValue) return;

            try
            {

#if UNITY_2022_1_OR_NEWER
                switch (p.propertyType)
                {
                    case SerializedPropertyType.ArraySize:
                    case SerializedPropertyType.Integer:
                        if (p.numericType == SerializedPropertyNumericType.UInt64)
                            p.ulongValue = System.Convert.ToUInt64(value);
                        else
                            p.longValue = System.Convert.ToInt64(value);
                        return;

                    case SerializedPropertyType.Float:
                        if (p.numericType == SerializedPropertyNumericType.Double)
                            p.doubleValue = System.Convert.ToDouble(value);
                        else
                            p.floatValue = System.Convert.ToSingle(value);
                        return;

                    case SerializedPropertyType.Character: p.uintValue = System.Convert.ToUInt16(value); return;
                    case SerializedPropertyType.Gradient: p.gradientValue = (Gradient)value; return;
                    case SerializedPropertyType.Hash128: p.hash128Value = (Hash128)value; return;

                }
#endif

                switch (p.propertyType)
                {
                    case SerializedPropertyType.ArraySize:
                    case SerializedPropertyType.Integer: p.intValue = System.Convert.ToInt32(value); return;
                    case SerializedPropertyType.Float: p.floatValue = System.Convert.ToSingle(value); return;
                    case SerializedPropertyType.Vector2: p.vector2Value = (Vector2)value; return;
                    case SerializedPropertyType.Vector3: p.vector3Value = (Vector3)value; return;
                    case SerializedPropertyType.Vector4: p.vector4Value = (Vector4)value; return;
                    case SerializedPropertyType.Vector2Int: p.vector2IntValue = (Vector2Int)value; return;
                    case SerializedPropertyType.Vector3Int: p.vector3IntValue = (Vector3Int)value; return;
                    case SerializedPropertyType.Quaternion: p.quaternionValue = (Quaternion)value; return;
                    case SerializedPropertyType.Rect: p.rectValue = (Rect)value; return;
                    case SerializedPropertyType.RectInt: p.rectIntValue = (RectInt)value; return;
                    case SerializedPropertyType.Bounds: p.boundsValue = (Bounds)value; return;
                    case SerializedPropertyType.BoundsInt: p.boundsIntValue = (BoundsInt)value; return;
                    case SerializedPropertyType.String: p.stringValue = (string)value; return;
                    case SerializedPropertyType.Boolean: p.boolValue = (bool)value; return;
                    case SerializedPropertyType.Enum: p.enumValueIndex = (int)value; return;
                    case SerializedPropertyType.Color: p.colorValue = (Color)value; return;
                    case SerializedPropertyType.AnimationCurve: p.animationCurveValue = (AnimationCurve)value; return;
                    case SerializedPropertyType.ObjectReference: p.objectReferenceValue = (UnityEngine.Object)value; return;
                    case SerializedPropertyType.ExposedReference: p.exposedReferenceValue = (UnityEngine.Object)value; return;
                    case SerializedPropertyType.ManagedReference: p.managedReferenceValue = value; return;

                    case SerializedPropertyType.LayerMask:
                        try
                        {
                            p.intValue = ((LayerMask)value).value; return;
                        }
                        catch (System.InvalidCastException)
                        {
                            p.intValue = System.Convert.ToInt32(value); return;
                        }

                }

            }
            catch { }

        }

        static object _noValue = new();

#endif




        #endregion

        #region GlobalID

#if UNITY_EDITOR

        [System.Serializable]
        public struct GlobalID : System.IEquatable<GlobalID>
        {
            public Object GetObject() => GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            public int GetObjectInstanceId() => GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(globalObjectId);


            public int idType => globalObjectId.identifierType;
            public string guid => globalObjectId.assetGUID.ToString();
            public ulong fileId => globalObjectId.targetObjectId;
            public ulong prefabId => globalObjectId.targetPrefabId;

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




            public GlobalID UnpackForPrefab()
            {
                var unpackedFileId = (this.fileId ^ this.prefabId) & 0x7fffffffffffffff;

                var unpackedGId = new GlobalID($"GlobalObjectId_V1-{this.idType}-{this.guid}-{unpackedFileId}-0");

                return unpackedGId;

            }

        }

        public static GlobalID GetGlobalID(this Object o) => new(o);
        public static GlobalID[] GetGlobalIDs(this IEnumerable<int> instanceIds)
        {
            var unityGlobalIds = new GlobalObjectId[instanceIds.Count()];

            GlobalObjectId.GetGlobalObjectIdsSlow(instanceIds.ToArray(), unityGlobalIds);

            var globalIds = unityGlobalIds.Select(r => new GlobalID(r.ToString()));

            return globalIds.ToArray();

        }

        public static Object[] GetObjects(this IEnumerable<GlobalID> globalIDs)
        {
            var goids = globalIDs.Select(r => r.globalObjectId).ToArray();

            var objects = new Object[goids.Length];

            GlobalObjectId.GlobalObjectIdentifiersToObjectsSlow(goids, objects);

            return objects;

        }
        public static int[] GetObjectInstanceIds(this IEnumerable<GlobalID> globalIDs)
        {
            var goids = globalIDs.Select(r => r.globalObjectId).ToArray();

            var iids = new int[goids.Length];

            GlobalObjectId.GlobalObjectIdentifiersToInstanceIDsSlow(goids, iids);

            return iids;

        }


#endif




        #endregion

        #region Editor

#if UNITY_EDITOR


        public static class EditorUtils
        {

            public static void OpenFolder(string path)
            {
                var folder = AssetDatabase.LoadAssetAtPath(path, typeof(Object));

                var t = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
                var w = (EditorWindow)t.GetField("s_LastInteractedProjectBrowser").GetValue(null);

                var m_ListAreaState = t.GetField("m_ListAreaState", maxBindingFlags).GetValue(w);

                m_ListAreaState.GetType().GetField("m_SelectedInstanceIDs").SetValue(m_ListAreaState, new List<int> { folder.GetInstanceID() });

                t.GetMethod("OpenSelectedFolders", maxBindingFlags).Invoke(null, null);

            }

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




            public static void SetSymbolDefinedInAsmdef(string asmdefName, string symbol, bool defined)
            {
                var isDefined = IsSymbolDefinedInAsmdef(asmdefName, symbol);
                var shouldBeDefined = defined;

                if (shouldBeDefined && !isDefined)
                    DefineSymbolInAsmdef(asmdefName, symbol);

                if (!shouldBeDefined && isDefined)
                    UndefineSymbolInAsmdef(asmdefName, symbol);

            }
            public static bool IsSymbolDefinedInAsmdef(string asmdefName, string symbol)
            {
                var path = AssetDatabase.FindAssets("t: asmdef " + asmdefName, null).First().ToPath();
                var importer = AssetImporter.GetAtPath(path);

                var editorType = typeof(Editor).Assembly.GetType("UnityEditor.AssemblyDefinitionImporterInspector");
                var editor = Editor.CreateEditor(importer, editorType);

                var state = editor.GetFieldValue<Object[]>("m_ExtraDataTargets").First();


                var definesList = state.GetFieldValue<IList>("versionDefines");
                var isSymbolDefined = Enumerable.Range(0, definesList.Count).Any(i => definesList[i].GetFieldValue<string>("define") == symbol);


                Object.DestroyImmediate(editor);

                return isSymbolDefined;

            }

            static void DefineSymbolInAsmdef(string asmdefName, string symbol)
            {
                var path = AssetDatabase.FindAssets("t: asmdef " + asmdefName, null).First().ToPath();
                var importer = AssetImporter.GetAtPath(path);

                var editorType = typeof(Editor).Assembly.GetType("UnityEditor.AssemblyDefinitionImporterInspector");
                var editor = Editor.CreateEditor(importer, editorType);

                var state = editor.GetFieldValue<Object[]>("m_ExtraDataTargets").First();


                var definesList = state.GetFieldValue<IList>("versionDefines");

                var defineType = definesList.GetType().GenericTypeArguments[0];
                var newDefine = System.Activator.CreateInstance(defineType);

                newDefine.SetFieldValue("name", "Unity");
                newDefine.SetFieldValue("define", symbol);

                definesList.Add(newDefine);


                editor.InvokeMethod("Apply");

                Object.DestroyImmediate(editor);

            }
            static void UndefineSymbolInAsmdef(string asmdefName, string symbol)
            {
                var path = AssetDatabase.FindAssets("t: asmdef " + asmdefName, null).First().ToPath();
                var importer = AssetImporter.GetAtPath(path);

                var editorType = typeof(Editor).Assembly.GetType("UnityEditor.AssemblyDefinitionImporterInspector");
                var editor = Editor.CreateEditor(importer, editorType);

                var state = editor.GetFieldValue<Object[]>("m_ExtraDataTargets").First();


                var definesList = state.GetFieldValue<IList>("versionDefines");

                var defineIndex = Enumerable.Range(0, definesList.Count).First(i => definesList[i].GetFieldValue<string>("define") == symbol);

                definesList.RemoveAt(defineIndex);


                editor.InvokeMethod("Apply");

                Object.DestroyImmediate(editor);

            }




            public static int GetCurrendUndoGroupIndex()
            {
                var args = new object[] { _dummyList, 0 };

                typeof(Undo).GetMethodInfo("GetRecords", typeof(List<string>), typeof(int).MakeByRefType())
                            .Invoke(null, args);


                return (int)args[1];

            }

            static List<string> _dummyList = new();





            public static void Hide(string path)
            {
                if (IsHidden(path)) return;

                if (File.Exists(path))
                    File.Move(path, path + "~");


                path += ".meta";
                if (File.Exists(path))
                    File.Move(path, path + "~");
            }
            public static void Unhide(string path)
            {
                if (!IsHidden(path)) return;
                if (path.EndsWith("~")) path = path.Substring(0, path.Length - 1);

                if (File.Exists(path + "~"))
                    File.Move(path + "~", path);

                path += ".meta";
                if (File.Exists(path + "~"))
                    File.Move(path + "~", path);
            }
            public static bool IsHidden(string path) => path.EndsWith("~") || File.Exists(path + "~");


            public static void CopyDirectoryDeep(string sourcePath, string destinationPath)
            {
                CopyDirectoryRecursively(sourcePath, destinationPath);

                var metas = GetFilesRecursively(destinationPath, (f) => f.EndsWith(".meta"));
                var guidTable = new List<(string originalGuid, string newGuid)>();

                foreach (string meta in metas)
                {
                    StreamReader file = new(meta);
                    file.ReadLine();
                    string guidLine = file.ReadLine();
                    file.Close();
                    string originalGuid = guidLine.Substring(6, guidLine.Length - 6);
                    string newGuid = GUID.Generate().ToString().Replace("-", "");
                    guidTable.Add((originalGuid, newGuid));
                }

                var allFiles = GetFilesRecursively(destinationPath);

                foreach (string fileToModify in allFiles)
                {
                    string content = File.ReadAllText(fileToModify);

                    foreach (var guidPair in guidTable)
                    {
                        content = content.Replace(guidPair.originalGuid, guidPair.newGuid);
                    }

                    File.WriteAllText(fileToModify, content);
                }

                AssetDatabase.Refresh();
            }

            private static void CopyDirectoryRecursively(string sourceDirName, string destDirName)
            {
                DirectoryInfo dir = new(sourceDirName);

                DirectoryInfo[] dirs = dir.GetDirectories();

                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                }

                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string temppath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(temppath, false);
                }

                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    CopyDirectoryRecursively(subdir.FullName, temppath);
                }
            }

            private static List<string> GetFilesRecursively(string path, System.Func<string, bool> criteria = null, List<string> files = null)
            {
                if (files == null)
                {
                    files = new List<string>();
                }

                files.AddRange(Directory.GetFiles(path).Where(f => criteria == null || criteria(f)));

                foreach (string directory in Directory.GetDirectories(path))
                {
                    GetFilesRecursively(directory, criteria, files);
                }

                return files;
            }





            // for non-extension methods

        }


        public static class EditorPrefsCached
        {
            public static int GetInt(string key, int defaultValue = 0)
            {
                if (ints_byKey.ContainsKey(key))
                    return ints_byKey[key];
                else
                    return ints_byKey[key] = EditorPrefs.GetInt(key, defaultValue);

            }
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

            public static void SetInt(string key, int value)
            {
                ints_byKey[key] = value;

                EditorPrefs.SetInt(key, value);

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


            static Dictionary<string, bool> bools_byKey = new();
            static Dictionary<string, float> floats_byKey = new();
            static Dictionary<string, int> ints_byKey = new();

        }

        public static class ProjectPrefs
        {
            public static int GetInt(string key, int defaultValue = 0) => EditorPrefs.GetInt(key + projectId, defaultValue);
            public static bool GetBool(string key, bool defaultValue = false) => EditorPrefs.GetBool(key + projectId, defaultValue);
            public static float GetFloat(string key, float defaultValue = 0) => EditorPrefs.GetFloat(key + projectId, defaultValue);
            public static string GetString(string key, string defaultValue = "") => EditorPrefs.GetString(key + projectId, defaultValue);

            public static void SetInt(string key, int value) => EditorPrefs.SetInt(key + projectId, value);
            public static void SetBool(string key, bool value) => EditorPrefs.SetBool(key + projectId, value);
            public static void SetFloat(string key, float value) => EditorPrefs.SetFloat(key + projectId, value);
            public static void SetString(string key, string value) => EditorPrefs.SetString(key + projectId, value);



            public static bool HasKey(string key) => EditorPrefs.HasKey(key + projectId);
            public static void DeleteKey(string key) => EditorPrefs.DeleteKey(key + projectId);



            public static int projectId => PlayerSettings.productGUID.GetHashCode();

        }



        public static void RecordUndo(this Object o, string operationName = "") => Undo.RecordObject(o, operationName);
        public static void Dirty(this Object o) => UnityEditor.EditorUtility.SetDirty(o);
        public static void Save(this Object o) => AssetDatabase.SaveAssetIfDirty(o);



        public static void SelectInInspector(this Object[] objects, bool frameInHierarchy = false, bool frameInProject = false)
        {
            void setHierarchyLocked(bool isLocked) => allHierarchies.ForEach(r => r?.GetMemberValue("m_SceneHierarchy")?.SetMemberValue("m_RectSelectInProgress", isLocked));
            void setProjectLocked(bool isLocked) => allProjectBrowsers.ForEach(r => r?.SetMemberValue("m_InternalSelectionChange", isLocked));


            if (!frameInHierarchy) setHierarchyLocked(true);
            if (!frameInProject) setProjectLocked(true);

            Selection.objects = objects?.ToArray();

            if (!frameInHierarchy) EditorApplication.delayCall += () => setHierarchyLocked(false);
            if (!frameInProject) EditorApplication.delayCall += () => setProjectLocked(false);

        }
        public static void SelectInInspector(this Object obj, bool frameInHierarchy = false, bool frameInProject = false) => new[] { obj }.SelectInInspector(frameInHierarchy, frameInProject);

        static IEnumerable<EditorWindow> allHierarchies => _allHierarchies ??= typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow").GetFieldValue<IList>("s_SceneHierarchyWindows").Cast<EditorWindow>();
        static IEnumerable<EditorWindow> _allHierarchies;

        static IEnumerable<EditorWindow> allProjectBrowsers => _allProjectBrowsers ??= typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser").GetFieldValue<IList>("s_ProjectBrowsers").Cast<EditorWindow>();
        static IEnumerable<EditorWindow> _allProjectBrowsers;



#endif

        #endregion

    }

    public static partial class VGUI
    {

        #region Drawing


        public static Rect Draw(this Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);

            return rect;

        }
        public static Rect Draw(this Rect rect) => rect.Draw(Color.black);

        public static Rect DrawOutline(this Rect rect, Color color, float thickness = 1)
        {

            rect.SetWidth(thickness).Draw(color);
            rect.SetWidthFromRight(thickness).Draw(color);

            rect.SetHeight(thickness).Draw(color);
            rect.SetHeightFromBottom(thickness).Draw(color);


            return rect;

        }
        public static Rect DrawOutline(this Rect rect, float thickness = 1) => rect.DrawOutline(Color.black, thickness);




        public static Rect DrawRounded(this Rect rect, Color color, int cornerRadius)
        {
            if (!curEvent.isRepaint) return rect;

            cornerRadius = cornerRadius.Min((rect.height / 2).FloorToInt()).Min((rect.width / 2).FloorToInt());

            if (cornerRadius < 0) return rect;

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
        public static Rect DrawRounded(this Rect rect, Color color, float cornerRadius) => rect.DrawRounded(color, cornerRadius.RoundToInt());

        static Dictionary<int, GUIStyle> _roundedStylesByCornerRadius = new();




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
                var kernel = GaussianKernel.GenerateArray(blurRadiusScaled * 2 + 1);

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

        static Dictionary<(int, int), GUIStyle> _blurredStylesByTextureSize = new();




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

        static Texture2D[] _gradientTextures;

        public static void DrawCurtainUp(this Rect rect, Color color) => rect.DrawCurtain(color, 0);
        public static void DrawCurtainDown(this Rect rect, Color color) => rect.DrawCurtain(color, 1);
        public static void DrawCurtainLeft(this Rect rect, Color color) => rect.DrawCurtain(color, 2);
        public static void DrawCurtainRight(this Rect rect, Color color) => rect.DrawCurtain(color, 3);






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

        #region Shortcuts


        public static Rect lastRect => GUILayoutUtility.GetLastRect();

        public static bool isDarkTheme => EditorGUIUtility.isProSkin;

        public static bool IsHovered(this Rect r) => r.Contains(curEvent.mousePosition);

        public static float GetLabelWidth(this string s) => GUI.skin.label.CalcSize(new GUIContent(s)).x;
        public static float GetLabelWidth(this string s, int fontSize)
        {
            SetLabelFontSize(fontSize);

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
        public static float GetLabelWidth(this string s, int fontSize, bool isBold)
        {
            if (isBold)
                SetLabelBold();

            SetLabelFontSize(fontSize);

            var r = s.GetLabelWidth();

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
            _guiColorStack.Push(GUI.color);

            GUI.color *= c;

        }
        public static void ResetGUIColor()
        {
            GUI.color = _guiColorStack.Pop();
        }

        static Stack<Color> _guiColorStack = new();



        public static float editorDeltaTime = .0166f;

        static void EditorDeltaTime_Update()
        {
            editorDeltaTime = (float)(EditorApplication.timeSinceStartup - _lastUpdateTime);

            _lastUpdateTime = EditorApplication.timeSinceStartup;

        }
        static double _lastUpdateTime;

        [InitializeOnLoadMethod]
        static void EditorDeltaTime_Subscribe()
        {
            EditorApplication.update -= EditorDeltaTime_Update;
            EditorApplication.update += EditorDeltaTime_Update;
        }




        #endregion

        #region Controls


        public static bool IconButton(Rect rect, string iconName, float iconSize = default, Color color = default, Color colorHovered = default, Color colorPressed = default)
        {
            var id = EditorGUIUtility.GUIToScreenRect(rect).GetHashCode();// GUIUtility.GetControlID(FocusType.Passive, rect);
            var isPressed = id == _pressedIconButtonId;

            var wasActivated = false;

            void icon()
            {
                if (!curEvent.isRepaint) return;


                if (color == default)
                    color = Color.white;

                if (colorHovered == default)
                    colorHovered = Color.white;

                if (colorPressed == default)
                    colorPressed = Color.white.SetAlpha(.8f);


                if (rect.IsHovered())
                    color = colorHovered;

                if (isPressed)
                    color = colorPressed;


                if (iconSize == default)
                    iconSize = rect.width.Min(rect.height);

                var iconRect = rect.SetSizeFromMid(iconSize);



                SetGUIColor(color);

                GUI.DrawTexture(iconRect, EditorIcons.GetIcon(iconName));

                ResetGUIColor();


            }
            void mouseDown()
            {
                if (!curEvent.isMouseDown) return;
                if (!rect.IsHovered()) return;

                _pressedIconButtonId = id;

                curEvent.Use();

            }
            void mouseUp()
            {
                if (!curEvent.isMouseUp) return;
                if (!isPressed) return;

                _pressedIconButtonId = 0;

                if (rect.IsHovered())
                    wasActivated = true;

                curEvent.Use();

            }
            void mouseDrag()
            {
                if (!curEvent.isMouseDrag) return;
                if (!isPressed) return;

                curEvent.Use();

            }

            rect.MarkInteractive();

            icon();
            mouseDown();
            mouseUp();
            mouseDrag();

            return wasActivated;

        }

        static int _pressedIconButtonId;



        public static string Tabs(string current, bool equalButtonSizes = true, float rowHeight = 24, params string[] variants)
        {
            GUILayout.BeginHorizontal();

            Space(EditorGUI.indentLevel * 15);

            for (int i = 0; i < variants.Length; i++)
            {
                GUI.backgroundColor = variants[i] == current ? GUIColors.pressedButtonBackground : Color.white;
                bool b;

                if (variants[i] == "Settings" && i == variants.Length - 1)
                    b = GUILayout.Button(EditorGUIUtility.IconContent("Settings"), GUILayout.Height(24), GUILayout.Width(26));

                else if (variants[i] == "More" && i == variants.Length - 1)
                    b = GUILayout.Button(EditorGUIUtility.IconContent("more"), GUILayout.Height(24), GUILayout.Width(28));

                else if (equalButtonSizes)
                    b = ButtonFixedSize(variants[i], rowHeight);
                else
                    b = Button(variants[i], rowHeight);

                if (b) current = variants[i];

                GUI.backgroundColor = Color.white;


                if (i != variants.Length - 1) GUILayout.Space(-6f);

            }
            GUILayout.EndHorizontal();

            return current;
        }

        public static string TabsMultiRow(string current, bool equalButtonSizes = true, float rowHeight = 24, params string[] variants)
        {
            float textWidth(string text)
            {
                if (text == "Settings" || text == "More")
                    return 22;
                else
                    return text.GetLabelWidth();
            }

            var spaceBetweenTexts = 5;

            var maxWidth = GetCurrentInspectorWidth() - 22;
            var totalWidth = variants.Sum(r => textWidth(r) + spaceBetweenTexts);
            var rowsN = (totalWidth / maxWidth).CeilToInt();
            var rowWidth = totalWidth / rowsN;

            var rows = new List<List<string>>();
            var curRow = new List<string>();
            rows.Add(curRow);

            var curRowWidth = 0f;

            for (int i = 0; i < variants.Length; i++)
            {
                void nextRow()
                {
                    curRowWidth = 0;
                    curRow = new List<string>();
                    rows.Add(curRow);
                }


                var widthToAdd = textWidth(variants[i]) + spaceBetweenTexts;

                if (curRowWidth + widthToAdd > maxWidth && curRow.Any())
                    nextRow();

                curRow.Add(variants[i]);
                curRowWidth += widthToAdd;

                if (curRowWidth > rowWidth && i != variants.Length - 1)
                    nextRow();

            }


            foreach (var row in rows)
            {
                current = Tabs(current, equalButtonSizes, rowHeight, row.ToArray());
                Space(-3);
            }

            Space(3);


            return current;

        }

        public static bool Button(string text = "") => GUILayout.Button(text);
        public static bool Button(string text, float height = 24) => GUILayout.Button(text, GUILayout.Height(height));
        public static bool ButtonFixedSize(string text, float height = 24)
        {
            GUILayout.Label("", GUILayout.Height(height));

            var b = GUI.Button(lastRect, "");

            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(lastRect, text);
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            return b;
        }







        #endregion

        #region Layout


        public static void Space(float px = 6) => GUILayout.Space(px);

        public static Rect ExpandWidthLabelRect() { GUILayout.Label(""/* , GUILayout.Height(0) */, GUILayout.ExpandWidth(true)); return lastRect; }
        public static Rect ExpandWidthLabelRect(float height) { GUILayout.Label("", GUILayout.Height(height), GUILayout.ExpandWidth(true)); return lastRect; }



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
        static Stack<float> _indentLabelWidthStack = new();





        #endregion

        #region GUIColors


        public static class GUIColors
        {
            public static Color windowBackground => isDarkTheme ? Greyscale(.22f) : Greyscale(.78f); // prev backgroundCol
            public static Color pressedButtonBackground => isDarkTheme ? new Color(.48f, .76f, 1f, 1f) * 1.4f : new Color(.48f, .7f, 1f, 1f) * 1.2f; // prev pressedButtonCol
            public static Color greyedOutTint => Greyscale(.7f);
            public static Color selectedBackground => isDarkTheme ? new Color(.17f, .365f, .535f) : new Color(.2f, .375f, .555f) * 1.2f;
        }




        #endregion

        #region EditorIcons


        public static partial class EditorIcons
        {
            public static Texture2D GetIcon(string iconNameOrPath)
            {
                if (icons_byName.TryGetValue(iconNameOrPath, out var cachedResult) && cachedResult != null) return cachedResult;

                Texture2D icon = null;

                void getCustom()
                {
                    if (icon) return;
                    if (!customIcons.ContainsKey(iconNameOrPath)) return;

                    var pngBytesString = customIcons[iconNameOrPath];
                    var pngBytes = pngBytesString.Split("-").Select(r => System.Convert.ToByte(r, 16)).ToArray();

                    icon = new Texture2D(1, 1);

                    icon.LoadImage(pngBytes);

                }
                void getBuiltin()
                {
                    if (icon) return;

                    icon = typeof(EditorGUIUtility).InvokeMethod<Texture2D>("LoadIcon", iconNameOrPath) as Texture2D;

                }
                void getEmpty()
                {
                    if (icon) return;

                    icon = new Texture2D(1, 1);

                }

                getCustom();
                getBuiltin();
                getEmpty();

                return icons_byName[iconNameOrPath] = icon;

            }

            static Dictionary<string, Texture2D> icons_byName = new();



            static Dictionary<string, string> customIcons = new()
            {
                ["Copy"] = "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-09-70-48-59-73-00-00-0B-13-00-00-0B-13-01-00-9A-9C-18-00-00-00-01-73-52-47-42-00-AE-CE-1C-E9-00-00-00-04-67-41-4D-41-00-00-B1-8F-0B-FC-61-05-00-00-01-AD-49-44-41-54-78-01-ED-D9-BD-51-C3-40-10-86-E1-4F-1A-0A-20-F5-4F-60-77-00-1D-40-07-74-00-74-E0-0E-30-15-30-54-00-54-00-54-00-1D-40-09-44-56-AA-54-89-C4-DE-A0-C0-09-20-AD-6E-CF-27-DD-F7-CC-78-AC-80-61-E6-5E-CB-A7-95-0C-10-11-11-11-25-2A-43-00-45-51-AC-9A-A6-79-90-C3-13-79-1D-A3-A7-2C-CB-B6-B3-D9-EC-16-06-CC-03-B4-8B-FF-80-62-E1-FB-AC-22-E4-30-26-8B-BF-C3-C0-C5-B7-FF-67-2B-31-6F-E0-99-F9-19-B0-DB-ED-1A-78-E4-FB-4C-30-3F-03-7C-F3-7D-26-8C-2E-80-E3-33-C2-28-03-38-BE-22-04-B9-0C-FE-A7-BD-52-BC-C9-E1-0A-3D-0D-DD-13-A2-08-E0-1C-2A-42-34-01-9C-36-C2-33-7E-06-A6-5E-E6-F3-B9-6A-2D-51-ED-01-F2-29-7E-55-55-75-2E-87-9F-08-24-BA-4D-70-BD-5E-97-21-23-A8-BF-02-21-E6-7B-19-A2-1E-E5-ED-12-1D-04-FD-0A-EC-CD-F7-67-50-8E-B9-5D-2E-63-B2-A8-2B-79-7B-82-21-55-80-90-F3-BD-8B-50-D7-F5-3D-8C-68-F7-80-0B-78-D2-25-C2-72-B9-DC-C8-DF-99-DC-0E-47-B1-09-76-89-B0-58-2C-B6-16-11-A2-B9-0A-74-8D-00-CF-A2-BA-0C-5A-DD-F3-FF-25-BA-39-20-74-84-28-EF-06-43-46-88-F6-76-D8-45-40-00-A3-7D-1E-E0-0B-03-20-71-0C-80-C4-31-00-12-C7-00-48-DC-24-02-C8-D4-F8-0A-A5-29-04-28-F3-3C-DF-40-69-CC-01-4A-79-BD-CB-C3-D5-53-F7-38-1D-4A-47-18-19-ED-D3-DF-DF-70-13-44-E2-18-00-89-63-00-24-8E-01-A0-53-62-22-B4-01-CC-7F-BB-1F-32-DF-F7-A1-0A-20-E3-E7-35-6C-CF-82-41-F3-7D-1F-AA-00-6E-F6-76-33-B8-1C-BE-C0-2F-2F-F3-3D-11-11-11-11-75-F0-0D-1A-BD-C9-B2-42-82-18-F9-00-00-00-00-49-45-4E-44-AE-42-60-82",
                ["Copied"] = "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-09-70-48-59-73-00-00-0B-13-00-00-0B-13-01-00-9A-9C-18-00-00-00-01-73-52-47-42-00-AE-CE-1C-E9-00-00-00-04-67-41-4D-41-00-00-B1-8F-0B-FC-61-05-00-00-04-76-49-44-41-54-78-01-ED-9A-4D-4E-DB-40-14-C7-DF-E4-13-54-16-E9-06-89-0F-A9-41-0D-52-97-20-35-15-CB-E4-06-C9-09-4A-4E-00-9C-80-70-02-E0-04-A4-27-08-37-20-CB-8A-54-2A-DD-55-02-44-36-80-C4-A6-59-14-45-C1-89-A7-EF-19-27-B5-1D-4F-E2-F8-63-30-C1-3F-A9-B2-3D-E3-71-79-7F-CF-BC-37-EF-39-00-11-11-11-11-11-11-11-11-6F-14-06-12-B8-BF-BF-CF-72-CE-4F-F0-74-03-FF-65-60-4A-18-63-D5-A5-A5-A5-03-08-80-C0-05-D0-8D-FF-09-2E-0C-37-12-94-08-31-08-18-34-FE-10-3C-1A-AF-3F-A7-8A-62-EE-83-CF-04-3E-03-EE-EE-EE-38-F8-88-DF-33-21-F0-19-E0-37-7E-CF-84-57-27-00-E1-A7-08-AF-52-00-C2-2F-11-A4-84-C1-49-E8-91-E2-0C-4F-B3-30-25-5E-7D-42-28-04-20-5E-4A-84-D0-08-40-E8-22-D4-E1-79-C3-34-15-CB-CB-CB-AE-6C-09-95-0F-C0-B7-D8-EA-76-BB-45-3C-BD-00-49-84-CE-09-AE-AD-AD-B5-65-8A-E0-7A-09-C8-D8-DF-E3-26-AA-86-87-AF-E0-00-A9-4B-C0-B0-BF-2F-80-CB-6D-AE-93-30-86-46-6D-E3-E1-1B-04-88-2B-01-64-EE-EF-49-04-55-55-8F-21-20-DC-FA-80-12-F8-84-13-11-56-57-57-77-F1-BE-40-D2-E1-50-38-41-27-22-AC-AC-AC-54-83-10-21-34-51-C0-A9-08-E0-33-A1-0A-83-41-E5-FC-E3-08-DD-3E-40-B6-08-A1-CC-06-65-8A-10-DA-74-98-44-00-09-BC-DA-7A-80-5F-BC-79-01-12-F0-06-D8-52-72-05-55-C5-6D-7B-9C-5F-9C-27-AE-4F-8D-7D-33-3F-03-F2-4F-EB-27-7D-15-CE-B0-34-BD-CF-FB-AC-4E-D7-C6-FE-59-11-A0-65-D7-98-7F-CA-1D-A2-37-DD-36-35-E2-F5-56-27-57-18-5C-CE-84-00-89-44-A2-6C-6D-FB-AC-E4-F6-81-C3-AE-DD-FD-6A-4C-CB-62-35-66-41-80-BD-C5-C5-45-53-F1-04-A7-F9-0E-53-A1-2A-1E-C2-87-F7-BF-94-00-0D-2C-88-14-B1-F2-F3-9E-0A-19-74-A4-6B-98-32-F7-A7-E4-08-C7-1F-19-DB-F2-7F-D7-37-B0-E3-48-38-06-E0-F4-3C-FD-DF-11-CA-8E-02-6D-34-B4-8C-95-A0-86-B1-91-CA-60-78-A0-B6-06-EE-00-6B-7A-A5-29-3B-EE-41-54-23-C0-34-B9-6A-6C-DB-EA-7C-CA-F6-63-FD-BA-68-0C-03-D6-7A-97-52-2A-C6-36-99-33-80-8C-DF-B4-1A-6F-85-FA-F5-D9-D0-1E-73-5B-83-6A-04-C6-06-DD-78-2C-AB-F3-AC-DD-00-32-3E-A6-C6-8B-0D-D6-32-3D-57-A6-00-07-54-F5-75-72-23-DD-87-8E-AD-28-E8-A6-CA-B1-C9-E9-15-78-36-D3-8F-F7-EA-22-E3-91-36-19-FF-7D-FE-77-CB-DA-21-6B-09-B4-6C-D7-6A-0A-4B-6B-1C-3D-32-A3-0A-30-AB-34-53-97-43-E7-64-75-6C-03-68-76-E8-4B-66-C8-A3-92-A4-70-27-FC-96-C0-80-57-EC-8C-27-A4-CC-00-5C-D3-BF-8C-D7-34-5D-79-12-BF-02-71-3D-1C-71-20-C7-75-86-DE-7B-E2-07-11-EB-2C-7A-0E-77-96-58-6F-FA-BF-61-CF-E8-F4-AC-C8-5A-02-A6-B7-D9-63-BD-12-1B-2D-AA-66-48-04-12-07-1C-42-C6-8F-0B-77-D8-77-F0-63-EE-EA-68-DC-33-C2-B6-0F-C8-90-23-73-22-C2-97-EE-C7-D2-D8-58-CF-F9-F1-F9-FC-55-75-D2-73-64-09-F0-C1-78-D1-4B-F7-6A-20-F4-F2-5C-F3-E6-1B-E8-D8-40-00-09-A4-02-3B-11-F5-33-F4-29-CD-B9-EB-5D-70-80-14-01-D0-71-95-6E-6E-6E-86-06-5D-50-28-52-D9-9E-78-04-CF-26-95-84-AD-08-83-70-C7-04-DF-25-B4-70-D7-1F-DD-1A-8B-90-35-03-32-A9-54-CA-F4-46-9A-F3-97-35-72-50-C2-11-E8-18-93-E4-DD-0D-68-E1-CE-41-AC-17-79-7C-3B-A4-F9-00-9C-05-3B-0F-0F-0F-26-2F-4F-0E-8A-A3-A3-12-0E-42-EF-AE-65-74-3A-8F-38-2B-C6-C5-7A-AE-40-79-1A-E3-B5-BF-0B-5C-E0-E1-97-5F-2D-8A-E3-23-A1-AC-93-AB-B2-18-08-8B-A0-D8-87-1F-45-28-4A-80-78-5D-AB-B8-8F-C0-59-05-53-E2-56-80-3F-E0-FE-DB-A0-AD-08-F9-CE-7A-0D-62-DC-D1-97-60-2B-14-EE-9C-78-7C-3B-DC-2E-01-2F-DF-EE-B3-98-C8-D4-8D-4E-91-C0-B7-B7-8D-AF-A3-01-53-E2-C5-78-C2-95-00-F8-06-29-A3-6A-83-4B-70-FC-46-3A-9D-1E-C9-DA-94-64-AF-FC-BC-2D-76-86-96-DA-7A-30-9E-70-25-00-4D-5F-CA-EC-F0-F4-14-DC-53-B8-BD-BD-35-C5-72-0A-8F-71-0C-61-E4-CD-27-0D-A6-7B-16-52-BD-0A-78-24-54-3F-92-1A-A0-6D-74-30-DC-71-1F-C3-9D-88-50-0A-40-68-D9-62-52-FB-D9-9C-D5-D9-B6-E3-6A-62-D3-0F-E3-89-D0-D6-04-9B-0B-98-1A-2B-23-85-91-36-B5-F9-65-3C-11-DA-19-30-80-96-03-65-8F-74-BE-80-39-84-B5-A2-13-11-11-11-11-E1-81-7F-9B-A0-1E-B4-75-CF-A0-EC-00-00-00-00-49-45-4E-44-AE-42-60-82",
                ["Paste values"] = "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-09-70-48-59-73-00-00-0B-13-00-00-0B-13-01-00-9A-9C-18-00-00-00-01-73-52-47-42-00-AE-CE-1C-E9-00-00-00-04-67-41-4D-41-00-00-B1-8F-0B-FC-61-05-00-00-02-AB-49-44-41-54-78-01-ED-9B-CD-4D-EB-40-10-80-67-A3-57-C0-3B-3D-BD-FC-1C-92-0E-A0-03-E8-00-D2-00-50-01-A1-02-4C-05-88-0A-08-0D-04-A8-80-12-A0-84-5C-92-40-4E-BE-46-8A-BC-CC-10-83-C0-10-B0-37-B3-B3-63-B1-9F-14-D9-B1-12-D0-7C-9E-DD-9D-DD-75-00-22-91-48-24-12-89-44-7E-29-06-04-98-CD-66-5D-6B-ED-25-9E-6E-E1-EB-2F-54-C4-18-93-34-9B-CD-33-F0-80-77-01-79-F0-F7-E0-10-F8-7B-7C-49-68-80-67-30-F8-73-D8-30-F8-FC-EF-24-28-F3-14-98-F1-9E-01-D3-E9-D4-02-23-DC-99-E0-3D-03-B8-E1-CE-84-DA-09-20-38-25-D4-52-00-C1-25-41-64-18-FC-89-7C-A4-B8-C3-D3-2E-54-64-D3-3E-41-85-00-22-94-04-35-02-88-5C-C2-35-AC-0A-A6-4A-B4-5A-2D-A7-58-54-F5-01-78-17-C7-8B-C5-62-17-4F-1F-40-08-75-9D-60-AF-D7-4B-25-25-38-37-01-89-FA-1E-8B-A8-21-1E-0E-A0-04-A2-4D-E0-5D-7D-BF-03-8E-65-6E-99-61-0C-83-3A-C4-C3-15-78-C4-49-80-64-7D-4F-12-B2-2C-BB-00-4F-B8-F6-01-7B-C0-44-19-09-9D-4E-67-80-9F-F3-32-1D-56-D1-09-96-91-D0-6E-B7-13-1F-12-D4-8C-02-65-25-00-33-AA-86-41-5F-73-FE-EF-50-57-07-48-4B-50-39-1B-94-94-A0-76-3A-4C-12-40-80-DA-AD-07-60-05-79-84-87-31-30-51-3B-01-58-3E-0F-51-02-CD-15-C6-C0-80-66-01-6B-4B-60-9A-35-E2-81-A5-3A-D4-2A-E0-2A-9F-07-AC-05-B3-E0-06-18-D0-28-E0-CB-E0-FB-4F-D6-CB-A8-A0-4D-C0-FA-E0-2D-24-E0-01-4D-02-C4-83-27-B4-08-08-12-3C-A1-41-40-B0-E0-89-D0-02-58-82-C7-AA-F1-16-1C-09-29-80-EB-CE-A7-8D-46-63-00-8E-FC-81-30-6C-1C-7C-5E-0C-6D-BC-AF-11-22-03-82-B6-F9-22-D2-02-54-05-4F-48-0A-50-17-3C-E1-D4-86-1C-9E-FA-B8-C1-E0-F7-8B-17-83-04-6F-20-C5-D7-70-F4-CF-9C-D0-5B-91-0C-C0-89-CB-49-F1-5A-B0-3B-6F-71-3F-23-83-41-7F-B6-5A-70-91-10-90-E6-3D-F6-1B-A1-D3-FE-05-03-C7-74-10-11-F0-E9-4A-A6-67-5B-5E-42-40-77-3E-9F-7F-D8-EF-1F-35-4D-82-19-E0-65-A7-A7-2C-36-5B-2D-B8-B8-0A-48-AB-7C-78-B9-5C-9E-17-AF-05-93-40-9D-20-FE-DF-EB-96-19-AC-DE-3A-80-A3-00-3D-CA-B2-53-E5-3B-58-AF-0F-71-67-E7-A8-78-FD-A5-33-32-50-6A-B1-63-F4-DF-B0-37-1D-A7-0C-C8-57-66-2B-65-01-7E-E7-70-32-99-5C-16-AF-87-6E-0E-4E-02-A8-57-C7-80-B6-F1-B4-D2-BA-9C-46-09-AA-1E-92-FA-A9-39-A8-69-02-BE-08-91-09-EA-56-85-A5-25-A8-DC-17-90-94-A0-76-67-48-4A-82-EA-BD-41-0D-15-A3-0A-68-74-E8-3F-5A-D6-1F-5E-D4-8E-D7-E9-6B-84-99-67-84-DC-66-24-9B-76-8E-1A-00-00-00-00-49-45-4E-44-AE-42-60-82",
                ["Chevron Left"] = "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-09-70-48-59-73-00-00-0B-13-00-00-0B-13-01-00-9A-9C-18-00-00-00-01-73-52-47-42-00-AE-CE-1C-E9-00-00-00-04-67-41-4D-41-00-00-B1-8F-0B-FC-61-05-00-00-01-29-49-44-41-54-78-01-ED-9B-CD-71-83-30-14-06-3F-A5-02-4A-71-09-A4-A3-B4-94-0A-28-01-77-10-77-90-16-D2-81-A2-A7-21-A7-FC-8C-A4-60-23-9E-76-67-34-1C-2C-1F-76-2D-30-C3-08-09-00-00-60-58-82-3A-25-C6-38-A7-C3-25-8D-29-8D-6B-08-E1-AA-11-48-E2-53-1A-6B-FC-CE-62-9F-C9-33-9B-FC-5B-FC-9D-55-5E-29-90-FF-62-D6-8E-3C-A9-03-B6-A5-6D-BF-EE-A5-60-7A-C9-9C-62-0E-0F-50-29-6F-F8-B9-0E-54-2C-FB-BB-9D-02-87-D1-28-BF-C8-03-8D-F2-36-FF-FC-CB-1F-79-E4-91-47-1E-79-E4-91-47-1E-79-E4-91-F7-21-6F-C4-C1-E5-5F-62-1D-7E-E4-8D-24-F3-3E-B2-FC-74-76-F9-2E-1E-8A-9E-9A-B3-9F-02-7B-AC-80-D7-8A-B9-F6-E4-77-ED-2D-C2-BF-88-A3-DF-03-18-44-10-11-32-44-10-11-32-44-10-11-32-44-10-11-32-44-10-11-32-44-10-11-32-44-50-73-04-5F-FB-04-1B-23-CC-DA-91-43-1F-89-85-10-3E-D2-E1-39-8D-5B-C5-D7-7C-ED-13-6C-88-E0-73-BF-70-C5-E9-30-CB-2B-05-11-7C-EC-13-FC-8B-2D-C2-F2-83-FC-1A-EF-F0-37-D8-FB-0B-13-36-EC-1A-71-1B-E6-85-09-00-00-80-C7-F1-09-79-C0-DD-81-D6-B5-69-91-00-00-00-00-49-45-4E-44-AE-42-60-82",
                ["Chevron Right"] = "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-09-70-48-59-73-00-00-0B-13-00-00-0B-13-01-00-9A-9C-18-00-00-00-01-73-52-47-42-00-AE-CE-1C-E9-00-00-00-04-67-41-4D-41-00-00-B1-8F-0B-FC-61-05-00-00-01-1A-49-44-41-54-78-01-ED-9B-D1-0D-82-30-10-86-AF-4E-C0-28-8E-80-1B-39-82-1B-39-02-6C-00-1B-B8-82-1B-9C-6D-D0-27-05-69-4B-48-B9-7E-5F-D2-F0-40-5F-FE-2F-B4-47-9A-AB-08-00-00-40-B5-38-D9-08-55-6D-FD-E3-EC-47-E3-47-EF-9C-EB-A5-06-7C-F0-C6-8F-4E-BF-B9-87-77-62-9D-77-D0-39-06-D3-12-C2-67-AF-FF-29-5A-C2-49-F2-68-57-CC-09-FB-42-57-AA-84-5C-01-CF-95-F3-8A-95-90-2B-60-8C-98-5B-F4-97-90-CC-4C-05-38-EC-9E-10-8D-4E-65-70-88-73-80-04-24-20-01-09-48-40-02-12-90-80-04-24-20-01-09-48-40-42-04-37-B1-44-82-84-87-58-23-41-C2-A6-CB-20-F7-48-0C-72-A8-7A-09-68-DA-26-78-15-0B-24-86-1F-C4-02-5A-F3-3F-00-E1-09-4F-78-C2-13-9E-F0-84-27-3C-E1-09-4F-78-3B-E7-7B-BA-DC-27-68-3E-FC-9A-3E-C1-A2-C3-EF-D1-27-F8-21-74-94-5D-9C-73-6B-5B-EB-76-61-AF-3E-C1-22-C3-67-A3-06-5A-65-B3-D1-E5-3E-41-DB-E1-03-3A-95-C1-5F-95-A0-3B-42-F8-AD-2F-4C-84-11-D6-F9-58-CD-85-09-00-00-80-E3-F2-02-5D-3B-DF-D0-96-78-5C-6E-00-00-00-00-49-45-4E-44-AE-42-60-82",
                ["Save"] = "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-09-70-48-59-73-00-00-0B-13-00-00-0B-13-01-00-9A-9C-18-00-00-00-01-73-52-47-42-00-AE-CE-1C-E9-00-00-00-04-67-41-4D-41-00-00-B1-8F-0B-FC-61-05-00-00-01-31-49-44-41-54-78-01-ED-DA-CD-0D-82-40-10-86-E1-0F-62-01-5E-E1-64-29-76-62-0D-56-20-76-60-49-94-E2-05-B8-5A-01-38-93-A8-F1-E0-4F-16-67-85-65-BE-E7-22-89-06-C2-6B-56-18-15-20-22-22-22-22-22-8F-32-04-EA-BA-6E-37-0C-C3-49-36-D7-AF-9E-2F-CB-F2-B1-CF-A6-69-AA-2C-CB-0E-9F-F6-F7-FC-FA-57-DA-B6-1D-EE-DB-B2-AF-AA-28-8A-23-0C-E5-08-24-27-5F-E1-CD-C9-C7-A6-C7-96-37-E0-00-43-C1-01-C4-06-13-B2-8E-30-26-C0-E4-2C-23-24-19-40-59-45-48-36-80-B2-88-90-74-00-F5-6B-84-E4-03-A8-5F-22-2C-22-80-1A-1B-61-85-88-F2-3C-AF-FB-BE-C7-BF-DC-EE-51-82-6E-94-A2-06-90-BB-B6-5A-1E-6A-CC-D8-62-96-C0-58-EE-03-98-2F-81-E7-E1-25-05-5C-02-70-8E-01-E0-1C-03-C0-39-D3-CB-E0-B7-EF-F7-AC-58-5E-6A-B9-04-E0-5C-B4-61-48-46-D3-AD-4C-82-5B-18-D0-A9-F2-36-58-99-8B-16-40-4F-FE-DB-6F-02-01-FB-D2-87-1A-11-F0-33-00-CE-31-00-9C-63-00-38-C7-00-70-8E-01-E0-1C-03-C0-39-06-40-B8-0B-E6-EB-8C-40-C1-01-64-C4-DD-8F-39-D0-1F-5C-64-6C-36-FD-0B-1D-11-11-11-11-11-2D-D7-15-FC-07-71-D0-EE-EA-33-E6-00-00-00-00-49-45-4E-44-AE-42-60-82",
                ["Saved"] = "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-09-70-48-59-73-00-00-0B-13-00-00-0B-13-01-00-9A-9C-18-00-00-00-01-73-52-47-42-00-AE-CE-1C-E9-00-00-00-04-67-41-4D-41-00-00-B1-8F-0B-FC-61-05-00-00-04-1F-49-44-41-54-78-01-ED-9A-4D-52-E2-40-14-C7-5F-87-0F-B5-C6-05-B3-54-37-58-83-55-B3-D4-2A-99-72-19-4E-30-72-02-E5-04-A3-27-10-4F-30-33-27-90-39-81-CC-09-CC-72-4A-66-C1-72-AA-D4-32-1B-75-CB-62-2C-0A-02-E9-79-2F-80-13-63-3A-24-21-84-00-FD-AB-A2-20-DD-9D-86-F7-EF-EE-F7-FA-75-00-90-48-24-12-89-44-22-91-48-24-CB-08-83-80-3C-3D-3D-1D-71-CE-BF-E1-C7-9C-5B-FD-E6-E6-E6-4B-9F-0F-0F-0F-55-C6-D8-99-57-7F-F6-F6-6E-3C-3E-3E-F2-D1-67-EC-AB-BA-B1-B1-71-0E-11-A2-40-40-D0-F8-2A-08-8C-9F-36-F4-DD-38-00-67-10-21-81-05-40-F2-30-43-A2-16-21-8C-00-33-27-4A-11-E6-52-00-22-2A-11-E6-56-00-22-0A-11-E6-5A-00-62-52-11-E6-5E-00-62-12-11-16-42-00-22-AC-08-69-98-22-8A-A2-68-A6-69-42-5C-0C-F7-28-81-36-4A-53-15-00-77-6D-1A-BE-69-90-60-16-66-09-84-65-E9-05-88-7C-09-D8-93-97-79-40-2E-01-58-72-A4-00-B0-E4-48-01-60-C9-89-34-0C-8E-3B-DF-8B-8A-28-43-AD-5C-02-B0-E4-4C-2D-19-C2-D4-54-C5-4C-50-85-08-A0-AC-72-98-58-4D-C4-81-51-C0-DF-04-2A-A4-78-F3-3A-7D-57-A7-B2-A9-09-40-C6-8F-7B-26-10-A0-2F-7A-D3-60-02-8A-DD-9D-8B-BE-C9-8F-AD-8B-3E-A3-EB-5A-23-7B-53-59-A8-25-80-E7-01-4D-B7-F2-62-B7-F0-15-2B-8F-1D-8D-8F-0F-DA-05-75-91-04-D0-BB-DD-6E-C9-59-B8-6F-14-CE-80-C3-89-DB-0D-A6-02-EA-54-0F-44-02-40-23-D7-82-C1-13-A7-5D-08-8E-8E-CB-AD-B4-BD-BD-DD-B2-17-E2-34-FF-02-A6-75-4A-24-80-37-67-39-03-74-7C-9D-76-3A-9D-F7-B8-7F-D8-C3-57-89-DE-E9-1A-8D-A9-0C-EB-FD-D0-22-E3-D1-49-BE-6A-5F-FC-BB-B3-0B-83-67-98-AE-E0-46-A2-7E-BD-72-57-9F-D5-0C-D0-D0-D0-B2-73-C4-88-61-59-0D-A3-88-86-6B-FA-02-3F-AB-63-FA-AA-38-8D-3F-68-7F-CC-F7-95-FE-A5-E8-06-06-4C-7F-97-35-48-E4-99-EC-03-74-91-F1-76-C8-28-6A-07-83-E5-E1-0A-0A-74-8E-B3-A6-6E-2F-1B-1A-7F-85-B5-79-B7-7B-C8-78-C5-4C-95-34-A6-5B-DF-1F-BB-00-6E-6B-55-04-B5-43-11-4A-B8-0F-D0-9D-75-64-FC-D6-D6-56-D5-5E-A6-F2-7C-AE-9F-EA-5D-8A-8C-47-5A-64-FC-AF-B5-3F-FA-A8-20-D6-25-80-3F-FA-27-8E-98-6E-2F-B3-D6-6A-96-63-98-C2-A9-CE-68-B4-59-05-E3-F3-CB-A8-8F-96-84-A3-AB-1F-4E-E3-89-67-23-43-E1-4E-E8-44-19-F0-8A-DD-78-22-D6-19-80-A3-AF-D9-AF-69-BA-F2-0C-BF-B2-8C-27-38-90-E3-BA-42-EF-ED-19-09-70-56-BC-09-6B-83-70-E7-88-F5-36-38-87-53-72-7A-CE-F2-B8-05-78-B5-9E-7B-AC-77-C8-DE-FE-D9-22-47-22-90-38-A2-7E-9C-4B-88-8C-67-26-54-45-ED-B1-EE-FC-F7-EA-AD-6B-44-48-EA-46-28-47-8E-CC-4B-84-11-9F-3A-1F-0E-BD-8C-47-31-BF-5F-AF-DD-0A-EB-63-15-80-3B-D6-67-6F-A5-57-83-C1-06-C8-AD-B5-E5-CD-77-D1-B1-81-00-12-C8-04-76-21-AA-67-E8-53-1A-AB-77-27-E0-41-DC-33-E0-B3-FD-A2-49-A1-C8-64-A7-E2-E6-3C-9F-31-D2-AE-22-8C-C2-1D-13-FC-5F-C9-0A-77-FD-74-19-C6-10-46-00-5F-21-4C-80-8A-1B-9C-BC-BD-A0-B1-76-53-23-07-25-BC-03-1D-63-86-BC-BB-BD-13-0A-77-3E-62-BD-D3-E3-BB-11-58-00-74-64-F4-63-75-08-09-A6-B6-97-F7-F7-F7-AF-46-8D-1C-14-37-3D-9E-EA-A2-77-B7-32-BA-21-CF-38-2B-BC-62-3D-37-A0-EC-C7-78-22-96-33-3C-BF-EC-B7-0B-55-A6-80-F0-0C-01-EB-AA-38-5B-72-A2-EC-CE-C2-C4-7D-04-CE-2A-F0-49-A2-04-20-8A-ED-9D-1A-28-FC-08-42-40-E1-CE-CB-E3-BB-DE-03-09-04-A7-FB-FF-CD-91-4F-C2-18-4F-24-72-1F-60-64-7A-E5-C1-B6-D8-1F-56-6A-1B-C2-78-22-91-02-50-78-4C-61-08-23-6F-3E-AE-2D-B5-59-CF-F6-2A-10-92-C4-1E-89-91-17-A7-50-E6-25-82-33-B5-0D-43-22-7D-80-1D-2B-5B-A4-84-E9-ED-86-A7-95-32-D3-7B-7E-C3-9D-88-C4-1F-8A-36-D6-31-35-36-18-1D-76-DA-47-B9-45-65-93-1A-4F-24-7E-06-8C-A0-AD-2F-65-8F-F4-79-1D-73-88-49-A6-BD-44-22-79-E1-1F-10-69-DB-E8-61-FD-DA-C0-00-00-00-00-49-45-4E-44-AE-42-60-82",
            };

        }



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




        public static float GetCurrentInspectorWidth() => typeof(EditorGUIUtility).GetPropertyValue<float>("contextWidth");

        public static void CheckScrollbarVisibility(ref bool isScrollbarVisible)
        {
            GUILayout.Label("", GUILayout.Height(0), GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                isScrollbarVisible = GetCurrentInspectorWidth() - 33 > lastRect.width;

        }






        #endregion

    }

}
#endif