using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Windows.Views
{
    public class ToolbarView : VisualElement
    {
        public Button ParseButton { get; private set; }
        public Button GenerateButton { get; private set; }
        public Button SaveButton { get; private set; }
        public Button LoadButton { get; private set; }
        public Button ClearButton { get; private set; }
        public Label StatusLabel { get; private set; }
        
        public ToolbarView()
        {
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = new StyleColor(new UnityEngine.Color(0.15f, 0.15f, 0.15f));
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.paddingLeft = 10;
            style.paddingRight = 10;
            style.borderBottomWidth = 1;
            style.borderBottomColor = new StyleColor(new UnityEngine.Color(0.3f, 0.3f, 0.3f));
            
            ParseButton = CreateButton("Parse →", new UnityEngine.Color(0.2f, 0.5f, 0.8f));
            Add(ParseButton);
            
            GenerateButton = CreateButton("← Generate", new UnityEngine.Color(0.8f, 0.5f, 0.2f));
            Add(GenerateButton);
            
            AddSeparator();
            
            SaveButton = CreateButton("Save", new UnityEngine.Color(0.3f, 0.3f, 0.3f));
            Add(SaveButton);
            
            LoadButton = CreateButton("Load", new UnityEngine.Color(0.3f, 0.3f, 0.3f));
            Add(LoadButton);
            
            AddSeparator();
            
            ClearButton = CreateButton("Clear", new UnityEngine.Color(0.5f, 0.2f, 0.2f));
            Add(ClearButton);
            
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            Add(spacer);
            
            StatusLabel = new Label("Готов");
            StatusLabel.style.color = new StyleColor(new UnityEngine.Color(0.7f, 0.7f, 0.7f));
            StatusLabel.style.fontSize = 11;
            StatusLabel.style.marginRight = 5;
            Add(StatusLabel);
        }
        
        private Button CreateButton(string text, UnityEngine.Color color)
        {
            var btn = new Button();
            btn.text = text;
            btn.style.backgroundColor = color;
            btn.style.color = Color.white;
            btn.style.marginRight = 5;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.paddingTop = 5;
            btn.style.paddingBottom = 5;
            return btn;
        }
        
        private void AddSeparator()
        {
            var sep = new VisualElement();
            sep.style.width = 1;
            sep.style.height = 20;
            sep.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.4f, 0.4f, 0.4f));
            sep.style.marginLeft = 5;
            sep.style.marginRight = 10;
            Add(sep);
        }
        
        public void SetStatus(string text, UnityEngine.Color color)
        {
            StatusLabel.text = text;
            StatusLabel.style.color = color;
        }
        
        public void SetStatusNormal(string text) => SetStatus(text, new UnityEngine.Color(0.7f, 0.7f, 0.7f));
        public void SetStatusSuccess(string text) => SetStatus(text, new UnityEngine.Color(0.3f, 1f, 0.3f));
        public void SetStatusError(string text) => SetStatus(text, new UnityEngine.Color(1f, 0.5f, 0.5f));
        public void SetStatusWarning(string text) => SetStatus(text, new UnityEngine.Color(1f, 1f, 0.3f));
    }
}