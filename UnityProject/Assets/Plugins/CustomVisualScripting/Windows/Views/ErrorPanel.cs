using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Windows.Views
{
    public class ErrorPanel : VisualElement
    {
        private ListView _errorList;
        private Label _titleLabel;
        private Button _clearButton;
        
        public ErrorPanel()
        {
            style.backgroundColor = new StyleColor(new UnityEngine.Color(0.2f, 0.1f, 0.1f));
            style.borderTopWidth = 2;
            style.borderTopColor = new StyleColor(new UnityEngine.Color(0.8f, 0.2f, 0.2f));
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.paddingLeft = 10;
            style.paddingRight = 10;
            style.minHeight = 100;
            style.maxHeight = 200;
            
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            
            _titleLabel = new Label("Ошибки");
            _titleLabel.style.color = new StyleColor(new UnityEngine.Color(1f, 0.5f, 0.5f));
            _titleLabel.style.fontSize = 14;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(_titleLabel);
            
            _clearButton = new Button(Clear);
            _clearButton.text = "×";
            _clearButton.style.width = 20;
            _clearButton.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.3f, 0.1f, 0.1f));
            _clearButton.style.color = Color.white;
            header.Add(_clearButton);
            
            Add(header);
            
            _errorList = new ListView();
            _errorList.style.flexGrow = 1;
            _errorList.style.marginTop = 5;
            _errorList.selectionType = SelectionType.None;
            
            _errorList.makeItem = () =>
            {
                var label = new Label();
                label.style.color = new StyleColor(new UnityEngine.Color(1f, 0.7f, 0.7f));
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.paddingTop = 2;
                label.style.paddingBottom = 2;
                return label;
            };
            
            _errorList.bindItem = (element, index) =>
            {
                if (element is Label label && _errorList.itemsSource != null && index < _errorList.itemsSource.Count)
                {
                    label.text = _errorList.itemsSource[index] as string;
                }
            };
            
            Add(_errorList);
            
            style.display = DisplayStyle.None;
        }
        
        public void ShowErrors(List<string> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                style.display = DisplayStyle.None;
                return;
            }
            
            _titleLabel.text = $"Ошибки ({errors.Count})";
            _errorList.itemsSource = errors;
            _errorList.RefreshItems();
            
            style.display = DisplayStyle.Flex;
        }
        
        public void Clear()
        {
            style.display = DisplayStyle.None;
            _errorList.itemsSource = null;
            _errorList.RefreshItems();
        }
    }
}