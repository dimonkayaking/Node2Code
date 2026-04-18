using System;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    /// <summary>
    /// В Unity 6 <see cref="IStyle.cursor"/> принимает только <see cref="Cursor"/> (текстура),
    /// а не <see cref="MouseCursor"/>. Для системных курсоров редактора используем скрытое поле
    /// <c>defaultCursorId</c> у <see cref="Cursor"/> (как в обсуждениях Unity / Stack Overflow).
    /// </summary>
    internal static class EditorUiPointerCursor
    {
        private static MemberInfo _defaultCursorIdMember;

        public static bool TryApply(VisualElement element, MouseCursor mouseCursor)
        {
            if (element == null)
                return false;

            try
            {
                _defaultCursorIdMember ??= FindDefaultCursorIdMember(typeof(Cursor));
                if (_defaultCursorIdMember == null)
                    return false;

                object boxed = Activator.CreateInstance(typeof(Cursor));
                switch (_defaultCursorIdMember)
                {
                    case PropertyInfo pi:
                        pi.SetValue(boxed, (int)mouseCursor);
                        break;
                    case FieldInfo fi:
                        fi.SetValue(boxed, (int)mouseCursor);
                        break;
                    default:
                        return false;
                }

                element.style.cursor = new StyleCursor((Cursor)boxed);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Clear(VisualElement element)
        {
            if (element == null)
                return;
            element.style.cursor = StyleKeyword.Auto;
        }

        private static MemberInfo FindDefaultCursorIdMember(Type cursorType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var p = cursorType.GetProperty("defaultCursorId", flags);
            if (p != null)
                return p;
            var f = cursorType.GetField("defaultCursorId", flags);
            if (f != null)
                return f;
            // На случай переименования в другой версии Unity
            foreach (var field in cursorType.GetFields(flags))
            {
                if (field.FieldType == typeof(int) && field.Name.IndexOf("cursor", StringComparison.OrdinalIgnoreCase) >= 0)
                    return field;
            }

            return null;
        }
    }
}
