using UnityEngine;
using UnityEngine.UIElements;
using System.Text;

namespace CustomVisualScripting.Windows.Views
{
    public class CodeEditorView : VisualElement
    {
        private TextField _textField;
        private TextField _lineNumbersField;
        private ScrollView _codeScrollView;
        private ScrollView _lineScrollView;
        private bool _syncingScroll;
        
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

            _lineNumbersField = new TextField
            {
                multiline = true,
                isReadOnly = true,
                value = "1"
            };
            _lineNumbersField.style.flexGrow = 1;
            _lineNumbersField.style.unityTextAlign = TextAnchor.UpperRight;
            _lineNumbersField.style.backgroundColor = new Color(0.10f, 0.10f, 0.10f);
            _lineNumbersField.style.color = new Color(0.45f, 0.45f, 0.45f);
            _lineNumbersField.style.fontSize = 13;
            _lineNumbersField.style.paddingRight = 6;
            _lineNumbersField.style.paddingLeft = 2;
            _lineNumbersField.style.borderTopWidth = 0;
            _lineNumbersField.style.borderBottomWidth = 0;
            _lineNumbersField.style.borderLeftWidth = 0;
            _lineNumbersField.style.borderRightWidth = 0;
            _lineNumbersField.pickingMode = PickingMode.Ignore;
            gutter.Add(_lineNumbersField);

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
            RegisterCallback<AttachToPanelEvent>(_ => SetupScrollSync());
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

            if (_lineNumbersField != null)
                _lineNumbersField.value = sb.ToString();
        }

        private void SetupScrollSync()
        {
            if (_textField == null || _lineNumbersField == null)
                return;

            _codeScrollView = _textField.Q<ScrollView>();
            _lineScrollView = _lineNumbersField.Q<ScrollView>();
            if (_codeScrollView == null || _lineScrollView == null)
                return;

            _codeScrollView.verticalScroller.valueChanged += OnCodeScrollChanged;
            _lineScrollView.verticalScroller.valueChanged += OnLineScrollChanged;
            _lineScrollView.horizontalScroller.value = 0;
        }

        private void OnCodeScrollChanged(float value)
        {
            if (_syncingScroll || _lineScrollView == null)
                return;
            _syncingScroll = true;
            _lineScrollView.verticalScroller.value = value;
            _syncingScroll = false;
        }

        private void OnLineScrollChanged(float value)
        {
            if (_syncingScroll || _codeScrollView == null)
                return;
            _syncingScroll = true;
            _codeScrollView.verticalScroller.value = value;
            _syncingScroll = false;
        }
    }
}