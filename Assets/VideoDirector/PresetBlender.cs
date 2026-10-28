using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// Blends two terrain presets field by field through reflection, so a field that is added to the preset on <c>next</c> is blended without a change here.
    /// Floats glide, hue (x of the color values) takes the short way round, everything that cannot glide (switches, whole numbers, names, patterns, shapes)
    /// changes from the first to the second preset at one moment.
    /// </summary>
    public static class PresetBlender
    {
        private const string ColorValuesField = "ColorValues";

        /// <param name="t">0 = from, 1 = to.</param>
        /// <param name="takeTo">True once the fields that cannot glide have changed over to the second preset.</param>
        public static void Blend(object from, object to, object result, float t, bool takeTo)
        {
            foreach (FieldInfo field in from.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object value = BlendValue(field.Name, field.FieldType, field.GetValue(from), field.GetValue(to), field.GetValue(result), t, takeTo);
                field.SetValue(result, value);
            }
        }

        private static object BlendValue(string name, Type type, object a, object b, object current, float t, bool takeTo)
        {
            if (type == typeof(float))
                return Mathf.Lerp((float)a, (float)b, t);
            if (type == typeof(Vector3))
                return BlendVector((Vector3)a, (Vector3)b, t, name == ColorValuesField);
            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                return takeTo ? b : a;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return BlendList(name, type, (IList)a, (IList)b, t, takeTo);
            if (type.IsClass)
            {
                if (a == null || b == null)
                    return takeTo ? b : a;
                object result = current ?? Activator.CreateInstance(type);
                Blend(a, b, result, t, takeTo);
                return result;
            }
            return takeTo ? b : a;
        }

        private static Vector3 BlendVector(Vector3 a, Vector3 b, float t, bool hueInX)
        {
            Vector3 result = Vector3.Lerp(a, b, t);
            if (hueInX)
                result.x = Mathf.LerpAngle(a.x, b.x, t);
            return result;
        }

        /// <summary>Lists of the same length are blended element by element, otherwise one of them is taken whole.</summary>
        private static object BlendList(string name, Type listType, IList a, IList b, float t, bool takeTo)
        {
            IList source = takeTo ? b : a;
            IList list = (IList)Activator.CreateInstance(listType);
            Type elementType = listType.GetGenericArguments()[0];
            bool sameLength = a != null && b != null && a.Count == b.Count;
            if (source == null)
                return list;

            for (int i = 0; i < source.Count; i++)
            {
                if (!sameLength)
                    list.Add(Clone(elementType, source[i]));
                else
                    list.Add(BlendValue(name, elementType, a[i], b[i], null, t, takeTo));
            }
            return list;
        }

        private static object Clone(Type type, object value)
        {
            if (value == null || type.IsValueType || type == typeof(string))
                return value;
            object copy = Activator.CreateInstance(type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                field.SetValue(copy, Clone(field.FieldType, field.GetValue(value)));
            return copy;
        }
    }
}
