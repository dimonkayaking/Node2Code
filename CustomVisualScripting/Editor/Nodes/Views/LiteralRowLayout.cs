using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    /// <summary>
    /// Строка «метка + контрол» на всю ширину ноды (поле растягивается сразу после подписи).
    /// </summary>
    public static class LiteralRowLayout
    {
        public static VisualElement Row(string labelText, VisualElement control, float labelWidth = 52f)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minWidth = 0;
            row.style.width = Length.Percent(100);
            row.style.marginBottom = 3;

            var lab = new Label(labelText);
            lab.style.width = labelWidth;
            lab.style.minWidth = labelWidth;
            lab.style.flexShrink = 0;
            lab.style.marginRight = 8;
            lab.style.fontSize = 11;
            lab.style.color = new Color(0.82f, 0.82f, 0.82f);
            lab.style.unityTextAlign = TextAnchor.MiddleLeft;

            control.style.flexGrow = 1;
            control.style.flexShrink = 1;
            control.style.flexBasis = 0;
            control.style.minWidth = 40;
            control.style.alignSelf = Align.Stretch;

            row.Add(lab);
            row.Add(control);
            return row;
        }
    }
}
