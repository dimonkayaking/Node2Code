using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Windows.Views
{
    public class ErrorPanel : VisualElement
    {
        private Label _titleLabel;
        private ScrollView _errorScrollView;
        
        public ErrorPanel()
        {
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            style.marginTop = 5;
            style.marginBottom = 5;
            style.marginLeft = 5;
            style.marginRight = 5;
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.paddingLeft = 10;
            style.paddingRight = 10;
            
            _titleLabel = new Label("Ошибки:");
            _titleLabel.style.fontSize = 12;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.color = new Color(1f, 0.6f, 0.6f);
            _titleLabel.style.display = DisplayStyle.None;
            Add(_titleLabel);
            
            _errorScrollView = new ScrollView();
            _errorScrollView.style.flexGrow = 1;
            _errorScrollView.style.maxHeight = 150;
            _errorScrollView.style.display = DisplayStyle.None;
            Add(_errorScrollView);
            
            style.display = DisplayStyle.None;
        }
        
        public void ShowErrors(List<string> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                Clear();
                return;
            }
            
            style.display = DisplayStyle.Flex;
            _titleLabel.style.display = DisplayStyle.Flex;
            _errorScrollView.style.display = DisplayStyle.Flex;
            
            _errorScrollView.Clear();
            
            foreach (var error in errors)
            {
                var errorLabel = new Label($"• {error}");
                errorLabel.style.color = new Color(1f, 0.5f, 0.5f);
                errorLabel.style.fontSize = 11;
                errorLabel.style.marginBottom = 2;
                errorLabel.style.whiteSpace = WhiteSpace.Normal;
                _errorScrollView.Add(errorLabel);
            }
        }
        
        public new void Clear()
        {
            style.display = DisplayStyle.None;
            _titleLabel.style.display = DisplayStyle.None;
            _errorScrollView.style.display = DisplayStyle.None;
            _errorScrollView.Clear();
        }
    }
}