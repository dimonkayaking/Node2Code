using UnityEngine.UIElements;

namespace CustomVisualScripting.Windows.Views
{
    public class CodeEditorView : VisualElement
    {
        private TextField _codeField;
        private Label _titleLabel;
        
        public string Code
        {
            get => _codeField?.value ?? string.Empty;
            set { if (_codeField != null) _codeField.value = value; }
        }
        
        public CodeEditorView()
        {
            style.flexGrow = 1;
            style.backgroundColor = new StyleColor(new UnityEngine.Color(0.18f, 0.18f, 0.18f));
            style.paddingTop = 5;
            style.paddingLeft = 5;
            style.paddingRight = 5;
            
            _titleLabel = new Label("C# Code");
            _titleLabel.style.fontSize = 14;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.color = Color.white;
            _titleLabel.style.marginBottom = 5;
            Add(_titleLabel);
            
            _codeField = new TextField();
            _codeField.multiline = true;
            _codeField.style.flexGrow = 1;
            _codeField.style.whiteSpace = WhiteSpace.Normal;
            _codeField.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.22f, 0.22f, 0.22f));
            _codeField.style.color = Color.white;
            _codeField.style.fontSize = 12;
            _codeField.style.fontFamily = "Courier New";
            _codeField.value = @"float speed = 5;
float offset = speed * 0.1f;
if (speed > 3)
{
    transform.position = new Vector3(offset, 0, 0);
    UnityEngine.Debug.Log(speed);
}
else
{
    UnityEngine.Debug.Log(0);
}";
            
            Add(_codeField);
        }
        
        public void Clear()
        {
            _codeField.value = string.Empty;
        }
    }
}