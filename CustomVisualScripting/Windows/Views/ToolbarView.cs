using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Windows.Views
{
    public class ToolbarView : VisualElement
    {
        public Button ParseButton { get; private set; }
        public Button GenerateButton { get; private set; }
        public Button RunButton { get; private set; }
        public Button StopButton { get; private set; }
        public Button SaveButton { get; private set; }
        public Button SaveAsButton { get; private set; }
        public Button LoadButton { get; private set; }
        public Button ClearButton { get; private set; }
        
        private Label _statusLabel;
        
        public ToolbarView()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            style.paddingTop = 6;
            style.paddingBottom = 6;
            style.paddingLeft = 12;
            style.paddingRight = 12;
            style.borderBottomWidth = 1;
            style.borderBottomColor = Color.black;
            style.borderTopWidth = 1;
            style.borderTopColor = Color.black;
            
            ParseButton = new Button { text = "Парсить код" };
            ConfigureToolbarButton(ParseButton);
            ParseButton.style.marginRight = 5;
            Add(ParseButton);
            
            GenerateButton = new Button { text = "Сгенерировать" };
            ConfigureToolbarButton(GenerateButton);
            GenerateButton.style.marginRight = 5;
            Add(GenerateButton);
            
            RunButton = new Button { text = "▶ Run" };
            ConfigureToolbarButton(RunButton);
            RunButton.style.marginRight = 5;
            RunButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f);
            Add(RunButton);
            
            StopButton = new Button { text = "⏹ Stop" };
            ConfigureToolbarButton(StopButton);
            StopButton.style.marginRight = 5;
            StopButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
            StopButton.SetEnabled(false);
            Add(StopButton);
            
            SaveButton = new Button { text = "Сохранить" };
            ConfigureToolbarButton(SaveButton);
            SaveButton.style.marginRight = 5;
            Add(SaveButton);
            
            SaveAsButton = new Button { text = "Сохранить как" };
            ConfigureToolbarButton(SaveAsButton);
            SaveAsButton.style.marginRight = 5;
            Add(SaveAsButton);
            
            LoadButton = new Button { text = "Загрузить" };
            ConfigureToolbarButton(LoadButton);
            LoadButton.style.marginRight = 5;
            Add(LoadButton);
            
            ClearButton = new Button { text = "Очистить" };
            ConfigureToolbarButton(ClearButton);
            ClearButton.style.marginRight = 10;
            Add(ClearButton);
            
            _statusLabel = new Label("Готов");
            _statusLabel.style.color = Color.white;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _statusLabel.style.flexGrow = 1;
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            _statusLabel.style.fontSize = 13;
            Add(_statusLabel);

            // Эффект наведения для Run и Stop (только если активны)
            AddHoverEffect(RunButton);
            AddHoverEffect(StopButton);
        }

        private void AddHoverEffect(Button button)
        {
            Color normalColor = button.resolvedStyle.backgroundColor;
            button.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (button.enabledSelf)
                {
                    button.style.backgroundColor = LightenColor(normalColor, 0.07f);
                }
            });
            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (button.enabledSelf)
                {
                    button.style.backgroundColor = normalColor;
                }
            });
        }

        private static Color LightenColor(Color color, float amount)
        {
            return new Color(
                Mathf.Min(color.r + amount, 1f),
                Mathf.Min(color.g + amount, 1f),
                Mathf.Min(color.b + amount, 1f),
                color.a
            );
        }

        private static void ConfigureToolbarButton(Button button)
        {
            button.style.fontSize = 14;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.paddingTop = 4;
            button.style.paddingBottom = 4;
            button.style.minHeight = 24;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
        }
        
        public void SetRunMode(bool isRunning)
        {
            RunButton.SetEnabled(!isRunning);
            StopButton.SetEnabled(isRunning);
            
            if (isRunning)
            {
                RunButton.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
                StopButton.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            }
            else
            {
                RunButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f);
                StopButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
            }
        }
        
        public void SetStatusNormal(string message)
        {
            _statusLabel.text = message;
            _statusLabel.style.color = Color.white;
        }
        
        public void SetStatusWarning(string message)
        {
            _statusLabel.text = message;
            _statusLabel.style.color = Color.yellow;
        }
        
        public void SetStatusError(string message)
        {
            _statusLabel.text = message;
            _statusLabel.style.color = Color.red;
        }
        
        public void SetStatusSuccess(string message)
        {
            _statusLabel.text = message;
            _statusLabel.style.color = Color.green;
        }
    }
}