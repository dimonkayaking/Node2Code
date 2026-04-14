using UnityEngine;
using UnityEngine.UIElements;
using System.Text;

namespace CustomVisualScripting.Windows.Views
{
    public class CodeEditorView : VisualElement
    {
        private TextField _textField;
        private Label _lineNumbers;
        
        public string Code
        {
            get => _textField?.value ?? "";
            set
            {
                if (_textField != null)
                {
                    _textField.value = value;
                    UpdateLineNumbers();
                }
            }
        }
        
        public CodeEditorView()
        {
            var label = new Label("Код для парсинга:");
            label.style.marginBottom = 5;
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.white;
            Add(label);
            
            _textField = new TextField
            {
                multiline = true,
                value = ""
            };

            var editorRow = new VisualElement();
            editorRow.style.flexDirection = FlexDirection.Row;
            editorRow.style.flexGrow = 1;
            editorRow.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            editorRow.style.borderTopWidth = 1;
            editorRow.style.borderBottomWidth = 1;
            editorRow.style.borderLeftWidth = 1;
            editorRow.style.borderRightWidth = 1;
            editorRow.style.borderTopColor = new Color(0.24f, 0.24f, 0.24f);
            editorRow.style.borderBottomColor = new Color(0.24f, 0.24f, 0.24f);
            editorRow.style.borderLeftColor = new Color(0.24f, 0.24f, 0.24f);
            editorRow.style.borderRightColor = new Color(0.24f, 0.24f, 0.24f);

            var gutter = new VisualElement();
            gutter.style.width = 48;
            gutter.style.backgroundColor = new Color(0.10f, 0.10f, 0.10f);
            gutter.style.borderRightWidth = 1;
            gutter.style.borderRightColor = new Color(0.20f, 0.20f, 0.20f);
            gutter.style.paddingTop = 4;
            gutter.style.paddingBottom = 4;
            gutter.style.paddingRight = 6;

            _lineNumbers = new Label();
            _lineNumbers.style.unityTextAlign = TextAnchor.UpperRight;
            _lineNumbers.style.color = new Color(0.45f, 0.45f, 0.45f);
            _lineNumbers.style.fontSize = 12;
            _lineNumbers.style.whiteSpace = WhiteSpace.Normal;
            gutter.Add(_lineNumbers);

            _textField.style.flexGrow = 1;
            _textField.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            _textField.style.unityTextAlign = TextAnchor.UpperLeft;
            _textField.style.color = new Color(0.9f, 0.9f, 0.9f);
            _textField.style.fontSize = 13;
            _textField.style.paddingLeft = 6;
            _textField.style.paddingRight = 6;
            _textField.RegisterValueChangedCallback(_ => UpdateLineNumbers());

            editorRow.Add(gutter);
            editorRow.Add(_textField);
            Add(editorRow);
            UpdateLineNumbers();
            
            style.flexGrow = 1;
            style.marginLeft = 5;
            style.marginRight = 5;
        }
        
        public new void Clear()
        {
            _textField.value = "";
            UpdateLineNumbers();
        }

        private void UpdateLineNumbers()
        {
            var code = _textField?.value ?? string.Empty;
            int lineCount = 1;
            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] == '\n')
                    lineCount++;
            }

            var sb = new StringBuilder(lineCount * 4);
            for (int i = 1; i <= lineCount; i++)
            {
                sb.Append(i);
                if (i < lineCount)
                    sb.Append('\n');
            }

            _lineNumbers.text = sb.ToString();
        }
    }
}