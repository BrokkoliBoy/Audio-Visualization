using System;
using System.Collections;
using System.Reflection;

namespace Gavi.VideoDirector
{
    /// <summary>Reads and writes a field of an object by its path, e.g. "MorphAmount", "NoiseLayer2.Zoom" or "Shapes[0].Strength".</summary>
    public static class FieldPath
    {
        public static object Get(object root, string path)
        {
            Resolve(root, path, out object owner, out FieldInfo field);
            return field.GetValue(owner);
        }

        public static float GetFloat(object root, string path)
        {
            return Convert.ToSingle(Get(root, path));
        }

        public static void Set(object root, string path, object value)
        {
            Resolve(root, path, out object owner, out FieldInfo field);
            field.SetValue(owner, field.FieldType.IsEnum ? Enum.ToObject(field.FieldType, value) : Convert.ChangeType(value, field.FieldType));
        }

        private static void Resolve(object root, string path, out object owner, out FieldInfo field)
        {
            string[] parts = path.Split('.');
            owner = root;
            for (int i = 0; i < parts.Length - 1; i++)
                owner = Step(owner, parts[i]);

            field = FindField(owner, parts[parts.Length - 1]);
        }

        /// <summary>One part of the path: the field, and the element of it when the part ends in [index].</summary>
        private static object Step(object owner, string part)
        {
            int bracket = part.IndexOf('[');
            if (bracket < 0)
                return FindField(owner, part).GetValue(owner);

            string fieldName = part.Substring(0, bracket);
            int index = int.Parse(part.Substring(bracket + 1, part.Length - bracket - 2));
            return ((IList)FindField(owner, fieldName).GetValue(owner))[index];
        }

        private static FieldInfo FindField(object owner, string name)
        {
            FieldInfo field = owner.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException("No field '" + name + "' in " + owner.GetType().Name);
            return field;
        }
    }
}
